﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;


namespace Signum.Utilities.ExpressionTrees
{
    public static class ExpressionExpanderSamples
	{
		/// <summary>
		/// Returns whether string contains any of the specified keywords as a substring.
		/// </summary>
		/// <param name="value">Value of the string</param>
		/// <param name="keywords">Array with keywords</param>
		/// <returns>True when value contains any of the specified keywords</returns>
		[MethodExpander(typeof(ExpandContainsAny))]
		public static bool ContainsAny(this string value, params string[] keywords)
		{
			return keywords.Any((s) => value.Contains(s));
		}


		/// <summary>
		/// Returns whether string contains all of the specified keywords as a substring.
		/// </summary>
		/// <param name="value">Value of the string</param>
		/// <param name="keywords">Array with keywords</param>
		/// <returns>True when value contains all of the specified keywords</returns>
		[MethodExpander(typeof(ExpandContainsAll))]
		public static bool ContainsAll(this string value, params string[] keywords)
		{
			return keywords.All((s) => value.Contains(s));
		}
	}

    class ExpandContainsAny : IMethodExpander
	{
        static MethodInfo miContains = ReflectionTools.GetMethodInfo(() => "".Contains(""));

        public Expression Expand(Expression instance, Expression[] parameters, MethodInfo mi)
        {
            return parameters.Select(p => (Expression)Expression.Call(instance, miContains, p)).AggregateOr();
        }
	}

    class ExpandContainsAll : IMethodExpander
    {
        static MethodInfo miContains = ReflectionTools.GetMethodInfo(() => "".Contains(""));

        public Expression Expand(Expression instance, Expression[] parameters, MethodInfo mi)
        {
            return parameters.Select(p => (Expression)Expression.Call(instance, miContains, p)).AggregateAnd();
        }
    }

}
