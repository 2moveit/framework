﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Threading;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine
{
    public static class GlobalLazy
    {
        static bool initialized; 

        internal static void GlobalLazy_Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            var s = Schema.Current;
            foreach (var kvp in registeredLazyList.ToList())
            {
                if (kvp.Value != null)
                {
                    AttachInvalidations(s,kvp.Key, kvp.Value);
                }
            }
        }

        private static void AttachInvalidations(Schema s, IResetLazy lazy, params Type[] types)
        {
            Action reset = () =>
            {
                if (Transaction.InTestTransaction)
                {
                    lazy.Reset();
                    Transaction.Rolledback += () => lazy.Reset();
                }

                Transaction.PostRealCommit += dic => lazy.Reset();
            };

            List<Type> cached = new List<Type>();
            foreach (var type in types)
            {
                var cc = s.CacheController(type);
                if (cc != null && cc.IsComplete)
                {
                    cc.Disabled += reset;
                    cached.Add(type);
                }
            }

            var nonCached = types.Except(cached).ToList();
            if (nonCached.Any())
            {
                foreach (var type in nonCached)
                {
                    giAttachInvalidations.GetInvoker(type)(s, reset);
                }

                var dgIn = DirectedGraph<Table>.Generate(types.Except(cached).Select(t => s.Table(t)), t => t.DependentTables().Select(kvp => kvp.Key)).ToHashSet();
                var dgOut = DirectedGraph<Table>.Generate(cached.Select(t => s.Table(t)), t => t.DependentTables().Select(kvp => kvp.Key)).ToHashSet();

                foreach (var table in dgIn.Except(dgOut))
                {
                    giAttachInvalidationsDependant.GetInvoker(table.Type)(s, reset);
                }
            }
        }


        static GenericInvoker<Action<Schema, Action>> giAttachInvalidationsDependant = new GenericInvoker<Action<Schema, Action>>((s, a) => AttachInvalidationsDependant<IdentifiableEntity>(s, a));
        static void AttachInvalidationsDependant<T>(Schema s, Action action) where T : IdentifiableEntity
        {
            var ee = s.EntityEvents<T>();

            ee.Saving += e =>
            {
                if (!e.IsNew && e.Modified == true)
                    action();
            };
            ee.PreUnsafeUpdate += q => action();
        }

        static GenericInvoker<Action<Schema, Action>> giAttachInvalidations = new GenericInvoker<Action<Schema, Action>>((s, a) => AttachInvalidations<IdentifiableEntity>(s, a));
        static void AttachInvalidations<T>(Schema s, Action action) where T : IdentifiableEntity
        {
            var ee = s.EntityEvents<T>();

            ee.Saving += e =>
            {
                if (e.Modified == true)
                    action();
            };
            ee.PreUnsafeUpdate += q => action();
            ee.PreUnsafeDelete += q => action();
        }

        static Dictionary<IResetLazy, Type[]> registeredLazyList = new Dictionary<IResetLazy, Type[]>();
        public static ResetLazy<T> Create<T>(Func<T> func, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly) where T:class
        {
            var result = new ResetLazy<T>(() =>
            {
                using (Schema.Current.GlobalMode())
                using (HeavyProfiler.Log("Lazy", () => typeof(T).TypeName()))
                using (Transaction tr = Transaction.InTestTransaction ? null:  Transaction.ForceNew())
                using (new EntityCache(true))
                {
                    var value = func();

                    if (tr != null)
                        tr.Commit();

                    return value;
                }
            }, mode);

            registeredLazyList.Add(result, null);

            return result;
        }

        public static ResetLazy<T> InvalidateWith<T>(this ResetLazy<T> lazy, params Type[] types) where T:class
        {
            if (!registeredLazyList.ContainsKey(lazy))
                throw new InvalidOperationException("The lazy is not a GlobalLazy");

            registeredLazyList[lazy] = types;

            if (initialized)
                AttachInvalidations(Schema.Current, lazy, types);

            return lazy;
        }

        public static void ResetAll()
        {
            foreach (var lp in registeredLazyList.Keys)
                lp.Reset();
        }

        public static void LoadAll()
        {
            foreach (var lp in registeredLazyList.Keys)
                lp.Load();
        }
    }
}
