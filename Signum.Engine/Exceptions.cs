using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using System.Data.SqlServerCe;
using System.Collections.Concurrent;
using System.Reflection;

namespace Signum.Engine.Exceptions
{
    [Serializable]
    public class UniqueKeyException : ApplicationException
    {
        public string TableName { get; private set; }
        public Table Table { get; private set; }

        public string IndexName { get; private set; }
        public UniqueIndex Index { get; private set; }
        public List<PropertyInfo> Properties { get; private set; }

        public string Values { get; private set; }

        protected UniqueKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        static Regex[] regexes = new []
        {   
            new Regex(@"Cannot insert duplicate key row in object '(?<table>.*)' with unique index '(?<index>.*)'\. The duplicate key value is \((?<value>.*)\)")
        };

        public UniqueKeyException(Exception inner) : base(null, inner) 
        {
            foreach (var rx in regexes)
            {
                Match m = rx.Match(inner.Message);
                if (m.Success)
                {
                    TableName = m.Groups["table"].Value;
                    IndexName = m.Groups["index"].Value;
                    Values = m.Groups["value"].Value;

                    Table = cachedTables.GetOrAdd(TableName, tn=>Schema.Current.Tables.Values.FirstOrDefault(t => t.Name.ToStringDbo() == tn));

                    if(Table != null)
                    {
                        var tuple = cachedLookups.GetOrAdd(Tuple.Create(Table, IndexName), tup=>
                        {
                            var index = tup.Item1.GeneratAllIndexes().OfType<UniqueIndex>().FirstOrDefault(ix => ix.IndexName == tup.Item2);

                            if(index == null)
                                return null;

                            var properties = (from f in tup.Item1.Fields.Values
                                              let cols = f.Field.Columns()
                                              where cols.Any() && cols.All(index.Columns.Contains)
                                              select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().ToList();

                            if (properties.IsEmpty())
                                return null;

                            return Tuple.Create(index, properties); 
                        });
 
                        if(tuple != null)
                        {
                            Index = tuple.Item1;
                            Properties = tuple.Item2;
                        }
                    }
                }
            }
        }

        static ConcurrentDictionary<string, Table> cachedTables = new ConcurrentDictionary<string, Table>();
        static ConcurrentDictionary<Tuple<Table, string>, Tuple<UniqueIndex, List<PropertyInfo>>> cachedLookups = new ConcurrentDictionary<Tuple<Table, string>, Tuple<UniqueIndex, List<PropertyInfo>>>();

        public override string Message
        {
            get
            {
                if (Table == null)
                    return InnerException.Message;

                return EngineMessage.TheresAlreadyA0With1EqualsTo2.NiceToString().ForGenderAndNumber(Table == null? null: Table.Type.GetGender()).Formato(
                    Table == null ? TableName : Table.Type.NiceName(),
                    Index == null ? IndexName :
                    Properties.IsNullOrEmpty() ? Index.Columns.CommaAnd(c => c.Name) : 
                    Properties.CommaAnd(p=>p.NiceName()),
                    Values);
            }
        }
    }

  
    [Serializable]
    public class ForeignKeyException : ApplicationException
    {
        public string TableName { get; private set; }
        public string Field { get; private set; }
        public Type TableType { get; private set; }

        public string ReferedTableName { get; private set; }
        public Type ReferedTableType { get; private set; }

        public bool IsInsert { get; private set; }

        static Regex indexRegex = new Regex(@"""FK_(?<table>[^_]+)_(?<field>[^_""]+)""");

        static Regex referedTable = new Regex(@"table ""(?<referedTable>.+?)""");

        protected ForeignKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
       
        public ForeignKeyException(Exception inner) : base(null, inner) 
        {
            Match m = indexRegex.Match(inner.Message);
            
            if (m.Success)
            {
                TableName = m.Groups["table"].Value;
                Field = m.Groups["field"].Value;
                TableType = Schema.Current.Tables
                    .Where(kvp => kvp.Value.Name.Name == TableName)
                    .Select(p => p.Key)
                    .SingleOrDefaultEx();
            }

            if(inner.Message.Contains("INSERT"))
            {
                IsInsert = true;

                Match m2 = referedTable.Match(inner.Message);
                if (m2.Success)
                {
                    ReferedTableName = m2.Groups["referedTable"].Value.Split('.').Last();
                    ReferedTableType = Schema.Current.Tables
                                    .Where(kvp => kvp.Value.Name.Name == ReferedTableName)
                                    .Select(p => p.Key)
                                    .SingleOrDefaultEx();

                    ReferedTableType = EnumEntity.Extract(ReferedTableType) ?? ReferedTableType; 
                }
            }
        }

        public override string Message
        {
            get
            {
                if (TableName == null)
                    return InnerException.Message;

                if (IsInsert)
                    return (TableType == null || ReferedTableType == null) ?
                        "The column {0} on table {1} does not reference {2}".Formato(Field, TableName, ReferedTableName) :
                        "The column {0} of the {1} does not refer to a valid {2}".Formato(Field, TableType.NiceName(), ReferedTableType.NiceName());
                else
                    return (TableType == null) ?
                        EngineMessage.ThereAreRecordsIn0PointingToThisTableByColumn1.NiceToString().Formato(TableName, Field) :
                        EngineMessage.ThereAre0ThatReferThisEntity.NiceToString().Formato(TableType.NicePluralName());
            }
        }
    }


    [Serializable]
    public class EntityNotFoundException : Exception
    {
        public Type Type { get; private set; }
        public int[] Ids { get; private set; }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EntityNotFoundException(Type type, params int[] ids)
            : base(EngineMessage.EntityWithType0AndId1NotFound.NiceToString().Formato(type.Name, ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }

    [Serializable]
    public class ConcurrencyException: Exception
    {
        public Type Type { get; private set; }
        public int[] Ids { get; private set; }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ConcurrencyException(Type type, params int[] ids)
            : base(EngineMessage.ConcurrencyErrorOnDatabaseTable0Id1.NiceToString().Formato(type.NiceName(), ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }
}
