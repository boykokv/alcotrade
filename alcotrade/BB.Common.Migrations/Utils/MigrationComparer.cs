using System;
using System.Collections.Generic;

namespace BB.Common.Migrations.Utils
{
    public class MigrationComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var splitX = x.Split('_');
            var splitY = y.Split('_');
            var splitVersionX = splitX[0].Split('.');
            var splitVersionY = splitY[0].Split('.');
            for (int i = 0; i < splitVersionX.Length; i++)
            {
                if (splitVersionY.Length < i + 1)
                    return 1;
                var res = CompareVersionPart(splitVersionX[i], splitVersionY[i]);
                if ( res != 0)
                {
                    return res;
                }
            }

            if (splitVersionX.Length < splitVersionY.Length)
                return -1;
            var indexX = x.IndexOf('_');
            var indexY = y.IndexOf('_');
            return
                System.String.Compare(x.Substring(indexX, x.Length - indexX)
                    .ToLowerInvariant(), y.Substring(indexY, y.Length - indexY).ToLowerInvariant(), System.StringComparison.Ordinal);
        }

        private int CompareVersionPart(string x, string y)
        {
            int xVersion;
            int yVersion;
            if (int.TryParse(x, out xVersion))
            {
                if (int.TryParse(y, out yVersion))
                {
                    return xVersion.CompareTo(yVersion);
                }

                throw new ArgumentException(string.Format("Некорректное число {0} в версии миграции", y));
            }

            throw new ArgumentException(string.Format("Некорректное число {0} в версии миграции", x));
        }
    }
}
