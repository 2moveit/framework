﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Entities;
using Signum.Utilities;
using System.Globalization;
using Signum.Engine.Properties;
using System.Diagnostics;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.DataStructures;
using System.Threading;
using System.Linq.Expressions;
using Signum.Entities.Reflection;
using System.Collections;
using Signum.Utilities.Reflection;

namespace Signum.Engine.Maps
{
    public class Schema : IImplementationsFinder
    {
        bool silentMode = false;
        public bool SilentMode
        {
            get { return silentMode; }
            set { this.silentMode = value; }
        }

        public CultureInfo ForceCultureInfo { get; set; }

        public TimeZoneMode TimeZoneMode { get; set; }

        public Assembly MainAssembly { get; set; }

        public SchemaSettings Settings { get; private set; }

        Dictionary<Type, Table> tables = new Dictionary<Type, Table>();
        public Dictionary<Type, Table> Tables
        {
            get { return tables; }
        }

        const string errorType = "TypeDN table not cached. Remember to call Schema.Current.Initialize";

        Dictionary<Type, int> typeToId;
        internal Dictionary<Type, int> TypeToId
        {
            get { return typeToId.ThrowIfNullC(errorType); }
            set { typeToId = value; }
        }

        Dictionary<int, Type> idToType;
        internal Dictionary<int, Type> IdToType
        {
            get { return idToType.ThrowIfNullC(errorType); }
            set { idToType = value; }
        }

        Dictionary<Type, TypeDN> typeToDN;
        internal Dictionary<Type, TypeDN> TypeToDN
        {
            get { return typeToDN.ThrowIfNullC(errorType); }
            set { typeToDN = value; }
        }

        Dictionary<TypeDN, Type> dnToType;
        internal Dictionary<TypeDN, Type> DnToType
        {
            get { return dnToType.ThrowIfNullC(errorType); }
            set { dnToType = value; }
        }

        Dictionary<string, Type> nameToType = new Dictionary<string, Type>();
        internal Dictionary<string, Type> NameToType
        {
            get { return nameToType; }
            //set { nameToType = value; }
        }

        Dictionary<Type, string> typeToName = new Dictionary<Type, string>();
        internal Dictionary<Type, string> TypeToName
        {
            get { return typeToName; }
            //set { typeToName = value; }
        }

        internal Type GetType(int id)
        {
            return this.idToType[id];
        }

        #region Events

        public event Func<Type, string> IsAllowedCallback;

        public string IsAllowed(Type type)
        {
            if (IsAllowedCallback != null)
                foreach (Func<Type, string> f in IsAllowedCallback.GetInvocationList())
                {
                    string result = f(type);

                    if (result != null)
                        return result;
                }

            return null;
        }

        public void AssertAllowed(Type type)
        {
            string error = IsAllowed(type);

            if (error != null)
                throw new UnauthorizedAccessException(Resources.UnauthorizedAccessTo0Because1.Formato(type.NiceName(), error));
        }

        readonly IEntityEvents entityEventsGlobal = new EntityEvents<IdentifiableEntity>();
        public EntityEvents<IdentifiableEntity> EntityEventsGlobal
        {
            get { return (EntityEvents<IdentifiableEntity>)entityEventsGlobal; }
        }

        Dictionary<Type, IEntityEvents> entityEvents = new Dictionary<Type, IEntityEvents>();
        public EntityEvents<T> EntityEvents<T>()
            where T : IdentifiableEntity
        {
            return (EntityEvents<T>)entityEvents.GetOrCreate(typeof(T), () => new EntityEvents<T>());
        }

        internal void OnPreSaving(IdentifiableEntity entity, ref bool graphModified)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnPreSaving(entity, ref graphModified);

            entityEventsGlobal.OnPreSaving(entity, ref graphModified);
        }

        internal void OnSaving(IdentifiableEntity entity)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnSaving(entity);

            entityEventsGlobal.OnSaving(entity);
        }

        internal void OnRetrieved(IdentifiableEntity entity)
        {
            AssertAllowed(entity.GetType());

            IEntityEvents ee = entityEvents.TryGetC(entity.GetType());

            if (ee != null)
                ee.OnRetrieved(entity);

            entityEventsGlobal.OnRetrieved(entity);
        }

        internal void OnPreUnsafeDelete<T>(IQueryable<T> query) where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee != null)
                ee.OnPreUnsafeDelete(query);
        }

        internal void OnPreUnsafeUpdate<T>(IQueryable<T> query) where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee != null)
                ee.OnPreUnsafeUpdate(query);
        }

        internal ICacheController CacheController(Type type)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal CacheController<T> CacheController<T>() where T : IdentifiableEntity
        {
            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));

            if (ee == null)
                return null;

            return ee.CacheController;
        }

        internal IQueryable<T> OnFilterQuery<T>(IQueryable<T> query)
            where T : IdentifiableEntity
        {
            AssertAllowed(typeof(T));

            EntityEvents<T> ee = (EntityEvents<T>)entityEvents.TryGetC(typeof(T));
            if (ee == null)
                return query;

            return ee.OnFilterQuery(query);
        }

        internal bool HasQueryFilter(Type type)
        {
            IEntityEvents ee = entityEvents.TryGetC(type);
            if (ee == null)
                return false;

            return ee.HasQueryFilter;
        }

        public event Func<Replacements, SqlPreCommand> Synchronizing;
        internal SqlPreCommand SynchronizationScript(string schemaName)
        {
            if (Synchronizing == null)
                return null;

            using (Sync.ChangeBothCultures(ForceCultureInfo))
            {
                Replacements replacements = new Replacements();
                SqlPreCommand command = Synchronizing
                    .GetInvocationList()
                    .Cast<Func<Replacements, SqlPreCommand>>()
                    .Select(e =>
                    {
                        try
                        {
                            return e(replacements);
                        }
                        catch (Exception ex)
                        {
                            return new SqlPreCommandSimple("Exception on {0}.{1}: {2}".Formato(e.Method.DeclaringType.Name, e.Method.Name, ex.Message));
                        }
                    })
                    .Combine(Spacing.Triple);

                if (command == null)
                    return null;

                return SqlPreCommand.Combine(Spacing.Double,
                    new SqlPreCommandSimple(Resources.StartOfSyncScriptGeneratedOn0.Formato(DateTime.Now)),
                    new SqlPreCommandSimple("use {0}".Formato(schemaName)),
                    command,
                    new SqlPreCommandSimple(Resources.EndOfSyncScript));
            }
        }

        public event Func<SqlPreCommand> Generating;
        internal SqlPreCommand GenerationScipt()
        {
            if (Generating == null)
                return null;

            using (Sync.ChangeBothCultures(ForceCultureInfo))
            {
                return Generating
                    .GetInvocationList()
                    .Cast<Func<SqlPreCommand>>()
                    .Select(e => e())
                    .Combine(Spacing.Triple);
            }
        }

        public class InitEventDictionary
        {
            Dictionary<InitLevel, InitEventHandler> dict = new Dictionary<InitLevel, InitEventHandler>();

            Dictionary<MethodInfo, long> times = new Dictionary<MethodInfo, long>();

            InitLevel? initLevel;

            public InitEventHandler this[InitLevel level]
            {
                get { return dict.TryGetC(level); }
                set
                {
                    int current = dict.TryGetC(level).TryCS(d => d.GetInvocationList().Length) ?? 0;
                    int @new = value.TryCS(d => d.GetInvocationList().Length) ?? 0;

                    if (Math.Abs(current - @new) > 1)
                        throw new InvalidOperationException("add or remove just one event handler each time");

                    dict[level] = value;
                }
            }

            public void InitializeUntil(InitLevel topLevel)
            {
                for (InitLevel current = initLevel + 1 ?? InitLevel.Level0SyncEntities; current <= topLevel; current++)
                {
                    InitializeJust(current);
                    initLevel = current;
                }
            }

            void InitializeJust(InitLevel currentLevel)
            {
                InitEventHandler h = dict.TryGetC(currentLevel);
                if (h == null)
                    return;

                var handlers = h.GetInvocationList().Cast<InitEventHandler>();

                foreach (InitEventHandler handler in handlers)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    handler();
                    sw.Stop();
                    times.Add(handler.Method, sw.ElapsedMilliseconds);
                }
            }

            public override string ToString()
            {
                return dict.OrderBy(a => a.Key).ToString(a => "{0} -> \r\n{1}".Formato(a.Key,
                    a.Value.GetInvocationList().Select(h => h.Method).ToString(mi =>
                        "\t{0}.{1}: {2}".Formato(mi.DeclaringType.TypeName(), mi.MethodName(), times.TryGetS(mi).TryToString("0 ms") ?? "Not Initialized"), "\r\n")), "\r\n\r\n");
            }
        }

        public InitEventDictionary Initializing = new InitEventDictionary();

        public void Initialize()
        {
            using (GlobalMode())
                Initializing.InitializeUntil(InitLevel.Level4BackgroundProcesses);
        }

        public void InitializeUntil(InitLevel level)
        {
            using (GlobalMode())
                Initializing.InitializeUntil(level);
        }


        #endregion

        static Schema()
        {
            PropertyRoute.SetFindImplementationsCallback(pr => Schema.Current.FindImplementations(pr));
        }

        internal Schema(SchemaSettings settings)
        {
            this.Settings = settings;

            Generating += Administrator.CreateTablesScript;
            Generating += Administrator.InsertEnumValuesScript;
            Generating += TypeLogic.Schema_Generating;


            Synchronizing += Administrator.SynchronizeSchemaScript;
            Synchronizing += Administrator.SynchronizeEnumsScript;
            Synchronizing += TypeLogic.Schema_Synchronizing;

            Initializing[InitLevel.Level0SyncEntities] += TypeLogic.Schema_Initializing;
            Initializing[InitLevel.Level0SyncEntities] += GlobalLazy.GlobalLazy_Initialize;
        }

        public static Schema Current
        {
            get { return Connector.Current.Schema; }
        }

        public Table Table<T>() where T : IdentifiableEntity
        {
            return Table(typeof(T));
        }

        public Table Table(Type type)
        {
            return Tables.GetOrThrow(type, "Table {0} not loaded in schema");
        }

        internal static Field FindField(IFieldFinder fieldFinder, MemberInfo[] members)
        {
            IFieldFinder current = fieldFinder;
            Field result = null;
            foreach (var mi in members)
            {
                if (current == null)
                    throw new InvalidOperationException("{0} does not implement {1}".Formato(result, typeof(IFieldFinder).Name));

                result = current.GetField(mi);

                current = result as IFieldFinder;
            }

            return result;
        }

        internal static Field TryFindField(IFieldFinder fieldFinder, MemberInfo[] members)
        {
            IFieldFinder current = fieldFinder;
            Field result = null;
            foreach (var mi in members)
            {
                if (current == null)
                    return null;

                result = current.TryGetField(mi);

                if (result == null)
                    return null;

                current = result as IFieldFinder;
            }

            return result;
        }

        public Dictionary<PropertyRoute, Implementations> FindAllImplementations(Type root)
        {
            if (!Tables.ContainsKey(root))
                return null;

            var table = Table(root);

            return PropertyRoute.GenerateRoutes(root).Select(r=> KVP.Create(r, FindImplementations(r))).Where(r=>r.Value != null).ToDictionary();
        }

        public Implementations FindImplementations(PropertyRoute route)
        {
            Type type = route.RootType;

            if (!Tables.ContainsKey(type))
                return null;

            Field field = TryFindField(Table(type), route.Members);

            FieldImplementedBy ibField = field as FieldImplementedBy;
            if (ibField != null)
                return new ImplementedByAttribute(ibField.ImplementationColumns.Keys.ToArray());

            FieldImplementedByAll ibaField = field as FieldImplementedByAll;
            if (ibaField != null)
                return new ImplementedByAllAttribute();

            return null;
        }

        /// <summary>
        /// Uses a lambda navigate in a strongly-typed way, you can acces field using the property and collections using Single().
        /// Nota: Haz el campo internal y añade [assembly:InternalsVisibleTo]
        /// </summary>
        public Field Field<T>(Expression<Func<T, object>> lambdaToField)
            where T : IdentifiableEntity
        {
            return FindField(Table(typeof(T)), Reflector.GetMemberList(lambdaToField));
        }

        public override string ToString()
        {
            return tables.Values.ToString(t => t.Type.TypeName(), "\r\n\r\n");
        }

        internal Dictionary<string, ITable> GetDatabaseTables()
        {
            return Schema.Current.Tables.Values.SelectMany(t =>
                t.Fields.Values.Select(a => a.Field).OfType<FieldMList>().Select(f => (ITable)f.RelationalTable).PreAnd(t))
                .ToDictionary(a => a.Name);
        }

        public DirectedEdgedGraph<Table, bool> ToDirectedGraph()
        {
            return DirectedEdgedGraph<Table, bool>.Generate(Tables.Values, t => t.DependentTables());
        }

        ThreadLocal<bool> inGlobalMode = new ThreadLocal<bool>(() => false);
        public bool InGlobalMode
        {
            get { return inGlobalMode.Value; }
        }

        public IDisposable GlobalMode()
        {
            var oldValue = inGlobalMode.Value; 
            inGlobalMode.Value = true;
            return new Disposable(() => inGlobalMode.Value = oldValue);
        }
    }

    internal interface IEntityEvents
    {
        void OnPreSaving(IdentifiableEntity entity, ref bool graphModified);
        void OnSaving(IdentifiableEntity entity);
        void OnRetrieved(IdentifiableEntity entity);

        ICacheController CacheController { get; }

        bool HasQueryFilter { get; }
    }

    public interface ICacheController
    {
        bool Enabled { get; }
        bool IsComplete { get; }
        void Load();

        IEnumerable<int> GetAllIds();

        event Action Invalidation;

        bool CompleteCache(IdentifiableEntity entity, IRetriever retriver);

        string GetToString(int id);
    }

    public abstract class CacheController<T> : ICacheController 
        where T : IdentifiableEntity
    {
        public abstract bool Enabled { get; }
        public abstract bool IsComplete { get; }
        public abstract void Load();

        public abstract IEnumerable<int> GetAllIds();
        public abstract event Action Invalidation;

        bool ICacheController.CompleteCache(IdentifiableEntity entity, IRetriever retriver)
        {
            return CompleteCache((T)entity, retriver);
        }

        public abstract bool CompleteCache(T entity, IRetriever retriver);

        public abstract string GetToString(int id);
    }

    public class EntityEvents<T> : IEntityEvents
        where T : IdentifiableEntity
    {
        public event PreSavingEventHandler<T> PreSaving;
        public event SavingEventHandler<T> Saving;

        public event RetrievedEventHandler<T> Retrieved;

        public CacheController<T> CacheController { get; set; }

        public event FilterQueryEventHandler<T> FilterQuery;

        public event QueryHandler<T> PreUnsafeDelete;

        public event QueryHandler<T> PreUnsafeUpdate;

        internal IQueryable<T> OnFilterQuery(IQueryable<T> query)
        {
            if (FilterQuery != null)
                foreach (FilterQueryEventHandler<T> filter in FilterQuery.GetInvocationList())
                    query = filter(query);

            return query;
        }

        public bool HasQueryFilter
        {
            get { return FilterQuery != null; }
        }

        internal void OnPreUnsafeDelete(IQueryable<T> query)
        {
            if (PreUnsafeDelete != null)
                foreach (QueryHandler<T> action in PreUnsafeDelete.GetInvocationList().Reverse())
                    action(query);
        }

        internal void OnPreUnsafeUpdate(IQueryable<T> query)
        {
            if (PreUnsafeUpdate != null)
                foreach (QueryHandler<T> action in PreUnsafeUpdate.GetInvocationList().Reverse())
                    action(query);

        }

        void IEntityEvents.OnPreSaving(IdentifiableEntity entity, ref bool graphModified)
        {
            if (PreSaving != null)
                PreSaving((T)entity, ref graphModified);
        }

        void IEntityEvents.OnSaving(IdentifiableEntity entity)
        {
            if (Saving != null)
                Saving((T)entity);

        }

        void IEntityEvents.OnRetrieved(IdentifiableEntity entity)
        {
            if (Retrieved != null)
                Retrieved((T)entity);
        }

        ICacheController IEntityEvents.CacheController
        {
            get { return CacheController; }
        }
    }

    public delegate void PreSavingEventHandler<T>(T ident, ref bool graphModified) where T : IdentifiableEntity;
    public delegate void RetrievedEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavingEventHandler<T>(T ident) where T : IdentifiableEntity;
    public delegate void SavedEventHandler<T>(T ident, SavedEventArgs args) where T : IdentifiableEntity;
    public delegate IQueryable<T> FilterQueryEventHandler<T>(IQueryable<T> query);

    public delegate void QueryHandler<T>(IQueryable<T> query);

    public delegate void InitEventHandler();
    public delegate void SyncEventHandler();
    public delegate void GenSchemaEventHandler();

    public class SavedEventArgs
    {
        public bool IsRoot { get; set; }
        public bool WasNew { get; set; }
        public bool WasModified { get; set; }
    }
}
