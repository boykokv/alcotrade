using System;
using System.Linq;

namespace BB.Extentions
{
    public static class BitConverterExtention
    {
        public static byte[] StringToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ByteArrayToHexString(this byte[] array)
        {
            return BitConverter.ToString(array).Replace("-", String.Empty);
        }
    }
}
