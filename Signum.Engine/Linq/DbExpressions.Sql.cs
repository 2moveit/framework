﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Signum.Utilities;
using System.Reflection;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using Signum.Entities.DynamicQuery;
using System.Data;


namespace Signum.Engine.Linq
{

    /// <summary>
    /// Extended node types for custom expressions
    /// </summary>
    internal enum DbExpressionType
    {
        Table = 1000, // make sure these don't overlap with ExpressionType
        Column,
        Select,
        Projection,
        ChildProjection, 
        Join,
        Aggregate,
        AggregateSubquery,
        SqlFunction,
        SqlConstant,
        SqlEnum,
        SqlCast,
        Case, 
        RowNumber,
        Like,
        In,
        Exists,
        Scalar,
        IsNull,
        IsNotNull,
        Update,
        Delete, 
        CommandAggregate,
        SelectRowCount,
        FieldInit = 2000,
        EmbeddedFieldInit,
        ImplementedBy,
        ImplementedByAll,
        LiteReference,
        MList,
        MListElement,
    }

    class Alias
    {
        public readonly string Name;  

        Alias(string name)
        {
            this.Name = name;
        }

        [ThreadStatic]
        static AliasGenerator current;

        public static Alias Raw(string name)
        {
            return new Alias(name);
        }

        public static Alias NextTableAlias(string tableName)
        {
            string abv = new string(tableName.Where(c => char.IsUpper(c)).ToArray());

            return current.GetUniqueAlias(abv);
        }

        public static Alias CloneAlias(Alias alias)
        {
            return current.GetUniqueAlias(alias.Name + "b");
        }

        public static Alias NextSelectAlias()
        {
            return current.NextSelectAlias();
        }

        public static Alias GetUniqueAlias(string baseAlias)
        {
            return current.GetUniqueAlias(baseAlias);
        }

        public static IDisposable NewGenerator()
        {
            return current = new AliasGenerator();
        }

        class AliasGenerator: IDisposable
        {
            HashSet<string> usedAliases = new HashSet<string>();

            int selectAliasCount = 0;
            public Alias NextSelectAlias()
            {
                return GetUniqueAlias("s" + (selectAliasCount++));
            }

            public Alias GetUniqueAlias(string baseAlias)
            {
                if (usedAliases.Add(baseAlias))
                    return new Alias(baseAlias);

                for (int i = 1; ; i++)
                {
                    string alias = baseAlias + i;

                    if (usedAliases.Add(alias))
                        return new Alias(alias);

                }
            }

            public void Dispose()
            {
                current = null;
            }
        }
    }

    internal abstract class DbExpression : Expression
    {
        readonly Type type;
        public override Type Type
        {
            get { return type; }
        }

        readonly DbExpressionType nodeType;
        public override ExpressionType NodeType
        {
            get { return (ExpressionType)nodeType; }
        }

        protected DbExpression(DbExpressionType nodeType, Type type)
        {
            this.type = type;
            this.nodeType = nodeType;
        }

        public abstract override string ToString();
    }

    internal abstract class SourceExpression : DbExpression
    {
        public abstract Alias[] KnownAliases { get; }

        public SourceExpression(DbExpressionType nodeType)
            : base(nodeType, typeof(void))
        {
        }
    }

    internal abstract class SourceWithAliasExpression: SourceExpression
    {
        public readonly Alias Alias;

        public SourceWithAliasExpression(DbExpressionType nodeType, Alias alias)
            : base(nodeType)
        {
            this.Alias = alias;
        }
    }


    /// <summary>
    /// A custom expression node that represents a table reference in a SQL query
    /// </summary>
    internal class TableExpression : SourceWithAliasExpression
    {
        public readonly string Name;

        public override Alias[] KnownAliases
        {
            get { return new[] { Alias }; }
        }

        internal TableExpression(Alias alias, string name)
            : base(DbExpressionType.Table, alias)
        {
            this.Name = name;
        }

        public override string ToString()
        {
            return "{0} as {1}".Formato(Name, Alias);
        }
    }

    /// <summary>
    /// A custom expression node that represents a reference to a column in a SQL query
    /// </summary>
    internal class ColumnExpression : DbExpression, IEquatable<ColumnExpression>
    {
        public readonly Alias Alias;
        public readonly string Name;

        internal ColumnExpression(Type type, Alias alias, string name)
            : base(DbExpressionType.Column, type)
        {
            if (alias == null)
                throw new ArgumentNullException("alias");

            if (name == null)
                throw new ArgumentNullException("name"); 

            this.Alias = alias;
            this.Name = name;
        }

        public override string ToString()
        {
            return "{0}.{1}".Formato(Alias, Name);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ColumnExpression);
        }

        public bool Equals(ColumnExpression other)
        {
            return other != null && other.Alias == Alias && other.Name == Name; 
        }

        public override int GetHashCode()
        {
            return Alias.GetHashCode() ^ Name.GetHashCode();
        }
    }

    /// <summary>
    /// A declaration of a column in a SQL SELECT expression
    /// </summary>
    internal class ColumnDeclaration
    {
        public readonly string Name;
        public readonly Expression Expression;
        internal ColumnDeclaration(string name, Expression expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (name == null) throw new ArgumentNullException("name");

            this.Name = name;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return Name.HasText()? "{0} AS {1}".Formato(Expression.NiceToString(), Name):
                                   Expression.NiceToString();
        }

        public ColumnExpression GetReference(Alias alias)
        {
            return new ColumnExpression(Expression.Type, alias, Name); 
        }
    }

    internal enum AggregateFunction
    {
        Average,
        Count,
        Min,
        Max,
        Sum,
    }

    internal class AggregateExpression : DbExpression
    {
        public readonly Expression Source;
        public readonly AggregateFunction AggregateFunction;
        public AggregateExpression(Type type, Expression source, AggregateFunction aggregateFunction)
            : base(DbExpressionType.Aggregate, type)
        {
            if (source == null && aggregateFunction != AggregateFunction.Count) throw new ArgumentNullException("source");

            this.Source = source;
            this.AggregateFunction = aggregateFunction;
        }

        public override string ToString()
        {
            return "{0}({1})".Formato(AggregateFunction, Source.NiceToString() ?? "*");
        }
    }

    /// <summary>
    /// A pairing of an expression and an order type for use in a SQL Order By clause
    /// </summary>
    internal class OrderExpression
    {
        public readonly OrderType OrderType;
        public readonly Expression Expression;

        internal OrderExpression(OrderType orderType, Expression expression)
        {
            if (expression == null) throw new ArgumentNullException("expression");

            this.OrderType = orderType;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(Expression.NiceToString(), OrderType == OrderType.Ascending ? "ASC": "DESC");
        }
    }

    [Flags]
    internal enum SelectRoles
    {
        Where = 1,
        Aggregate = 2,
        GroupBy = 4,
 	    Reverse = 8,
        OrderBy = 16,
        Select = 32,
        Distinct = 64,
        Top = 128
    }
    /// <summary>
    /// A custom expression node used to represent a SQL SELECT expression
    /// </summary>
    internal class SelectExpression : SourceWithAliasExpression
    {
        public readonly ReadOnlyCollection<ColumnDeclaration> Columns;
        public readonly SourceExpression From;
        public readonly Expression Where;
        public readonly ReadOnlyCollection<OrderExpression> OrderBy;
        public readonly ReadOnlyCollection<Expression> GroupBy;
        public readonly Expression Top;
        public readonly bool Distinct;
        public readonly bool Reverse;

        readonly Alias[] knownAliases;
        public override Alias[] KnownAliases
        {
            get { return knownAliases; }
        }

        internal SelectExpression(Alias alias, bool distinct, bool reverse, Expression top, IEnumerable<ColumnDeclaration> columns, SourceExpression from, Expression where, IEnumerable<OrderExpression> orderBy, IEnumerable<Expression> groupBy)
            : base(DbExpressionType.Select, alias)
        {
            this.Distinct = distinct;
            this.Reverse = reverse;
            this.Top = top;
            this.Columns = columns.ToReadOnly();
            this.From = from;
            this.Where = where;
            this.OrderBy = orderBy.ToReadOnly();
            this.GroupBy = groupBy.ToReadOnly();
            this.knownAliases = from == null ? new[] { alias } : from.KnownAliases.And(alias).ToArray(); 
        }

        internal SelectRoles SelectRoles
        {
            get 
            {
                SelectRoles roles = (SelectRoles)0;

                if (Where != null)
                    roles |= SelectRoles.Where;

                if (GroupBy != null && GroupBy.Count > 0)
                    roles |= SelectRoles.GroupBy;
                else if (Columns.Any(cd => AggregateFinder.HasAggregates(cd.Expression)))
                    roles |= SelectRoles.Aggregate;

 				if (Reverse)
                    roles |= SelectRoles.Reverse;

                if (OrderBy != null && OrderBy.Count > 0)
                    roles |= SelectRoles.OrderBy;

                if (!Columns.All(cd => cd.Expression is ColumnExpression))
                    roles |= SelectRoles.Select;

                if(Distinct)
                    roles |= SelectRoles.Distinct;
                
                if (Top != null)
                    roles |= SelectRoles.Top;

                return roles;
            }
        }

        public override string ToString()
        {
            return "SELECT {0}{1}{2}\r\nFROM {3}\r\n{4}{5}{6}AS {7}".Formato(
                Distinct ? "DISTINCT " : "",
                Top.TryCC(t => "TOP {0} ".Formato(t.NiceToString())),
                Columns.TryCC(c => c.ToString(", ")),
                From.TryCC(f=>f.ToString().Map(a => a.Contains("\r\n") ? "\r\n" + a.Indent(4) : a)),
                Where.TryCC(a => "WHERE " + a.NiceToString() + "\r\n"),
                OrderBy.TryCC(ob => "ORDER BY " + ob.ToString(" ,") + "\r\n"),
                GroupBy.TryCC(gb => "GROUP BY " + gb.ToString(g => g.NiceToString(), " ,") + "\r\n"),
                Alias);
        }
    }

    /// <summary>
    /// A kind of SQL join
    /// </summary>
    internal enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply,
        OuterApply,
        LeftOuterJoin,
        SingleRowLeftOuterJoin,
        RightOuterJoin,
        FullOuterJoin, 
    }

    /// <summary>
    /// A custom expression node representing a SQL join clause
    /// </summary>
    internal class JoinExpression : SourceExpression
    {
        public readonly JoinType JoinType;
        public readonly SourceExpression Left;
        public readonly SourceExpression Right;
        public new readonly Expression Condition;

        public override Alias[] KnownAliases
        {
            get { return Left.KnownAliases.Concat(Right.KnownAliases).ToArray(); }
        }

        internal JoinExpression(JoinType joinType, SourceExpression left, SourceExpression right, Expression condition)
            : base(DbExpressionType.Join)
        {
            if (left == null) 
                throw new ArgumentNullException("left");

            if (right == null)
                throw new ArgumentNullException("right");

            if (condition == null && joinType != JoinType.CrossApply && joinType != JoinType.OuterApply && joinType != JoinType.CrossJoin)
                throw new ArgumentNullException("condition");

            if (joinType == JoinType.SingleRowLeftOuterJoin && !(right is TableExpression))
                throw new ArgumentException("right"); 

            this.JoinType = joinType;
            this.Left = left;
            this.Right = right;
            this.Condition = condition;
        }
   
        public override string ToString()
        {
            return "{0}\r\n{1}\r\n{2}\r\nON {3}".Formato(Left.ToString().Indent(4), JoinType, Right.ToString().Indent(4), Condition.NiceToString());
        }
    }

    internal enum SqlFunction
    {
        CHARINDEX,
        LEFT,
        LEN,
        LOWER,
        LTRIM,
        REPLACE,
        REPLICATE,
        REVERSE,
        RIGHT,
        RTRIM,
        SUBSTRING,
        UPPER,
        
        ABS,
        PI,
        SIN,
        ASIN,
        COS,
        ACOS,
        TAN,
        ATAN,
        ATN2,
        POWER,
        SQRT,
     
        EXP,
        SQUARE, 
        LOG10, 
        LOG,

        FLOOR,
        CEILING, 
        ROUND,
        SIGN,

        DAY,
        MONTH,
        YEAR,
        DATEPART, 
        DATEDIFF,
        DATEADD,

        COALESCE,
        CONVERT,
    }

    internal enum SqlEnums
    {
        year,
        month,
        day,
        week,
        weekday,
        hour,
        minute,
        second,
        millisecond,
        dayofyear,
    }



    internal class SqlEnumExpression : DbExpression
    {
        public readonly SqlEnums Value; 
        public SqlEnumExpression(SqlEnums value)
            :base(DbExpressionType.SqlEnum, typeof(object))
        {
            this.Value = value; 
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    internal class SqlCastExpression : DbExpression
    {
        public readonly SqlDbType SqlDbType;
        public readonly Expression Expression;

        public SqlCastExpression(Type type, Expression expression)
            : this(type, expression, Schema.Current.Settings.DefaultSqlType(type))
        {
        }

        public SqlCastExpression(Type type, Expression expression, SqlDbType sqlDbType)
            :base(DbExpressionType.SqlCast, type)
        {
            this.Expression = expression;
            this.SqlDbType = sqlDbType; 
        }

        public override string ToString()
        {
            return "Cast({0} as {1})".Formato(Expression.NiceToString(), SqlDbType.ToString().ToUpper());
        }
    }

    internal class SqlFunctionExpression : DbExpression
    {
        public readonly string SqlFunction;
        public readonly ReadOnlyCollection<Expression> Arguments;

        public SqlFunctionExpression(Type type, string sqlFunction, IEnumerable<Expression> arguments)
            :base(DbExpressionType.SqlFunction, type )
        {
            this.SqlFunction = sqlFunction;
            this.Arguments = arguments.ToReadOnly(); 
        }

        public override string ToString()
        {
            return "{0}({1})".Formato(SqlFunction, Arguments.ToString(a => a.NiceToString(), ","));
        }
    }

    internal class SqlConstantExpression : DbExpression
    {
        public static readonly Expression False = Expression.Equal(new SqlConstantExpression(1), new SqlConstantExpression(0));
        public static readonly Expression True = Expression.Equal(new SqlConstantExpression(1), new SqlConstantExpression(1));

        public readonly object Value;

        public SqlConstantExpression(object value)
            : this(value, value.GetType())
        {
        }

        public SqlConstantExpression(object value, Type type)
            : base(DbExpressionType.SqlConstant, type)
        {
            this.Value = value;
        }

        public override string ToString()
        {
            return Value.TryToString() ?? "NULL";
        }
    }

    internal class When
    {
        public readonly Expression Condition;
        public readonly Expression Value;

        public When(Expression condition, Expression value)
        {
            if (condition == null)
                throw new ArgumentNullException("condition");

            if (value == null)
                throw new ArgumentNullException("value");

            if (condition.Type.UnNullify() != typeof(bool))
                throw new ArgumentException("condition");

            this.Condition = condition;
            this.Value = value; 
        }

        public override string ToString()
        {
            return "  WHEN {0} THEN {1}".Formato(Condition.NiceToString(), Value.NiceToString());
        }
    }

    internal static class ExpressionTools
    {
        public static Expression ToCondition(this IEnumerable<When> whens, Type returnType)
        {
            var @default = returnType.IsClass || returnType.IsNullable() ?
                                Expression.Constant(null, returnType) :
                                Convert(Expression.Constant(null, returnType.Nullify()), returnType);

            var result = whens.Reverse().Aggregate(
                @default, (acum, when) => Expression.Condition(when.Condition, Convert(when.Value, returnType), acum));

            return result;
        }

        public static Expression Convert(Expression expression, Type returnType)
        {
            if (expression.Type == returnType)
                return expression;

            if (expression.Type.Nullify() == returnType)
                return expression.Nullify();

            if (expression.Type.UnNullify() == returnType)
                return expression.UnNullify();

            if (returnType.IsAssignableFrom(expression.Type) || expression.Type.IsAssignableFrom(returnType))
                return Expression.Convert(expression, returnType);

            throw new InvalidOperationException("Imposible to convert to {0} the expression: \r\n{1}"
                .Formato(returnType.TypeName(), expression.NiceToString()));
        }

        public static Expression NotEqualsNulll(this Expression exp)
        {
            return Expression.NotEqual(exp.Nullify(), Expression.Constant(null, typeof(int?)));
        }
    }

    internal class CaseExpression : DbExpression
    {
        public readonly ReadOnlyCollection<When> Whens;
        public readonly Expression DefaultValue;

        
        public CaseExpression(IEnumerable<When> whens, Expression defaultValue)
            :base(DbExpressionType.Case, GetType(whens, defaultValue))
        {
            if (whens.Empty())
                throw new ArgumentNullException("whens");

            Type refType = this.Type.UnNullify();

            if (whens.Any(w => w.Value.Type.UnNullify() != refType))
                throw new ArgumentException("whens");

            this.Whens = whens.ToReadOnly();
            this.DefaultValue = defaultValue;
        }

        static Type GetType(IEnumerable<When> whens, Expression defaultValue)
        {
            var types = whens.Select(w => w.Value.Type).ToList();
            if (defaultValue != null)
                types.Add(defaultValue.Type);

            if (types.Any(a => a.IsNullable()))
                types = types.Select(ReflectionExtensions.Nullify).ToList();

            return types.Distinct().Single();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("CASE");
            foreach (var w in Whens)
                sb.AppendLine(w.ToString());

            if(DefaultValue != null)
                sb.AppendLine(DefaultValue.NiceToString()); 

            sb.AppendLine("END");
            return sb.ToString();
        }
    }

    internal class LikeExpression : DbExpression
    {
        public readonly Expression Expression;
        public readonly Expression Pattern;

        public LikeExpression(Expression expression, Expression pattern)
            :base(DbExpressionType.Like, typeof(bool))
        {
            if (expression == null || expression.Type != typeof(string))
                throw new ArgumentException("expression");

            if (pattern == null || pattern.Type != typeof(string))
                throw new ArgumentException("pattern");
            this.Expression = expression;
            this.Pattern = pattern;
        }

        public override string ToString()
        {
            return "{0} LIKE {1}".Formato(Expression.NiceToString(), Pattern.NiceToString());
        }
    }

    internal abstract class SubqueryExpression : DbExpression
    {
        public readonly SelectExpression Select;
        protected SubqueryExpression(DbExpressionType nodeType, Type type, SelectExpression select)
            : base(nodeType, type)
        {
            System.Diagnostics.Debug.Assert(nodeType == DbExpressionType.Scalar || nodeType == DbExpressionType.Exists || nodeType == DbExpressionType.In);
            this.Select = select;
        }
    }

    internal class ScalarExpression : SubqueryExpression
    {
        public ScalarExpression(Type type, SelectExpression select)
            : base(DbExpressionType.Scalar, type, select)
        {
        }

        public override string ToString()
        {
            return "SCALAR({0})".Formato(Select.NiceToString());
        }
    }

    internal class IsNullExpression : DbExpression
    {
        public readonly Expression Expression;

        public IsNullExpression(Expression expression)
            : base(DbExpressionType.IsNull, typeof(bool))
        {
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "{0} IS NULL".Formato(Expression.NiceToString());
        }
    }

    internal class IsNotNullExpression : DbExpression
    {
        public readonly Expression Expression;

        public IsNotNullExpression(Expression expression)
            : base(DbExpressionType.IsNotNull, typeof(bool))
        {
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "{0} IS NOT NULL".Formato(Expression.NiceToString());
        }
    }

    internal class ExistsExpression : SubqueryExpression
    {
        public ExistsExpression(SelectExpression select)
            : base(DbExpressionType.Exists, typeof(bool), select)
        {
        }

        public override string ToString()
        {
            return "EXIST({0})".Formato(Select.NiceToString());
        }
    }

    internal class InExpression : SubqueryExpression
    {
        public readonly Expression Expression;
        public readonly object[] Values;

        public InExpression(Expression expression, SelectExpression select)
            : base(DbExpressionType.In, typeof(bool), select)
        {
            if (expression == null)throw new ArgumentNullException("expression");
            if (select == null) throw new ArgumentNullException("select"); 
       
            this.Expression = expression;
        }

        public static Expression FromValues(Expression expression, object[] values)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (values == null) throw new ArgumentNullException("values");

            if (values.Length == 0)
                return SqlConstantExpression.False;

            return new InExpression(expression, values);
        }

        InExpression(Expression expression, object[] values)
            : base(DbExpressionType.In, typeof(bool), null)
        {
            if (expression == null) throw new ArgumentNullException("expression");
            if (values == null) throw new ArgumentNullException("values");

            this.Expression = expression;
            this.Values = values;
        }

        public override string ToString()
        {
            if (Values == null)
                return "{0} IN ({1})".Formato(Expression.NiceToString(), Select.NiceToString());
            else
                return "{0} IN ({1})".Formato(Expression.NiceToString(), Values.ToString(", "));
        }
    }

    internal class AggregateSubqueryExpression : DbExpression
    {
        public readonly Alias GroupByAlias;
        public readonly Expression AggregateInGroupSelect;
        public readonly ScalarExpression AggregateAsSubquery;
        public AggregateSubqueryExpression(Alias groupByAlias, Expression aggregateInGroupSelect, ScalarExpression aggregateAsSubquery)
            : base(DbExpressionType.AggregateSubquery, aggregateAsSubquery.Type)
        {
            this.AggregateInGroupSelect = aggregateInGroupSelect;
            this.GroupByAlias = groupByAlias;
            this.AggregateAsSubquery = aggregateAsSubquery;
        }

        public override string ToString()
        {
            return "AGGREGATE OF {0}({1}) OR\r\n {2}".Formato(GroupByAlias, AggregateInGroupSelect, AggregateAsSubquery);
        }
    }

    internal class RowNumberExpression : DbExpression
    {
        public readonly ReadOnlyCollection<OrderExpression> OrderBy;

        public RowNumberExpression(IEnumerable<OrderExpression> orderBy)
            :base(DbExpressionType.RowNumber, typeof(int))
        {
            this.OrderBy = orderBy.ToReadOnly(); 
        }

        public override string ToString()
        {
            return "ROW_NUMBER()";
        }
    }

    internal enum UniqueFunction
    {
        First, 
        FirstOrDefault,
        Single,
        SingleOrDefault,
    }

    public class ProjectionToken 
    {
        class ExternalToken : ProjectionToken
        {
            public override string ToString()
            {
                return "External";
            }
        }

        public static readonly ProjectionToken External = new ExternalToken(); 

        public override string ToString()
        {
            return GetHashCode().ToString();
        }
    }

    /// <summary>
    /// A custom expression representing the construction of one or more result objects from a 
    /// SQL select expression
    /// </summary> 
    internal class ProjectionExpression : DbExpression
    {
        public readonly SelectExpression Source;
        public readonly Expression Projector;
        public readonly UniqueFunction?  UniqueFunction;
        public readonly ProjectionToken Token; 

        internal ProjectionExpression(SelectExpression source, Expression projector, UniqueFunction? uniqueFunction, ProjectionToken token, Type resultType)
            : base(DbExpressionType.Projection, resultType)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (projector == null)
                throw new ArgumentNullException("projector");

            if (token == null)
                throw new ArgumentNullException("token");

            Type shouldImplement = uniqueFunction == null ? typeof(IEnumerable<>).MakeGenericType(projector.Type) : projector.Type;
            if (!shouldImplement.IsAssignableFrom(resultType))
                throw new InvalidOperationException("ProjectionType is {0} but should implement {1}".Formato(resultType.TypeName(), shouldImplement.TypeName()));  

            this.Source = source;
            this.Projector = projector;
            this.UniqueFunction = uniqueFunction;
            this.Token = token;
        }
    
        internal bool IsOneCell
        {
            get { return this.UniqueFunction.HasValue && Source.Columns.Count == 1; }
        }

        public override string ToString()
        {
            return "(SOURCE\r\n{0}\r\nPROJECTION\r\n{1})".Formato(Source.ToString().Indent(4), Projector.NiceToString().Indent(4)); 
        }
    }

    internal class ChildProjectionExpression : DbExpression
    {
        public readonly ProjectionExpression Projection;
        public readonly Expression OuterKey;

        internal ChildProjectionExpression(ProjectionExpression projection, Expression outerKey)
            : base(DbExpressionType.ChildProjection, ProjectionClientType(projection))
        {
            this.Projection = projection;
            this.OuterKey = outerKey;
        }

        static Type ProjectionClientType(ProjectionExpression projection)
        {
            if (!projection.Projector.Type.IsInstantiationOf(typeof(KeyValuePair<,>)))
                throw new InvalidOperationException("projection's projector should create KeyValuePairs");

            Type type = projection.Projector.Type.GetGenericArguments()[1];

            if (projection.UniqueFunction != null)
                return type;

            return typeof(IEnumerable<>).MakeGenericType(new Type[] { type });
        }

        public override string ToString()
        {
            return "{0}.InLookup({1})".Formato(Projection.NiceToString(), OuterKey.NiceToString());
        }
    }

    internal abstract class CommandExpression : DbExpression
    {
        public CommandExpression(DbExpressionType nodeType)
            : base(nodeType, typeof(void))
        {
        }
    }

    internal class DeleteExpression : CommandExpression
    {
        public readonly ITable Table;
        public readonly SourceExpression Source;
        public readonly Expression Where;

        public DeleteExpression(ITable table, SourceExpression source, Expression where)
            :base(DbExpressionType.Delete)
        {
            this.Table = table;
            this.Source = source;
            this.Where = where; 
        }

        public override string ToString()
        {
            return "DELETE {0}\r\nFROM {1}\r\n{2}".Formato(
                Table.Name, 
                Source.NiceToString(), 
                Where.TryCC(w => "WHERE " + w.NiceToString())); 
        }
    }

    internal class UpdateExpression : CommandExpression
    {
        public readonly ITable Table;
        public readonly ReadOnlyCollection<ColumnAssignment> Assigments; 
        public readonly SourceExpression Source;
        public readonly Expression Where;

        public UpdateExpression(ITable table, SourceExpression source, Expression where, IEnumerable<ColumnAssignment> assigments)
            :base(DbExpressionType.Update)
        {
            this.Table = table;
            this.Assigments = assigments.ToReadOnly();
            this.Source = source;
            this.Where = where;
        }

        public override string ToString()
        {
            return "UPDATE {0}\r\nSET {1}\r\nFROM {2}\r\n{3}".Formato(
                Table.Name,
                Assigments.ToString("\r\n"),
                Source.NiceToString(),
                Where.TryCC(w => "WHERE " + w.NiceToString()));
        }
    }

    internal class ColumnAssignment
    {
        public readonly string Column;
        public readonly Expression Expression;

        public ColumnAssignment(string column, Expression expression)
        {
            this.Column = column;
            this.Expression = expression;
        }

        public override string ToString()
        {
            return "{0} = {1}".Formato(Column, Expression);
        }
    }

    internal class CommandAggregateExpression : CommandExpression
    {
        public readonly ReadOnlyCollection<CommandExpression> Commands;

        public CommandAggregateExpression(IEnumerable<CommandExpression> commands)
            : base(DbExpressionType.CommandAggregate)
        {
            Commands = commands.ToReadOnly(); 
        }

        public override string ToString()
        {
            return Commands.ToString(a => a.NiceToString(), "\r\n\r\n");
        }
    }

    internal class SelectRowCountExpression : CommandExpression
    {
        public SelectRowCountExpression()
            : base(DbExpressionType.SelectRowCount)
        {
        }

        public override string ToString()
        {
            return "SELECT @@rowcount";
        }
    }
}
