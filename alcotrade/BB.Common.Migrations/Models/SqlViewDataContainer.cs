using System.Collections.Generic;

namespace BB.Common.Migrations.Models
{
    public enum JoinType
    {
        Inner,
        Left
    }
    /// <summary>
    /// Данный о таблице участвующей в SQL view
    /// </summary>
    public class SqlViewDataContainer
    {
        public SqlViewDataContainer()
        {
            UniqueNamedProperties = new string[] {};
        }
        /// <summary>
        /// Ключ главной таблицы
        /// </summary>
        public string[] InnerKeys { get; set; }

        /// <summary>
        /// Ключ присоединяймой таблицы
        /// </summary>
        public string[] OuterKeys { get; set; }

        /// <summary>
        /// Поля возвращаемые из даннай таблицы
        /// </summary>
        public IEnumerable<string> Properties { get; set; }

        /// <summary>
        /// не нужно выбирать актуальные записи
        /// </summary>
        public bool IsSimple { get; set; }
        /// <summary>
        /// Является ли данная таблица таблицей-регистром
        /// </summary>
        public bool IsRegister { get; set; }

        /// <summary>
        /// Признак версионности сущности
        /// </summary>
        public bool IsNotVersioned { get; set; }

        /// <summary>
        /// Псевдоним таблицы в view
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Имя таблицы
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// Тип джойна, иннер по дефолту
        /// </summary>
        public JoinType JoinType { get; set; }

        /// <summary>
        /// проперти имена которые являются уникальными, чтобы для них не добавлять префикс
        /// </summary>
        public IEnumerable<string> UniqueNamedProperties { get; set; }

        public IEnumerable<string> DistinctProps { get; set; }
    }
}
