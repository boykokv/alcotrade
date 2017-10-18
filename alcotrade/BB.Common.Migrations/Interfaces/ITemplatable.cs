using System.Collections.Generic;

namespace BB.Common.Migrations.Interfaces
{
    public interface ITemplatable
    {
        /// <summary>
        /// Возвращает SQL троку для миграции
        /// </summary>
        IEnumerable<string> Template { get;}
    }
}
