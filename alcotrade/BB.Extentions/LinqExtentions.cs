using System;
using System.Collections.Generic;

namespace BB.Extentions
{
    public static class LinqExtentions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }
    }
}
