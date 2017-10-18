using System.Collections.Generic;
using System.Linq;

namespace BB.Common.Migrations.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Получить имя SQL View из имени sql Table.
        /// </summary>
        public static string GetViewName(this string self)
        {
            return "vw_" + self;
        }

        private static T[] AddItems<T>(this T[] self, IEnumerable<T> items)
        {
            var list = self.ToList();
            list.AddRange(items);

            return list.ToArray();
        }

        public static T[] AddProperties<T>(this T[] self, params T[] properties)
        {
            return self.AddItems(properties);
        }
    }
}
