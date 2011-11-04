﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities.ExpressionTrees;
using System.Linq.Expressions;
using System.Diagnostics;
using Signum.Engine.Maps;
using Signum.Entities;
using System.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities;

namespace Signum.Engine.Linq
{
    public class QueryFilterer : SimpleExpressionVisitor
    {
        static GenericInvoker<Func<Schema, IQueryable, IQueryable>> miFilter = new GenericInvoker<Func<Schema, IQueryable, IQueryable>>((s,q) => s.OnFilterQuery<TypeDN>((IQueryable<TypeDN>)q));

        bool filter;

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (typeof(IQueryable).IsAssignableFrom(c.Type))
            {
                IQueryable query = (IQueryable)c.Value;

                if (query.IsBase())
                {
                    Type identType = c.Type.GetGenericArguments().SingleEx();

                    if (filter && Schema.Current.Tables.ContainsKey(identType))
                    {
                        IQueryable newQuery = miFilter.GetInvoker(identType)(Schema.Current, query);
                        
                        if (newQuery != query)
                            return newQuery.Expression;
                    }

                    return c;
                }
                else
                {
                    /// <summary>
                    /// Replaces every expression like ConstantExpression{ Type = IQueryable, Value = complexExpr } by complexExpr
                    /// </summary>
                    return DbQueryProvider.Clean(query.Expression, filter);
                }
            }

            return base.VisitConstant(c);
        }

        
        internal static Expression Filter(Expression expression, bool filter)
        {
            return new QueryFilterer { filter = filter }.Visit(expression);
        }
    }
}
