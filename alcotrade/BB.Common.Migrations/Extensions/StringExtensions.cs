using System;

namespace BB.Common.Migrations.Extensions
{
    internal static class StringExtensions
    {
        private static readonly string[] _lineEndings = new string[2]
        {
            "\r\n",
            "\n"
        };

        static StringExtensions()
        {
        }

        public static bool EqualsIgnoreCase(this string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool EqualsOrdinal(this string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.Ordinal);
        }

        public static string MigrationName(this string migrationId)
        {
            return migrationId.Substring(16);
        }

        public static string RestrictTo(this string s, int size)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= size)
                return s;
            else
                return s.Substring(0, size);
        }

        public static void EachLine(this string s, Action<string> action)
        {
            s.Split(_lineEndings, StringSplitOptions.None).Each(action);
        }
    }
}