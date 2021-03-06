using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Globalization;
using Signum.Utilities.Synchronization;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Data;
using System.Text.RegularExpressions;
using System.Collections;
using Signum.Utilities.ExpressionTrees;
using System.ComponentModel;

namespace Signum.Utilities
{

    public static class EnumerableUniqueExtensions
    {
        class UniqueExExpander : IMethodExpander
        {
            static MethodInfo miWhereE = ReflectionTools.GetMethodInfo(() => Enumerable.Where<int>(null, a => false)).GetGenericMethodDefinition();
            static MethodInfo miWhereQ = ReflectionTools.GetMethodInfo(() => Queryable.Where<int>(null, a => false)).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                bool query = mi.GetParameters()[0].ParameterType.IsInstantiationOf(typeof(IQueryable<>));

                var whereMi = (query ? miWhereQ : miWhereE).MakeGenericMethod(mi.GetGenericArguments());

                var whereExpr = Expression.Call(whereMi, arguments[0], arguments[1]);

                var uniqueMi = mi.DeclaringType.GetMethods().SingleEx(m => m.Name == mi.Name && m.IsGenericMethod && m.GetParameters().Length == (mi.GetParameters().Length - 1));

                return Expression.Call(uniqueMi.MakeGenericMethod(mi.GetGenericArguments()), whereExpr);
            }
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T SingleEx<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            T result = default(T);
            bool found = false;
            foreach (T item in collection)
            {
                if (predicate(item))
                {
                    if (found)
                        throw new InvalidOperationException("Sequence contains more than one {0}".Formato(typeof(T).TypeName()));

                    result = item;
                    found = true;
                }
            }

            if (found)
                return result;

            throw new InvalidOperationException("Sequence contains no {0}".Formato(typeof(T).TypeName()));
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T SingleEx<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {

            return query.Where(predicate).SingleEx();
        }

        public static T SingleEx<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no {0}".Formato(typeof(T).TypeName()));

                T current = enumerator.Current;

                if (!enumerator.MoveNext())
                    return current;
            }

            throw new InvalidOperationException("Sequence contains more than one {0}".Formato(typeof(T).TypeName()));
        }

        public static T SingleEx<T>(this IEnumerable<T> collection, Func<string> elementName, bool forEndUser = false)
        {
            return collection.SingleEx(
                () => forEndUser ? CollectionMessage.No0Found.NiceToString(elementName()) : "Sequence contains no {0}".Formato(elementName()),
                () => forEndUser ? CollectionMessage.MoreThanOne0Found.NiceToString(elementName()) : "Sequence contains more than one {0}".Formato(elementName()),
                forEndUser);
        }

        public static T SingleEx<T>(this IEnumerable<T> collection, Func<string> errorZero, Func<string> errorMoreThanOne, bool forEndUser = false)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw NewException(forEndUser, errorZero());

                T current = enumerator.Current;

                if (!enumerator.MoveNext())
                    return current;
            }

            throw NewException(forEndUser, errorMoreThanOne());
        }

        static Exception NewException(bool forEndUser, string message)
        {
            if (forEndUser)
                return new ApplicationException(message);
            else
                return new InvalidOperationException(message); 
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T SingleOrDefaultEx<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            T result = default(T);
            bool found = false;
            foreach (T item in collection)
            {
                if (predicate(item))
                {
                    if (found)
                        throw new InvalidOperationException("Sequence contains more than one {0}".Formato(typeof(T).TypeName()));

                    result = item;
                    found = true;
                }
            }

            return result;
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T SingleOrDefaultEx<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return query.Where(predicate).SingleOrDefaultEx();
        }

        public static T SingleOrDefaultEx<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return default(T);

                T current = enumerator.Current;

                if (!enumerator.MoveNext())
                    return current;
            }

            throw new InvalidOperationException("Sequence contains more than one {0}".Formato(typeof(T).TypeName()));
        }

        public static T SingleOrDefaultEx<T>(this IEnumerable<T> collection, Func<string> errorMoreThanOne, bool forEndUser = false)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return default(T);

                T current = enumerator.Current;

                if (!enumerator.MoveNext())
                    return current;
            }

            throw NewException(forEndUser, errorMoreThanOne());
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T FirstEx<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            foreach (T item in collection)
            {
                if (predicate(item))
                    return item;
            }

            throw new InvalidOperationException("Sequence contains no {0}".Formato(typeof(T).TypeName()));
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T FirstEx<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return query.Where(predicate).FirstEx();
        }

        public static T FirstEx<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no {0}".Formato(typeof(T).TypeName()));

                return enumerator.Current;
            }
        }

        public static T FirstEx<T>(this IEnumerable<T> collection, Func<string> errorZero, bool forEndUser = false)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw NewException(forEndUser, errorZero());

                return enumerator.Current;
            }
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T SingleOrManyEx<T>(this IEnumerable<T> collection, Func<T, bool> predicate)
        {
            return collection.Where(predicate).FirstEx();
        }

        [MethodExpander(typeof(UniqueExExpander))]
        public static T SingleOrManyEx<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        {
            return query.Where(predicate).FirstEx();
        }

        public static T SingleOrManyEx<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Sequence contains no {0}".Formato(typeof(T).TypeName()));

                T current = enumerator.Current;

                if (enumerator.MoveNext())
                    return default(T);

                return current;
            }
        }

        public static T SingleOrManyEx<T>(this IEnumerable<T> collection, Func<string> errorZero, bool forEndUser = false)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw NewException(forEndUser, errorZero());

                T current = enumerator.Current;

                if (enumerator.MoveNext())
                    return default(T);

                return current;
            }
        }

        //Throws exception if 0, returns if one, returns default if many
        public static T SingleOrMany<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("The collection has no elements");

                T current = enumerator.Current;

                if (enumerator.MoveNext())
                    return default(T);

                return current;
            }
        }

        //returns default if 0 or many, returns if one
        public static T Only<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return default(T);

                T current = enumerator.Current;

                if (enumerator.MoveNext())
                    return default(T);

                return current;
            }
        }
    }


    public static class EnumerableExtensions
    {
        [MethodExpander(typeof(IsEmptyExpander))]
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> collection)
        {
            if (collection == null)
                return Enumerable.Empty<T>();

            return collection;
        }

        [MethodExpander(typeof(IsEmptyExpander))]
        public static bool IsEmpty<T>(this IEnumerable<T> collection)
        {
            foreach (var item in collection)
                return false;

            return true;
        }

        class IsEmptyExpander : IMethodExpander
        {
            static readonly MethodInfo miAny = ReflectionTools.GetMethodInfo((int[] a) => a.Any()).GetGenericMethodDefinition();

            public Expression Expand(Expression instance, Expression[] arguments, MethodInfo mi)
            {
                return Expression.Not(Expression.Call(miAny.MakeGenericMethod(mi.GetGenericArguments()), arguments));
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || collection.IsEmpty();
        }

        public static bool HasItems<T>(this IEnumerable<T> collection)
        {
            return collection != null && collection.Any();
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T> collection) where T : class
        {
            foreach (var item in collection)
            {
                if (item != null)
                    yield return item;
            }
        }

        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> collection) where T : struct
        {
            foreach (var item in collection)
            {
                if (item.HasValue)
                    yield return item.Value;
            }
        }

        public static IEnumerable<T> And<T>(this IEnumerable<T> collection, T newItem)
        {
            foreach (var item in collection)
                yield return item;
            yield return newItem;
        }

        public static IEnumerable<T> PreAnd<T>(this IEnumerable<T> collection, T newItem)
        {
            yield return newItem;
            foreach (var item in collection)
                yield return item;
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, T item)
        {
            int i = 0;
            foreach (var val in collection)
            {
                if (EqualityComparer<T>.Default.Equals(item, val))
                    return i;
                i++;
            }
            return -1;
        }

        public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> condition)
        {

            int i = 0;
            foreach (var val in collection)
            {
                if (condition(val))
                    return i;
                i++;
            }
            return -1;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> collection, Random rng)
        {
            T[] elements = collection.ToArray();

            for (int i = elements.Length - 1; i > 0; i--)
            {
                int rnd = rng.Next(i + 1);
                yield return elements[rnd];
                elements[rnd] = elements[i];
            }

            yield return elements[0];
        }

        public static string ToString<T>(this IEnumerable<T> source, string separator)
        {
            StringBuilder sb = null;
            foreach (var item in source)
            {
                if (sb == null)
                    sb = new StringBuilder();
                else
                    sb.Append(separator);

                sb.Append(item.ToString());
            }

            if (sb == null)
                return "";

            return sb.ToString();  // Remove at the end is faster
        }

        public static string ToString<T>(this IEnumerable<T> source, Func<T, string> toString, string separator)
        {
            StringBuilder sb = null;
            foreach (var item in source)
            {
                if (sb == null)
                    sb = new StringBuilder();
                else
                    sb.Append(separator);

                sb.Append(toString(item));
            }

            if (sb == null)
                return "";

            return sb.ToString();  // Remove at the end is faster
        }

        public static string ToString<T>(this IQueryable<T> source, Expression<Func<T, string>> toString, string separator)
        {
            return source.Select(toString).ToString(separator);
        }

        public static string CommaAnd<T>(this IEnumerable<T> collection)
        {
            return CommaString(collection.Select(a => a.ToString()).ToArray(), CollectionMessage.And.NiceToString());
        }

        public static string CommaAnd<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            return CommaString(collection.Select(toString).ToArray(), CollectionMessage.And.NiceToString());
        }

        public static string CommaOr<T>(this IEnumerable<T> collection)
        {
            return CommaString(collection.Select(a => a.ToString()).ToArray(), CollectionMessage.Or.NiceToString());
        }

        public static string CommaOr<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            return CommaString(collection.Select(toString).ToArray(), CollectionMessage.Or.NiceToString());
        }

        public static string Comma<T>(this IEnumerable<T> collection, string lastSeparator)
        {
            return CommaString(collection.Select(a => a.ToString()).ToArray(), lastSeparator);
        }

        public static string Comma<T>(this IEnumerable<T> collection, Func<T, string> toString, string lastSeparator)
        {
            return CommaString(collection.Select(toString).ToArray(), lastSeparator);
        }

        static string CommaString(this string[] values, string lastSeparator)
        {
            if (values.Length == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            sb.Append(values[0]);

            for (int i = 1; i < values.Length - 1; i++)
            {
                sb.Append(", ");
                sb.Append(values[i]);
            }

            if (values.Length > 1)
            {
                sb.Append(lastSeparator);
                sb.Append(values[values.Length - 1]);
            }

            return sb.ToString();
        }

        public static void ToConsole<T>(this IEnumerable<T> collection)
        {
            ToConsole(collection, a => a.ToString());
        }

        public static void ToConsole<T>(this IEnumerable<T> collection, Func<T, string> toString)
        {
            foreach (var item in collection)
                Console.WriteLine(toString(item));
        }

        public static void ToFile(this IEnumerable<string> collection, string fileName)
        {
            using (FileStream fs = File.Create(fileName))
            using (StreamWriter sw = new StreamWriter(fs, Encoding.Default))
            {
                foreach (var item in collection)
                    sw.WriteLine(item);
            }
        }

        public static void ToFile<T>(this IEnumerable<T> collection, Func<T, string> toString, string fileName)
        {
            collection.Select(toString).ToFile(fileName);
        }

        public static DataTable ToDataTable<T>(this IEnumerable<T> collection)
        {
            DataTable table = new DataTable();

            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>();
            table.Columns.AddRange(members.Select(m => new DataColumn(m.Name, m.MemberInfo.ReturningType())).ToArray());
            foreach (var e in collection)
                table.Rows.Add(members.Select(m => m.Getter(e)).ToArray());
            return table;
        }

        #region String Tables
        public static string[,] ToStringTable<T>(this IEnumerable<T> collection)
        {
            List<MemberEntry<T>> members = MemberEntryFactory.GenerateList<T>();

            string[,] result = new string[members.Count, collection.Count() + 1];

            for (int i = 0; i < members.Count; i++)
                result[i, 0] = members[i].Name;

            int j = 1;
            foreach (var item in collection)
            {
                for (int i = 0; i < members.Count; i++)
                    result[i, j] = members[i].Getter(item).Try(a => a.ToString()) ?? "";
                j++;
            }

            return result;
        }

        public static string[,] ToStringTable(this DataTable table)
        {
            string[,] result = new string[table.Columns.Count, table.Rows.Count + 1];

            for (int i = 0; i < table.Columns.Count; i++)
                result[i, 0] = table.Columns[i].ColumnName;

            int j = 1;
            foreach (DataRow row in table.Rows)
            {
                for (int i = 0; i < table.Columns.Count; i++)
                    result[i, j] = row[i].Try(a => a.ToString()) ?? "";
                j++;
            }

            return result;
        }

        public static string FormatTable(this string[,] table, bool longHeaders = true, string separator = " ")
        {
            int width = table.GetLength(0);
            int height = table.GetLength(1);
            int start = height == 1 ? 0 : (longHeaders ? 0 : 1);

            int[] lengths = 0.To(width).Select(i => Math.Max(3, start.To(height).Max(j => table[i, j].Length))).ToArray();

            return 0.To(height).Select(j => 0.To(width).ToString(i => table[i, j].PadChopRight(lengths[i]), separator)).ToString("\r\n");
        }

        public static void WriteFormattedStringTable<T>(this IEnumerable<T> collection, TextWriter textWriter, string title, bool longHeaders)
        {
            textWriter.WriteLine();
            if (title.HasText())
                textWriter.WriteLine(title);
            textWriter.WriteLine(collection.ToStringTable().FormatTable(longHeaders));
            textWriter.WriteLine();
        }

        public static void ToConsoleTable<T>(this IEnumerable<T> collection, string title = null, bool longHeader = false)
        {
            collection.WriteFormattedStringTable(Console.Out, title, longHeader);
        }

        public static string ToFormattedTable<T>(this IEnumerable<T> collection, string title = null, bool longHeader = false)
        {
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
                collection.WriteFormattedStringTable(sw, title, longHeader);
            return sb.ToString();
        }
        #endregion

        #region Min Max
        public static T WithMin<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
          where V : IComparable<V>
        {
            T result = default(T);
            bool hasMin = false;
            V min = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMin || val.CompareTo(min) < 0)
                {
                    hasMin = true;
                    min = val;
                    result = item;
                }
            }

            return result;
        }

        public static T WithMax<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
               where V : IComparable<V>
        {
            T result = default(T);
            bool hasMax = false;
            V max = default(V);

            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMax || val.CompareTo(max) > 0)
                {
                    hasMax = true;
                    max = val;
                    result = item;
                }
            }
            return result;
        }

        public static MinMax<T> WithMinMaxPair<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
        where V : IComparable<V>
        {
            T withMin = default(T), withMax = default(T);
            bool hasMin = false, hasMax = false;
            V min = default(V), max = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);
                if (!hasMax || val.CompareTo(max) > 0)
                {
                    hasMax = true;
                    max = val;
                    withMax = item;
                }

                if (!hasMin || val.CompareTo(min) < 0)
                {
                    hasMin = true;
                    min = val;
                    withMin = item;
                }
            }

            return new MinMax<T>(withMin, withMax);
        }

        public static Interval<T> ToInterval<T>(this IEnumerable<T> collection)
            where T : struct, IComparable<T>, IEquatable<T>
        {
            bool has = false;
            T min = default(T), max = default(T);
            foreach (var item in collection)
            {
                if (!has)
                {
                    has = true;
                    min = max = item;
                }
                else
                {
                    if (item.CompareTo(max) > 0)
                        max = item;
                    if (item.CompareTo(min) < 0)
                        min = item;
                }
            }

            return new Interval<T>(min, max);
        }

        public static Interval<V> ToInterval<T, V>(this IEnumerable<T> collection, Func<T, V> valueSelector)
            where V : struct, IComparable<V>, IEquatable<V>
        {
            bool has = false;
            V min = default(V), max = default(V);
            foreach (var item in collection)
            {
                V val = valueSelector(item);

                if (!has)
                {
                    has = true;
                    min = max = val;
                }
                else
                {
                    if (val.CompareTo(max) > 0)
                        max = val;
                    if (val.CompareTo(min) < 0)
                        min = val;
                }
            }

            return new Interval<V>(min, max);
        }


        #endregion

        #region Operation
        public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] collections)
        {
            foreach (var collection in collections)
            {
                foreach (var item in collection)
                {
                    yield return item;
                }
            }
        }

        public static IEnumerable<S> BiSelect<T, S>(this IEnumerable<T> collection, Func<T, T, S> func)
        {
            return BiSelect(collection, func, BiSelectOptions.None);
        }

        public static IEnumerable<S> BiSelect<T, S>(this IEnumerable<T> collection, Func<T, T, S> func, BiSelectOptions options)
        {
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    yield break;


                T firstItem = enumerator.Current;
                if (options == BiSelectOptions.Initial || options == BiSelectOptions.InitialAndFinal)
                    yield return func(default(T), firstItem);

                T lastItem = firstItem;
                while (enumerator.MoveNext())
                {
                    T item = enumerator.Current;
                    yield return func(lastItem, item);
                    lastItem = item;
                }

                if (options == BiSelectOptions.Final || options == BiSelectOptions.InitialAndFinal)
                    yield return func(lastItem, default(T));

                if (options == BiSelectOptions.Circular)
                    yield return func(lastItem, firstItem);
            }
        }

        //return one element more
        public static IEnumerable<S> SelectAggregate<T, S>(this IEnumerable<T> collection, S seed, Func<S, T, S> aggregate)
        {
            yield return seed;
            foreach (var item in collection)
            {
                seed = aggregate(seed, item);
                yield return seed;
            }
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<ImmutableStack<T>> emptyProduct = new[] { ImmutableStack<T>.Empty };
            var result = sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Push(item));

            return result.Select(a => a.Reverse());
        }



        public static IEnumerable<T> Distinct<T, S>(this IEnumerable<T> collection, Func<T, S> func)
        {
            return collection.Distinct(new LambdaComparer<T, S>(func));
        }

        public static IEnumerable<T> Distinct<T, S>(this IEnumerable<T> collection, Func<T, S> func, IEqualityComparer<S> comparer)
        {
            return collection.Distinct(new LambdaComparer<T, S>(func, comparer, null));
        }

        public static IEnumerable<T> Slice<T>(this IEnumerable<T> collection, int firstIncluded, int toNotIncluded)
        {
            return collection.Skip(firstIncluded).Take(toNotIncluded - firstIncluded);
        }

        public static IOrderedEnumerable<T> OrderBy<T>(this IEnumerable<T> collection) where T : IComparable<T>
        {
            return collection.OrderBy(a => a);
        }

        public static IOrderedEnumerable<T> OrderByDescending<T>(this IEnumerable<T> collection) where T : IComparable<T>
        {
            return collection.OrderByDescending(a => a);
        }
        #endregion

        #region Zip
        public static IEnumerable<Tuple<A, B>> Zip<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (enumA.MoveNext() && enumB.MoveNext())
                {
                    yield return new Tuple<A, B>(enumA.Current, enumB.Current);
                }
            }
        }

        public static IEnumerable<R> ZipOrDefault<A, B, R>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> resultSelector)
        {
            bool okA = true, okB = true;

            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (okA & (okA = enumA.MoveNext()) | okB & (okB = enumB.MoveNext()))
                {
                    yield return resultSelector(
                        okA ? enumA.Current : default(A),
                        okB ? enumB.Current : default(B));
                }
            }
        }

        public static IEnumerable<Tuple<A, B>> ZipOrDefault<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)
        {
            bool okA = true, okB = true;

            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while ((okA &= enumA.MoveNext()) || (okB &= enumB.MoveNext()))
                {
                    yield return new Tuple<A, B>(
                        okA ? enumA.Current : default(A),
                        okB ? enumB.Current : default(B));
                }
            }
        }

        public static void ZipForeach<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Action<A, B> actions)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (enumA.MoveNext() && enumB.MoveNext())
                {
                    actions(enumA.Current, enumB.Current);
                }
            }
        }

        public static IEnumerable<Tuple<A, B>> ZipStrict<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (AssertoTwo(enumA.MoveNext(), enumB.MoveNext()))
                {
                    yield return new Tuple<A, B>(enumA.Current, enumB.Current);
                }
            }
        }

        public static IEnumerable<R> ZipStrict<A, B, R>(this IEnumerable<A> colA, IEnumerable<B> colB, Func<A, B, R> mixer)
        {
            colA.Zip(colB, (a, b) => a);
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (AssertoTwo(enumA.MoveNext(), enumB.MoveNext()))
                {
                    yield return mixer(enumA.Current, enumB.Current);
                }
            }
        }

        public static void ZipForeachStrict<A, B>(this IEnumerable<A> colA, IEnumerable<B> colB, Action<A, B> action)
        {
            using (var enumA = colA.GetEnumerator())
            using (var enumB = colB.GetEnumerator())
            {
                while (AssertoTwo(enumA.MoveNext(), enumB.MoveNext()))
                {
                    action(enumA.Current, enumB.Current);
                }
            }
        }

        static bool AssertoTwo(bool nextA, bool nextB)
        {
            if (nextA != nextB)
                if (nextA)
                    throw new InvalidOperationException("Second collection is shorter");
                else
                    throw new InvalidOperationException("First collection is shorter");
            else
                return nextA;
        }
        #endregion

        #region Conversions


        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }

        public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> collection)
        {
            return collection == null ? EmptyReadOnlyCollection<T>.Instance :
                collection as ReadOnlyCollection<T> ??
                (collection as List<T> ?? collection.ToList()).AsReadOnly();
        }

        static class EmptyReadOnlyCollection<T>
        {
            internal static ReadOnlyCollection<T> Instance;

            static EmptyReadOnlyCollection()
            {
                EmptyReadOnlyCollection<T>.Instance = new ReadOnlyCollection<T>(new T[0]);
            }
        }

        public static ReadOnlyDictionary<K, V> ToReadOnly<K, V>(this IDictionary<K, V> dictionary)
        {
            return dictionary == null ? EmptyReadOnlyDictionary<K, V>.Instance :
                dictionary as ReadOnlyDictionary<K, V> ??
                new ReadOnlyDictionary<K, V>(dictionary);
        }

        static class EmptyReadOnlyDictionary<K, V>
        {
            internal static ReadOnlyDictionary<K, V> Instance;

            static EmptyReadOnlyDictionary()
            {
                EmptyReadOnlyDictionary<K, V>.Instance = new ReadOnlyDictionary<K, V>(new Dictionary<K, V>());
            }
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> collection)
        {
            return collection == null ? null :
              collection as ObservableCollection<T> ??
              new ObservableCollection<T>(collection);
        }

        public static IEnumerable<T> AsThreadSafe<T>(this IEnumerable<T> source)
        {
            return new TreadSafeEnumerator<T>(source);
        }

        public static IEnumerable<T> ToProgressEnumerator<T>(this IEnumerable<T> source, out IProgressInfo pi)
        {
            pi = new ProgressEnumerator<T>(source, source.Count());
            return (IEnumerable<T>)pi;
        }



        public static void PushRange<T>(this Stack<T> stack, IEnumerable<T> elements)
        {
            foreach (var item in elements)
                stack.Push(item);
        }

        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> elements)
        {
            foreach (var item in elements)
                queue.Enqueue(item);
        }

        public static void AddRange<T>(this HashSet<T> hashset, IEnumerable<T> coleccion)
        {
            foreach (var item in coleccion)
            {
                hashset.Add(item);
            }
        }

        public static bool TryContains<T>(this HashSet<T> hashset, T element)
        {
            if (hashset == null)
                return false;

            return hashset.Contains(element);
        }
        #endregion

        public static IEnumerable<R> JoinStrict<K, C, S, R>(
           IEnumerable<C> currentCollection,
           IEnumerable<S> shouldCollection,
           Func<C, K> currentKeySelector,
           Func<S, K> shouldKeySelector,
           Func<C, S, R> resultSelector, string action)
        {

            var currentDictionary = currentCollection.ToDictionary(currentKeySelector);
            var shouldDictionary = shouldCollection.ToDictionary(shouldKeySelector);

            var extra = currentDictionary.Keys.Where(k => !shouldDictionary.ContainsKey(k)).ToList();
            var missing = shouldDictionary.Keys.Where(k => !currentDictionary.ContainsKey(k)).ToList();

            if (extra.Count != 0)
                if (missing.Count != 0)
                    throw new InvalidOperationException("Error {0}\r\n Extra: {1}\r\n Missing: {2}".Formato(action, extra.ToString(", "), missing.ToString(", ")));
                else
                    throw new InvalidOperationException("Error {0}\r\n Extra: {1}".Formato(action, extra.ToString(", ")));
            else
                if (missing.Count != 0)
                    throw new InvalidOperationException("Error {0}\r\n Missing: {1}".Formato(action, missing.ToString(", ")));

            return currentDictionary.Select(p => resultSelector(p.Value, shouldDictionary[p.Key]));
        }


        public static JoinStrictResult<C, S, R> JoinStrict<K, C, S, R>(
            IEnumerable<C> currentCollection,
            IEnumerable<S> shouldCollection,
            Func<C, K> currentKeySelector,
            Func<S, K> shouldKeySelector,
            Func<C, S, R> resultSelector)
        {
            var currentDictionary = currentCollection.ToDictionary(currentKeySelector);
            var newDictionary = shouldCollection.ToDictionary(shouldKeySelector);

            HashSet<K> commonKeys = currentDictionary.Keys.ToHashSet();
            commonKeys.IntersectWith(newDictionary.Keys);

            return new JoinStrictResult<C, S, R>
            {
                Extra = currentDictionary.Where(e => !newDictionary.ContainsKey(e.Key)).Select(e => e.Value).ToList(),
                Missing = newDictionary.Where(e => !currentDictionary.ContainsKey(e.Key)).Select(e => e.Value).ToList(),

                Result = commonKeys.Select(k => resultSelector(currentDictionary[k], newDictionary[k])).ToList()
            };
        }

        public static IEnumerable<Iteration<T>> Iterate<T>(this IEnumerable<T> collection)
        {
            using (IEnumerator<T> enumerator = collection.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }
                bool isFirst = true;
                bool isLast = false;
                int index = 0;
                while (!isLast)
                {
                    T current = enumerator.Current;
                    isLast = !enumerator.MoveNext();
                    yield return new Iteration<T>(current, isFirst, isLast, index++);
                    isFirst = false;
                }
            }
        }

        public static IEnumerable<T> Duplicates<T, K>(this IEnumerable<T> source, Func<T, K> selector, IEqualityComparer<K> comparer)
        {
            var hash = new HashSet<K>(comparer);
            return source.Where(item => !hash.Add(selector(item)));
        }

        public static IEnumerable<T> Duplicates<T, K>(this IEnumerable<T> source, Func<T, K> selector)
        {
            return source.Duplicates(selector, null);
        }

        public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            var hash = new HashSet<T>(comparer);
            return source.Where(item => !hash.Add(item));
        }

        public static IEnumerable<T> Duplicates<T>(this IEnumerable<T> source)
        {
            return source.Duplicates(EqualityComparer<T>.Default);
        }
    }

    public enum CollectionMessage
    {
        [Description(" and ")]
        And,
        [Description(" or ")]
        Or,
        [Description("No {0} found")]
        No0Found,
        [Description("More than one {0} found")]
        MoreThanOne0Found,
    }

    public class JoinStrictResult<O, N, R>
    {
        public List<O> Extra;
        public List<N> Missing;
        public List<R> Result;
    }

    public enum BiSelectOptions
    {
        None,
        Initial,
        Final,
        InitialAndFinal,
        Circular,
    }

    public class Iteration<T>
    {
        readonly T value;
        readonly bool isFirst;
        readonly bool isLast;
        readonly int position;

        internal Iteration(T value, bool isFirst, bool isLast, int position)
        {
            this.value = value;
            this.isFirst = isFirst;
            this.isLast = isLast;
            this.position = position;
        }

        public T Value { get { return value; } }
        public bool IsFirst { get { return isFirst; } }
        public bool IsLast { get { return isLast; } }
        public int Position { get { return position; } }
        public bool IsEven { get { return position % 2 == 0; } }
        public bool IsOdd { get { return position % 1 == 0; } }
    }
}
