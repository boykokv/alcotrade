using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BB.Core.Helpers
{
    public static class StringEx
    {
        private static char[] SEPARATORS = new[] {','};

        public static int[] ParseString2IntArray(this string source)
        {
            return
                Failover.Execute(
                    () => source.Split(SEPARATORS).Select(_ => _.Trim()).Select(_ => Int32.Parse(_)).ToArray(), exception => new int[0]);

        }
    }
}
