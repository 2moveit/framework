﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using Signum.Entities.Properties;

namespace Signum.Entities.DynamicQuery
{
    [Serializable]
    public class Filter
    {
        public QueryToken Token;

        public FilterOperation Operation { get; set; }

        object value;
        public object Value
        {
            get { return value; }

            set
            {
                this.value = ReflectionTools.ChangeType(value, Token.Type); 
            }
        }

        static MethodInfo miContains = ReflectionTools.GetMethodInfo((string s) => s.Contains(s));
        static MethodInfo miStartsWith = ReflectionTools.GetMethodInfo((string s) => s.StartsWith(s));
        static MethodInfo miEndsWith = ReflectionTools.GetMethodInfo((string s) => s.EndsWith(s));
        static MethodInfo miLike = ReflectionTools.GetMethodInfo((string s) => s.Like(s));
     
        public Expression GetCondition(BuildExpressionContext context)
        {
            List<CollectionElementToken> allAny = Token.FollowC(a => a.Parent)
                .OfType<CollectionElementToken>()
                .Where(a => a.ElementType == CollectionElementType.Any ||
                            a.ElementType == CollectionElementType.All).ToList();

            if (allAny.IsEmpty())
                return GetConditionBasic(context);

            var parameters = allAny.ToDictionary(a => a, a => a.CreateParameter());
            var expressions = allAny.ToDictionary(a => (QueryToken)a, a => a.CreateExpression(parameters[a]));

            context.Replacemens.AddRange(expressions);

            Expression exp = GetConditionBasic(context);
         
            foreach (CollectionElementToken cet in allAny)
            {
                LambdaExpression lambda = Expression.Lambda(exp, parameters[cet]);

                exp = cet.BuildExpressionLambda(context, lambda);  
            }

            context.Replacemens.RemoveRange(expressions.Keys);

            return exp; 
        }

        private Expression GetConditionBasic(BuildExpressionContext context)
        {
            Expression left = Token.BuildExpression(context);
            Expression right = Expression.Constant(Value, Token.Type);

            switch (Operation)
            {
                case FilterOperation.EqualTo: return Expression.Equal(left, right);
                case FilterOperation.DistinctTo: return Expression.NotEqual(left, right);
                case FilterOperation.GreaterThan: return Expression.GreaterThan(left, right);
                case FilterOperation.GreaterThanOrEqual: return Expression.GreaterThanOrEqual(left, right);
                case FilterOperation.LessThan: return Expression.LessThan(left, right);
                case FilterOperation.LessThanOrEqual: return Expression.LessThanOrEqual(left, right);
                case FilterOperation.Contains: return Expression.Call(left, miContains, right);
                case FilterOperation.StartsWith: return Expression.Call(left, miStartsWith, right);
                case FilterOperation.EndsWith: return Expression.Call(left, miEndsWith, right);
                case FilterOperation.Like: return Expression.Call(miLike, left, right);
                default:
                    throw new InvalidOperationException("Unknown operation {0}".Formato(Operation));
            }
        }

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(Token.FullKey(), Operation, Value);
        }
    }


    public enum FilterOperation
    {
        EqualTo,
        DistinctTo,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Contains,
        StartsWith,
        EndsWith,
        Like,
    }

    public enum FilterType
    {
        Number,
        DecimalNumber,
        String, 
        DateTime,
        Lite,
        Embedded,
        Boolean, 
        Enum,
    }

    public enum UniqueType
    {
        First,
        FirstOrDefault,
        Single,
        SingleOrDefault,
        SingleOrMany, 
        Only
    }
}
