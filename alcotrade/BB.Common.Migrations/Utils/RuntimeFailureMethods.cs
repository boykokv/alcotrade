using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BB.Common.Migrations.Utils
{
    internal static class RuntimeFailureMethods
    {
        public static readonly Regex IsNotNull = new Regex("^\\s*(@?\\w+)\\s*\\!\\=\\s*null\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly Regex IsNullOrWhiteSpace = new Regex("^\\s*\\!\\s*string\\s*\\.\\s*IsNullOrWhiteSpace\\s*\\(\\s*(@?[\\w]+)\\s*\\)\\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        [DebuggerStepThrough]
        public static void Requires(bool condition, string userMessage, string conditionText)
        {
            if (condition)
            {
                return;
            }
            Match match;
            if ((match = IsNotNull.Match(conditionText)) != null && match.Success)
            {
                throw new ArgumentNullException(match.Groups[1].Value);
            }
            if ((match = IsNullOrWhiteSpace.Match(conditionText)) != null && match.Success)
            {
                throw new ArgumentNullException(match.Groups[1].Value);
            }
            throw new ArgumentNullException(conditionText, userMessage);
        }
    }
}