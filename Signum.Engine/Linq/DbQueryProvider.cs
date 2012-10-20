﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Data.SqlClient;
using System.Diagnostics;
using Signum.Utilities.Reflection;
using Signum.Utilities.DataStructures;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine;
using System.Data;
using Signum.Entities;

namespace Signum.Engine.Linq
{

    /// <summary>
    /// Stateless query provider 
    /// </summary>
    public class DbQueryProvider : QueryProvider
    {
        public static readonly DbQueryProvider Single = new DbQueryProvider();

        private DbQueryProvider()
        {
        }
    
        public override string GetQueryText(Expression expression)
        {
            return this.Translate(expression, tr => tr.CleanCommandText());
        }

        public override object Execute(Expression expression)
        {
            using (HeavyProfiler.Log("DB"))
                return this.Translate(expression, tr => tr.Execute());
        }

        internal R Translate<R>(Expression expression, Func<ITranslateResult, R> continuation) //For debugging purposes
        {
            using (Alias.NewGenerator())
            {
                ITranslateResult result;

                using (HeavyProfiler.Log("LINQ", () => expression.NiceToString()))
                using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
                {
                    Expression cleaned = Clean(expression, true, log);
                    var binder = new QueryBinder();
                    log.Switch("Bind");
                    ProjectionExpression binded = (ProjectionExpression)binder.BindQuery(cleaned);
                    ProjectionExpression optimized = (ProjectionExpression)Optimize(binded, binder, log);
                    log.Switch("ChPrjFlatt");
                    ProjectionExpression flat = ChildProjectionFlattener.Flatten(optimized);
                    log.Switch("TB");
                    result = TranslatorBuilder.Build(flat);
                }
                return continuation(result);
            }
        }

        public static Expression Clean(Expression expression, bool filter, HeavyProfiler.Tracer log)
        {
            Expression clean = ExpressionCleaner.Clean(expression);
            log.Switch("OvrLdSmp");
            Expression simplified = OverloadingSimplifier.Simplify(clean);
            log.Switch("QrFlr");
            Expression filtered = QueryFilterer.Filter(simplified, filter);
            return filtered;
        }

        internal static Expression Optimize(Expression binded, QueryBinder binder, HeavyProfiler.Tracer log)
        {
            log.Switch("AggRew");
            Expression rewrited = AggregateRewriter.Rewrite(binded);
            log.Switch("EnCom");
            Expression completed = EntityCompleter.Complete(rewrited, binder);
            log.Switch("AlPrRe");
            Expression replaced = AliasProjectionReplacer.Replace(completed);
            log.Switch("OrBtRw");
            Expression orderRewrited = OrderByRewriter.Rewrite(replaced);
            log.Switch("QuRb");
            Expression rebinded = QueryRebinder.Rebind(orderRewrited);
            log.Switch("UnClRmv");
            Expression columnCleaned = UnusedColumnRemover.Remove(rebinded);
            log.Switch("RwNmbFlr");
            Expression rowFilled = RowNumberFiller.Fill(columnCleaned);
            log.Switch("RdnSqRm");
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(rowFilled);
            log.Switch("CndRwr");
            Expression rewriteConditions = ConditionsRewriter.Rewrite(subqueryCleaned);
            return rewriteConditions;
        }

        internal R Delete<R>(IQueryable query, Func<CommandResult, R> continuation, bool removeSelectRowCount = false)
        {
            using (Alias.NewGenerator())
            {
                CommandResult cr;
                using (HeavyProfiler.Log("LINQ"))
                using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
                {
                    Expression cleaned = Clean(query.Expression, true, log);

                    log.Switch("Bind");
                    var binder = new QueryBinder();
                    CommandExpression delete = binder.BindDelete(cleaned);
                    CommandExpression deleteOptimized = (CommandExpression)Optimize(delete, binder, log);
                    CommandExpression deleteSimplified = UpdateDeleteSimplifier.Simplify(deleteOptimized, removeSelectRowCount);

                    cr = TranslatorBuilder.BuildCommandResult(deleteSimplified);
                }
                return continuation(cr);
            }
        }

        internal R Update<R>(IQueryable query, LambdaExpression entitySelector, LambdaExpression updateConstructor, Func<CommandResult, R> continuation, bool removeSelectRowCount = false)
        {
            using (Alias.NewGenerator())
            {
                CommandResult cr;
                using (HeavyProfiler.Log("LINQ"))
                using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
                {
                    Expression cleaned = Clean(query.Expression, true, log);
                    var binder = new QueryBinder();
                    log.Switch("Bind");
                    CommandExpression update = binder.BindUpdate(cleaned, entitySelector, updateConstructor);
                    CommandExpression updateOptimized = (CommandExpression)Optimize(update, binder, log);
                    CommandExpression updateSimplified = UpdateDeleteSimplifier.Simplify(updateOptimized, removeSelectRowCount);
                    log.Switch("TR");
                    cr = TranslatorBuilder.BuildCommandResult(updateSimplified);
                }
                return continuation(cr);
            }
        }
    }
}
