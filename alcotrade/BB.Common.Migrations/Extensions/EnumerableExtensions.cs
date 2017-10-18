using System;
using System.Collections.Generic;
using System.Linq;

namespace BB.Common.Migrations.Extensions
{
    public static class EnumerableExtensions
    {
        public static string Uniquify(this IEnumerable<string> inputStrings, string targetString)
        {
            var num = 0;
            while (Enumerable.Any(inputStrings, n => string.Equals(n, targetString, StringComparison.Ordinal)))
                targetString = targetString + ++num;
            return targetString;
        }

        public static void Each<T>(this IEnumerable<T> ts, Action<T, int> action)
        {
            int num = 0;
            foreach (T obj in ts)
                action(obj, num++);
        }

        public static void Each<T>(this IEnumerable<T> ts, Action<T> action)
        {
            foreach (T obj in ts)
                action(obj);
        }

        public static void Each<T, S>(this IEnumerable<T> ts, Func<T, S> action)
        {
            foreach (T obj in ts)
            {
                S s = action(obj);
            }
        }

        public static string Join<T>(this IEnumerable<T> ts, Func<T, string> selector = null, string separator = ", ")
        {
            selector = selector ?? (t => t.ToString());
            return string.Join(separator, ts.Where(t => !ReferenceEquals(t, null)).Select(selector));
        }

        public static IEnumerable<TSource> Prepend<TSource>(this IEnumerable<TSource> source, TSource value)
        {
            yield return value;
            foreach (TSource source1 in source)
                yield return source1;
        }

        public static IEnumerable<TSource> Append<TSource>(this IEnumerable<TSource> source, TSource value)
        {
            foreach (TSource source1 in source)
                yield return source1;
            yield return value;
        }
    }
}