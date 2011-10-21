﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.DataStructures;
using System.Threading;
using System.Globalization;
using System.Reflection;

namespace Signum.Utilities
{
    public static class Sync
    {
        public static IDisposable ChangeBothCultures(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return null;

            return ChangeBothCultures(new CultureInfo(cultureName));
        }

        public static IDisposable ChangeBothCultures(CultureInfo ci)
        {
            if (ci == null)
                return null;

            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentCulture;
            CultureInfo oldUI = t.CurrentUICulture;
            t.CurrentCulture = ci;
            t.CurrentUICulture = ci;
            return new Disposable(() =>
            {
                t.CurrentCulture = old;
                t.CurrentUICulture = oldUI;
            });
        }

        public static IDisposable ChangeCulture(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return null;

            return ChangeCulture(new CultureInfo(cultureName));
        }

        public static IDisposable ChangeCulture(CultureInfo ci)
        {
            if (ci == null)
                return null;

            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentCulture;
            t.CurrentCulture = ci;
            return new Disposable(() => t.CurrentCulture = old);
        }

        public static IDisposable ChangeCultureUI(string cultureName)
        {
            if (string.IsNullOrEmpty(cultureName))
                return null;

            return ChangeCultureUI(new CultureInfo(cultureName));
        }

        public static IDisposable ChangeCultureUI(CultureInfo ci)
        {
            if (ci == null)
                return null;

            Thread t = Thread.CurrentThread;
            CultureInfo old = t.CurrentUICulture;
            t.CurrentUICulture = ci;
            return new Disposable(() => t.CurrentUICulture = old);
        }

        public static void ResetPublicationOnly<T>(this Lazy<T> lazy)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            LazyThreadSafetyMode mode = (LazyThreadSafetyMode)typeof(Lazy<T>).GetProperty("Mode", flags).GetValue(lazy, null);
            if (mode != LazyThreadSafetyMode.PublicationOnly)
                throw new InvalidOperationException("ResetPublicationOnly only works for Lazy<T> with LazyThreadSafetyMode.PublicationOnly");

            typeof(Lazy<T>).GetField("m_boxed", flags).SetValue(lazy, null); 
        }

        public static void Load<T>(this Lazy<T> lazy)
        {
            var a = lazy.Value;
        }

        public static void SafeUpdate<T>(ref T variable, Func<T, T> repUpdateFunction) where T : class
        {
            T oldValue, newValue;
            do
            {
                oldValue = variable;
                newValue = repUpdateFunction(oldValue);

                if (newValue == null)
                    break;

            } while (Interlocked.CompareExchange<T>(ref variable, newValue, oldValue) != oldValue);
        }

        public static V SafeGetOrCreate<K, V>(ref ImmutableAVLTree<K, V> tree, K key, Func<V> createValue)
            where K : IComparable<K>
        {
            V result;
            if (tree.TryGetValue(key, out result))
                return result;

            V value = createValue();

            SafeUpdate(ref tree, t =>
            {
                if (t.TryGetValue(key, out result))
                    return null;
                else
                {
                    result = value;
                    return t.Add(key, value);
                }
            });

            return result;
        }

        public static LocString ToLoc(Func<string> resourceProperty)
        {
            return lang =>
            {
                using (Sync.ChangeBothCultures(lang))
                    return resourceProperty();
            };
        }
    }

    public delegate string LocString(string lang);
}
