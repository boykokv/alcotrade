using System;

namespace BB.Extentions
{
    public static class ArrayExtentions
    {
        public static T[] SliceArray<T>(this T[] data, int index, int? length)
        {
            if (length + index > data.Length)
                throw new ArgumentOutOfRangeException($"Incorrect slicing parameters. Asked {length} items starting {index} from {data.Length} lenth long array");

            length = length ?? data.Length - index;

            T[] result = new T[length.Value];
            Array.Copy(data, index, result, 0, length.Value);
            return result;
        }

        public static T[] SliceArray<T>(this T[] data, ref int index, int? length, out T[] result)
        {
            if (length + index > data.Length)
                throw new ArgumentOutOfRangeException($"Incorrect slicing parameters. Asked {length} items starting {index} from {data.Length} lenth long array");

            length = length ?? data.Length - index;

            result = new T[length.Value];

            Array.Copy(data, index, result, 0, length.Value);

            index += length.Value;

            return data;
        }
    }
}
