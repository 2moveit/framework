﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Data;
using Signum.Entities;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Diagnostics;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;
using System.Linq.Expressions;
using System.Runtime.Remoting.Contexts;

namespace Signum.Engine.Maps
{
    public class SchemaBuilder
    {
        Schema schema;
        public SchemaSettings Settings
        {
            get { return schema.Settings; }
        }

        public SchemaBuilder() : this(DBMS.SqlServer2005)
        {

        }

        public SchemaBuilder(DBMS dbms)
        {
            schema = new Schema(new SchemaSettings(dbms));
            Include<TypeDN>();
        }

        protected SchemaBuilder(Schema schema)
        {
            this.schema = schema; 
        }

        public SchemaBuilder(SchemaSettings settings)
        {
            schema = new Schema(settings);
        }
       
        public Schema Schema
        {
            get { return schema; }
        }

    
        public void AddUniqueIndex<T>(Expression<Func<T, object>> fields) where T : IdentifiableEntity
        {
            AddUniqueIndex<T>(fields, null); 
        }

        public void AddUniqueIndex<T>(Expression<Func<T, object>> fields, Expression<Func<T, object>> fieldsAllowRepeatedNull) where T : IdentifiableEntity
        {
            Schema schema = Schema.Current;

            Expression<Func<T, object>>[] fieldLambdas = Split(fields);
            Expression<Func<T, object>>[] fieldsNotNullLambdas = Split(fieldsAllowRepeatedNull);

            Field[] colFields = fieldLambdas.Select(fun => schema.Field<T>(fun)).ToArray();
            Field[] colFieldsNotNull = fieldsNotNullLambdas.Select(fun => schema.Field<T>(fun)).ToArray();

            AddUniqueIndex(new UniqueIndex(schema.Table<T>(), colFields).WhereNotNull(colFieldsNotNull));
        }
        public void AddUniqueIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList, Expression<Func<MListElement<T, V>, object>> fields)
           where T : IdentifiableEntity
        {
            AddUniqueIndexMList(toMList, fields, null); 
        }

        public void AddUniqueIndexMList<T, V>(Expression<Func<T, MList<V>>> toMList, Expression<Func<MListElement<T, V>, object>> fields, Expression<Func<MListElement<T, V>, object>> fieldsNotNull)
            where T : IdentifiableEntity
        {
            Schema schema = Schema.Current;

            Expression<Func<MListElement<T, V>, object>>[] fieldLambdas = Split(fields);
            Expression<Func<MListElement<T, V>, object>>[] fieldsNotNullLambdas = Split(fieldsNotNull);

            RelationalTable table = ((FieldMList)Schema.FindField(schema.Table(typeof(T)), Reflector.GetMemberList(toMList))).RelationalTable;

            Field[] colFields = fieldLambdas.Select(fun => Schema.FindField(table,  Reflector.GetMemberList(fun))).ToArray();
            Field[] colFieldsNotNull = fieldsNotNullLambdas.Select(fun => Schema.FindField(table,  Reflector.GetMemberList(fun))).ToArray();

            AddUniqueIndex(table, colFields, colFieldsNotNull);
        }

        Expression<Func<T, object>>[] Split<T>(Expression<Func<T, object>> columns)
        {
            if (columns == null)
                return new Expression<Func<T, object>>[0];

            if (columns.Body.NodeType == ExpressionType.New)
            {
                return ((NewExpression)columns.Body).Arguments
                    .Select(a => Expression.Lambda<Func<T, object>>(Expression.Convert(a, typeof(object)), columns.Parameters))
                    .ToArray();
            }

            return new[] { columns };
        }

        public void AddUniqueIndex(ITable table, Field[] fields, Field[] notNullFields)
        {
            AddUniqueIndex(new UniqueIndex(table, fields).WhereNotNull(notNullFields));
        }

        public void AddUniqueIndex(ITable table, IColumn[] columns, IColumn[] notNullColumns)
        {
            AddUniqueIndex(new UniqueIndex(table, columns).WhereNotNull(notNullColumns));
        }

        private void AddUniqueIndex(UniqueIndex uniqueIndex)
        {
            ITable table = uniqueIndex.Table;

            if (table.MultiIndexes == null)
                table.MultiIndexes = new List<UniqueIndex>();

            table.MultiIndexes.Add(uniqueIndex);
        }

        public Table Include<T>() where T : IdentifiableEntity
        {
            return Include(typeof(T), null);
        }

        public virtual Table Include(Type type)
        {
            return Include(type, null);
        }

        internal protected virtual Table Include(Type type, PropertyRoute route)
        {
            Table result;
            if (!schema.Tables.TryGetValue(type, out result))
            {
                if (type.IsAbstract)
                    throw new InvalidOperationException(route.TryCC(r => "Error on field {0}: ".Formato(r)) + "Impossible to include in the Schema the type {0} because is abstract".Formato(type));

                if (!Reflector.IsIdentifiableEntity(type))
                    throw new InvalidOperationException(route.TryCC(r => "Error on field {0}: ".Formato(r)) + "Impossible to include in the Schema the type {0} because is not and IdentifiableEntity".Formato(type));

                result = new Table(type);

                schema.Tables.Add(type, result);

                string name = schema.Settings.desambiguatedNames.TryGetC(type) ?? Reflector.CleanTypeName(Reflector.ExtractEnumProxy(type) ?? type);

                if (schema.NameToType.ContainsKey(name))
                    throw new InvalidOperationException(route.TryCC(r => "Error on field {0}: ".Formato(r)) + "Two types have the same cleanName, desambiguate using Schema.Current.Settings.Desambiguate method: \r\n {0}\r\n {1}".Formato(schema.NameToType[name].FullName, type.FullName)); 

                schema.NameToType[name] = type;
                schema.TypeToName[type] = name;

                Complete(result);
            }
            return result;
        }

        void Complete(Table table)
        {
            Type type = table.Type;
            table.Identity = Reflector.ExtractEnumProxy(type) == null;
            table.Name = GenerateTableName(type);
            table.CleanTypeName = GenerateCleanTypeName(type);
            table.Fields = GenerateFields(PropertyRoute.Root(type), Contexts.Normal, table, NameSequence.Void, false);
            table.GenerateColumns();
        }

        HashSet<string> loadedModules = new HashSet<string>();
        public bool NotDefined(MethodBase methodBase)
        {
            return loadedModules.Add(methodBase.DeclaringType.FullName + "." + methodBase.Name); 
        }

        public void AssertDefined(MethodBase methodBase)
        {
            string name = methodBase.DeclaringType.FullName + "." + methodBase.Name;

            if (!loadedModules.Contains(name))
                throw new ApplicationException("Call {0} first".Formato(name)); 
        }

        #region Field Generator
        protected Dictionary<string, EntityField> GenerateFields(PropertyRoute root, Contexts contexto, Table table, NameSequence preName, bool forceNull)
        {
            Dictionary<string, EntityField> result = new Dictionary<string, EntityField>();
            var type = root.Type;

            foreach (FieldInfo fi in Reflector.InstanceFieldsInOrder(type))
            {
                PropertyRoute route = root.Add(fi); 

                if (!Settings.Attributes(route).Any(a=>a is IgnoreAttribute))
                {
                    if (!SilentMode() && Reflector.TryFindPropertyInfo(fi) == null)
                        Debug.WriteLine("Field {0} of type {1} has no property".Formato(fi.Name, type.Name));

                    Field campo = GenerateField(route, contexto, table, preName, forceNull);

                    if (result.ContainsKey(fi.Name))
                        throw new InvalidOperationException("Duplicated field with name {0} on {1}, shadowing not supported".Formato(fi.Name, type.TypeName()));

                    result.Add(fi.Name, new EntityField(type, fi) { Field = campo });
                }
            }
            return result;
        }

        protected virtual bool SilentMode()
        {
            return schema.SilentMode; 
        }

        protected virtual Field GenerateField(PropertyRoute route, Contexts context, Table table, NameSequence preName, bool forceNull)
        {
            //fieldType: Va variando segun se entra en colecciones o contenidos
            //fi.Type: el tipo del campo asociado

            KindOfField kof = GetKindOfField(route).ThrowIfNullS("Field {0} of type {1} has no database representation".Formato(route, route.Type.Name));

            if ((allowedContexts[kof] & context) != context)
                throw new InvalidOperationException("Field {0} of Type {1} should be mapped as {2} but is incompatible with context {3}".Formato(route, route.Type.Name, kof, context));

            //field name generation 
            NameSequence name = preName;
            if (context == Contexts.Normal || context == Contexts.Embedded || context == Contexts.View)
                name = name.Add(GenerateFieldName(route, kof));
            else if (context == Contexts.MList && (kof == KindOfField.Enum || kof == KindOfField.Reference))
                name = name.Add(GenerateFieldName(Reflector.ExtractLite(route.Type) ?? route.Type, kof));

            switch (kof)
            {
                case KindOfField.PrimaryKey:
                    return GenerateFieldPrimaryKey(route, table, name);
                case KindOfField.Value:
                    return GenerateFieldValue(route, name, forceNull);
                case KindOfField.Reference:
                    {
                        Implementations at = Settings.GetImplementations(route);
                        if (at == null)
                            return GenerateFieldReference(route, name, forceNull);
                        else if (at is ImplementedByAttribute)
                            return GenerateFieldImplmentedBy(route, name, forceNull, (ImplementedByAttribute)at);
                        else
                            return GenerateFieldImplmentedByAll(route, name, forceNull, (ImplementedByAllAttribute)at);
                    }
                case KindOfField.Enum:
                    return GenerateFieldEnum(route, name, forceNull);
                case KindOfField.Embedded:
                    return GenerateFieldEmbedded(route, name, forceNull);
                case KindOfField.MList:
                    return GenerateFieldMList(route, table, name);
                default:
                    throw new NotSupportedException(Resources.NoWayOfMappingType0Found.Formato(route.Type));
            }
        }

        static Dictionary<KindOfField, Contexts> allowedContexts = new Dictionary<KindOfField, Contexts>()
        {
            {KindOfField.PrimaryKey,    Contexts.Normal },
            {KindOfField.Value,         Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Reference,     Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Enum,          Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.Embedded,      Contexts.Normal | Contexts.MList | Contexts.Embedded | Contexts.View },
            {KindOfField.MList,         Contexts.Normal },
        };

        private KindOfField? GetKindOfField(PropertyRoute route)
        {
            if (route.FieldInfo != null && route.FieldInfo.FieldEquals((IdentifiableEntity ie) => ie.id))
                return KindOfField.PrimaryKey;

            if (Settings.GetSqlDbType(route) != null)
                return KindOfField.Value;

            if (route.Type.UnNullify().IsEnum)
                return KindOfField.Enum;

            if (Reflector.IsIIdentifiable(Reflector.ExtractLite(route.Type) ?? route.Type))
                return KindOfField.Reference;

            if (Reflector.IsEmbeddedEntity(route.Type))
                return KindOfField.Embedded;

            if (Reflector.IsMList(route.Type))
                return KindOfField.MList;

            return null;
        }

        private static Field GenerateFieldPrimaryKey(PropertyRoute route, Table table, NameSequence name)
        {
            return new FieldPrimaryKey(route.Type, table);
        }

        protected virtual Field GenerateFieldValue(PropertyRoute route, NameSequence name, bool forceNull)
        {
            SqlDbType sqlDbType = Settings.GetSqlDbType(route).Value;

            return new FieldValue(route.Type)
            {
                Name = name.ToString(),
                SqlDbType = sqlDbType,
                Nullable = Settings.IsNullable(route, forceNull),
                Size = Settings.GetSqlSize(route, sqlDbType),
                Scale = Settings.GetSqlScale(route, sqlDbType),
                IndexType = Settings.GetIndexType(route)
            };
        }

        protected virtual Field GenerateFieldEnum(PropertyRoute route, NameSequence name, bool forceNull)
        {
            Type cleanEnum = route.Type.UnNullify();

            var table = Include(Reflector.GenerateEnumProxy(cleanEnum), route);

            return new FieldEnum(route.Type)
            {
                Name = name.ToString(),
                Nullable = Settings.IsNullable(route, forceNull),
                IsLite = false,
                IndexType = Settings.GetIndexType(route),
                ReferenceTable = cleanEnum.HasAttribute<FlagsAttribute>() && !route.FieldInfo.HasAttribute<ForceForeignKey>() ? null : table,
            };
        }

        protected virtual Field GenerateFieldReference(PropertyRoute route, NameSequence name, bool forceNull)
        {
            return new FieldReference(route.Type)
            {
                Name = name.ToString(),
                IndexType = Settings.GetIndexType(route),
                Nullable = Settings.IsNullable(route, forceNull),
                IsLite  = route.Type.IsLite(),
                ReferenceTable = Include(Reflector.ExtractLite(route.Type) ?? route.Type, route),
            };
        }

        protected virtual Field GenerateFieldImplmentedBy(PropertyRoute route, NameSequence name, bool forceNull, ImplementedByAttribute ib)
        {
            Type cleanType = Reflector.ExtractLite(route.Type) ?? route.Type;
            string erroneos = ib.ImplementedTypes.Where(t => !cleanType.IsAssignableFrom(t)).ToString(t => t.TypeName(), ", ");
            if (erroneos.Length != 0)
                throw new InvalidOperationException("Type {0} do not implement {1}".Formato(erroneos, cleanType));

            bool nullable = Settings.IsNullable(route, forceNull) || ib.ImplementedTypes.Length > 1;

            return new FieldImplementedBy(route.Type)
            {
                IndexType = Settings.GetIndexType(route),
                ImplementationColumns = ib.ImplementedTypes.ToDictionary(t => t, t => new ImplementationColumn
                {
                    ReferenceTable = Include(t, route),
                    Name = name.Add(TypeLogic.GetCleanName(t)).ToString(),
                    Nullable = nullable,
                }),
                IsLite = route.Type.IsLite()
            };
        }

        protected virtual Field GenerateFieldImplmentedByAll(PropertyRoute route, NameSequence preName, bool forceNull, ImplementedByAllAttribute iba)
        {
            bool nullable = Settings.IsNullable(route, forceNull);

            return new FieldImplementedByAll(route.Type)
            {
                IndexType = Settings.GetIndexType(route),
                Column = new ImplementationColumn
                {
                    Name = preName.ToString(),
                    Nullable = nullable,
                    ReferenceTable = null,
                },
                ColumnTypes = new ImplementationColumn
                {
                    Name = preName.Add("Type").ToString(),
                    Nullable = nullable,
                    ReferenceTable = Include(typeof(TypeDN), route)
                },
                IsLite = route.Type.IsLite()
            };
        }

        protected virtual Field GenerateFieldMList(PropertyRoute route, Table table, NameSequence name)
        {
            Type elementType = route.Type.ElementType();

            Type type = route.Parent.Type;

            RelationalTable relationalTable = new RelationalTable(route.Type)
            {
                Name = GenerateTableNameCollection(type, name),
                BackReference = new FieldReference(table.Type)
                {
                    Name = GenerateBackReferenceName(type),
                    ReferenceTable = table
                },
                PrimaryKey = new RelationalTable.PrimaryKeyColumn(),
                Field = GenerateField(route.Add("Item"), Contexts.MList, null, NameSequence.Void, false) 
            };

            relationalTable.GenerateColumns(); 

            return new FieldMList(route.Type)
            {
                RelationalTable = relationalTable,
            };
        }

        protected virtual Field GenerateFieldEmbedded(PropertyRoute route, NameSequence name, bool forceNull)
        {
            bool nullable = Settings.IsNullable(route, false);

            return new FieldEmbedded(route.Type)
            {
                HasValue = nullable ? new FieldEmbedded.EmbeddedHasValueColumn() { Name = name.Add("HasValue").ToString() } : null,
                EmbeddedFields = GenerateFields(route, Contexts.Embedded, null, name, nullable || forceNull)
            };
        }
        #endregion

        #region Names

        public virtual string GenerateTableName(Type type)
        {
            return CleanType(type).Name;
        }

        public virtual string GenerateCleanTypeName(Type type)
        {
            type = CleanType(type);

            CleanTypeNameAttribute ctn = type.SingleAttribute<CleanTypeNameAttribute>();
            if (ctn != null)
                return ctn.Name;

            return Reflector.CleanTypeName(type);
        }

        protected static Type CleanType(Type type)
        {
            type = Reflector.ExtractLite(type) ?? type;
            type = Reflector.ExtractEnumProxy(type) ?? type;
            return type;
        }

        public virtual string GenerateTableNameCollection(Type type, NameSequence name)
        {
            return CleanType(type).Name + name.ToString();
        }

        public virtual string GenerateFieldName(Type type, KindOfField kindOfField)
        {
            switch (kindOfField)
            {
                case KindOfField.Value:
                case KindOfField.Embedded:
                    return type.Name.FirstUpper();
                case KindOfField.Enum:
                case KindOfField.Reference:
                    return "id" + CleanType(type).Name;
                default:
                    throw new NotImplementedException("No field name for type {0} defined".Formato(type));
            }
        }

        public virtual string GenerateFieldName(PropertyRoute route, KindOfField tipoCampo)
        {
            string name = Reflector.CleanFieldName(route.FieldInfo.Name);

            switch (tipoCampo)
            {
                case KindOfField.PrimaryKey:
                case KindOfField.Value:
                case KindOfField.Embedded:
                case KindOfField.MList:  //se usa solo para el nombre de la tabla 
                    return name;
                case KindOfField.Reference:
                case KindOfField.Enum:
                    return "id" + name;
                default:
                    throw new NotImplementedException("No name for {0} defined".Formato(route.FieldInfo.Name));
            }
        }

        public virtual string GenerateBackReferenceName(Type type)
        {
            return "idParent";
        }
        #endregion
    }

    internal class ViewBuilder : SchemaBuilder
    {
        public ViewBuilder(Schema schema) : base(schema)
        {
        }

        public override Table Include(Type type)
        {
            return Schema.Table(type);
        }

        protected override bool SilentMode()
        {
            return true;
        }

        public Table NewView(Type type)
        {
            Table table = new Table(type)
            {
                Name = GenerateTableName(type),
                IsView = true
            };

            table.Fields = GenerateFields(PropertyRoute.Root(type), Contexts.View, table, NameSequence.Void, false);

            return table;
        }

        public override string GenerateTableName(Type type)
        {
            SqlViewNameAttribute vn = type.SingleAttribute<SqlViewNameAttribute>();
            if (vn != null)
                return vn.Name;

            return CleanType(type).Name;
        }

        public override string GenerateFieldName(PropertyRoute route, KindOfField kindOfField)
        {
            SqlViewColumnAttribute vc = route.FieldInfo.SingleAttribute<SqlViewColumnAttribute>();
            if (vc != null)
                return vc.Name;

            return base.GenerateFieldName(route, kindOfField);
        }

        public override string GenerateFieldName(Type type, KindOfField tipoCampo)
        {
            return base.GenerateFieldName(type, tipoCampo);
        }
    }

}
