using System;
using System.Collections.Generic;
using System.Data.Entity.Design.PluralizationServices;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BB.Common.Migrations.Models
{
    /// <summary>
    /// Данный для генерации SQL view
    /// </summary>
    public class SqlViewModel
    {
        private readonly string _viewName;
        private readonly string _schema;

        private readonly PluralizationService _pluralizationService;

        private SqlViewDataContainer _root;

        private readonly List<SqlViewDataContainer> _joined = new List<SqlViewDataContainer>();


        /// <summary>
        /// Создание контейнера для SQL view
        /// </summary>
        /// <param name="viewName">Имя SQL view</param>
        /// <param name="schema"></param>
        public SqlViewModel(string viewName, string schema = "dbo")
        {
            _viewName = viewName;
            _schema = schema;
            _pluralizationService = PluralizationService.CreateService(new CultureInfo("en-US"));
        }

        /// <summary>
        /// Создает SQL скрипт для создания view
        /// </summary>
        /// <returns> SQL запрос </returns>
        public string CreateView()
        {
            var builder = new StringBuilder();
            builder.AppendFormat("CREATE VIEW [{2}].[{0}] AS {1}", _viewName, Environment.NewLine, _schema);

            CreateSelectParams(builder);

            CreateInnerFrom(builder);

            CreateJoins(builder);
            if (!_root.IsNotVersioned)
            {
                builder.AppendFormat("WHERE {0}.DeleteInVersionId is NULL", _root.Alias);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Добавляет список параметров возвращаемый через view
        /// </summary>
        /// <param name="builder">SQL запрос</param>
        private void CreateSelectParams(StringBuilder builder)
        {
            builder.AppendFormat("SELECT {0}", Environment.NewLine);

            var isCommaNeeded = false;

            foreach (var property in _root.Properties)
            {
                if (isCommaNeeded)
                {
                    builder.AppendFormat(", {0}", Environment.NewLine);
                }
                builder.AppendFormat("{0}.{1} as '{1}'", _root.Alias, property);
                isCommaNeeded = true;
            }


            foreach (var joinContainer in _joined)
            {
                foreach (var joinProperty in joinContainer.Properties)
                {
                    builder.AppendFormat(", {0}", Environment.NewLine);

                    var isNotUniqueJoinTable = _joined.Count(x => x.Alias == joinContainer.Alias) > 1;

                    var isUniqueNamedProperty = joinContainer.UniqueNamedProperties.Any(x => x == joinProperty);

                    if (!isUniqueNamedProperty)
                    {
                        builder.AppendFormat("{0}.{1} as '{2}_{1}'", joinContainer.Alias, joinProperty, isNotUniqueJoinTable ? joinContainer.Table : joinContainer.Alias);
                    }
                    else
                    {
                        builder.AppendFormat(isNotUniqueJoinTable ? "{0}.{1} as '{0}_{1}'" : "{0}.{1} as '{1}'", joinContainer.Alias, joinProperty);
                    }

                }
            }
        }

        /// <summary>
        /// Добавляет FROM для главной таблицы SQL view
        /// </summary>
        /// <param name="builder">SQL запрос</param>
        private void CreateInnerFrom(StringBuilder builder)
        {
            builder.Append(Environment.NewLine);
            builder.AppendFormat("FROM [{3}].[{0}] AS {1} {2}", _root.Table, _root.Alias, Environment.NewLine, _schema);

            if (_root.IsSimple) return;

            if (_root.IsRegister)
            {
                _root.InnerKeys = _root.InnerKeys.Concat(new[] { "UseFromDateTime" }).ToArray();
            }

            //builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);
            //builder.AppendFormat("{0},  MAX(VersionId) AS MaxVersion {1}", string.Join(",", _root.InnerKeys), Environment.NewLine);
            //builder.AppendFormat("FROM [{2}].[{0}] {1}", _root.Table, Environment.NewLine, _schema);
            //builder.AppendFormat("GROUP BY {0}) AS {1}grouped {2}", string.Join(",", _root.InnerKeys), _root.Alias, Environment.NewLine);
            //var groupExpression = string.Join(" AND ",
            //    _root.InnerKeys.Select(s => string.Format("(({0}.{1}= {0}grouped.{1}) OR ({0}.{1} is NULL AND {0}grouped.{1} is NULL))", _root.Alias, s)));
            //builder.AppendFormat("ON {1} AND {0}.VersionId = {0}grouped.MaxVersion {2}", _root.Alias, groupExpression, Environment.NewLine);

        }

        /// <summary>
        /// Добавляет join таблицы в запрос
        /// </summary>
        /// <param name="builder">SQL запрос</param>
        private void CreateJoins(StringBuilder builder)
        {
            foreach (var joinDataContainer in _joined)
            {
                if (joinDataContainer.DistinctProps != null && joinDataContainer.DistinctProps.Any())
                    CreateManualMaxValuesJoin(builder, joinDataContainer);
                else
                {
                    if (joinDataContainer.IsSimple)
                    {
                        CreateDictionaryJoin(builder, joinDataContainer);
                    }
                    else
                    {
                        CreateDictionaryVersionedJoin(builder, joinDataContainer);
                    }

                    //if (joinDataContainer.IsRegister)
                    //{
                    //    CreateRegisterJoin(builder, joinDataContainer);
                    //}
                    //else
                    //{

                    //    if (joinDataContainer.IsSimple)
                    //    {
                    //        CreateDictionaryJoin(builder, joinDataContainer);
                    //    }
                    //    else
                    //    {
                    //        CreateVersionJoin(builder, joinDataContainer);
                    //    }
                    //}
                }
            }
        }

        private void CreateManualMaxValuesJoin(StringBuilder builder, SqlViewDataContainer joinDataContainer)
        {
            switch (joinDataContainer.JoinType)
            {
                case JoinType.Inner:
                    builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);
                    break;
                case JoinType.Left:
                    builder.AppendFormat("LEFT JOIN (SELECT {0}", Environment.NewLine);
                    break;
                default:
                    throw new Exception(String.Format("Unsupported join: {0}", joinDataContainer.JoinType));

            }

            var isCommaNeeded = false;
            foreach (var property in joinDataContainer.Properties)
            {
                if (isCommaNeeded)
                {
                    builder.AppendFormat(", {0}", Environment.NewLine);
                }
                builder.AppendFormat("{0}inner.{1}", joinDataContainer.Alias, property);
                isCommaNeeded = true;
            }

            foreach (var outerKey in joinDataContainer.OuterKeys)
            {
                if (!joinDataContainer.Properties.Contains(outerKey))
                {
                    builder.AppendFormat(",{2}{0}inner.{1}", joinDataContainer.Alias, outerKey, Environment.NewLine);
                }
            }


            builder.Append(Environment.NewLine);

            builder.AppendFormat("FROM [{3}].[{0}] AS {1}inner {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);

            builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);

            //builder.AppendFormat("{0}, MAX(UseFromDateTime) as MaxFromDateTime,  MAX(VersionId) AS MaxVersion {1}", string.Join(",", joinDataContainer.OuterKeys), Environment.NewLine);

            builder.AppendFormat("{0}", string.Join(",", joinDataContainer.OuterKeys));

            if (joinDataContainer.DistinctProps != null)
                foreach (string prop in joinDataContainer.DistinctProps)
                    builder.AppendFormat(", MAX({0}) as Max{0}", prop);

            builder.AppendFormat(Environment.NewLine);


            builder.AppendFormat("FROM [{2}].[{0}] {1}", joinDataContainer.Table, Environment.NewLine, _schema);


            builder.AppendFormat("GROUP BY {0}) AS {1}innergrouped {2}", string.Join(",", joinDataContainer.OuterKeys), joinDataContainer.Alias, Environment.NewLine);

            var groupExpression = string.Join(" AND ",
                   joinDataContainer.OuterKeys.Select(s => string.Format("(({0}inner.{1}= {0}innergrouped.{1}) OR ({0}inner.{1} is NULL AND {0}innergrouped.{1} is NULL))", joinDataContainer.Alias, s)));

            //builder.AppendFormat("ON {1} AND {0}inner.UseFromDateTime = {0}innergrouped.MaxFromDateTime AND {0}inner.VersionId = {0}innergrouped.MaxVersion) AS {2} {3}", joinDataContainer.Alias,
            //        groupExpression, joinDataContainer.Alias, Environment.NewLine);

            builder.AppendFormat("ON {0} ", groupExpression);

            if (joinDataContainer.DistinctProps != null)
                foreach (string prop in joinDataContainer.DistinctProps)
                    builder.AppendFormat(" AND {0}inner.{1} = {0}innergrouped.Max{1}", joinDataContainer.Alias, prop);

            builder.AppendFormat(") AS {0} {1}", joinDataContainer.Alias, Environment.NewLine);

            var equalExpression = joinDataContainer.InnerKeys.Select((t, i) => string.Format("{0}.{1} = {2}.{3}",
                                  _root.Alias, t, joinDataContainer.Alias, joinDataContainer.OuterKeys[i]));

            builder.AppendFormat("ON {0} {1}", string.Join(" AND ", equalExpression), Environment.NewLine);
        }

        private void CreateRegisterJoin(StringBuilder builder, SqlViewDataContainer joinDataContainer)
        {
            switch (joinDataContainer.JoinType)
            {
                case JoinType.Inner:
                    builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);
                    break;
                case JoinType.Left:
                    builder.AppendFormat("LEFT JOIN (SELECT {0}", Environment.NewLine);
                    break;
                default:
                    throw new Exception(String.Format("Unsupported join: {0}", joinDataContainer.JoinType));

            }

            var isCommaNeeded = false;
            foreach (var property in joinDataContainer.Properties)
            {
                if (isCommaNeeded)
                {
                    builder.AppendFormat(", {0}", Environment.NewLine);
                }
                builder.AppendFormat("{0}inner.{1}", joinDataContainer.Alias, property);
                isCommaNeeded = true;
            }

            foreach (var outerKey in joinDataContainer.OuterKeys)
            {
                if (!joinDataContainer.Properties.Contains(outerKey))
                {
                    builder.AppendFormat(",{2}{0}inner.{1}", joinDataContainer.Alias, outerKey, Environment.NewLine);
                }
            }


            builder.Append(Environment.NewLine);

            builder.AppendFormat("FROM [{3}].[{0}] AS {1}inner {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);

            builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);

            builder.AppendFormat("{0}, MAX(UseFromDateTime) as MaxFromDateTime,  MAX(VersionId) AS MaxVersion {1}", string.Join(",", joinDataContainer.OuterKeys), Environment.NewLine);

            builder.AppendFormat("FROM [{2}].[{0}] {1}", joinDataContainer.Table, Environment.NewLine, _schema);

            builder.AppendFormat("GROUP BY {0}) AS {1}innergrouped {2}", string.Join(",", joinDataContainer.OuterKeys), joinDataContainer.Alias, Environment.NewLine);

            var groupExpression = string.Join(" AND ",
                   joinDataContainer.OuterKeys.Select(s => string.Format("(({0}inner.{1}= {0}innergrouped.{1}) OR ({0}inner.{1} is NULL AND {0}innergrouped.{1} is NULL))", joinDataContainer.Alias, s)));

            builder.AppendFormat("ON {1} AND {0}inner.UseFromDateTime = {0}innergrouped.MaxFromDateTime AND {0}inner.VersionId = {0}innergrouped.MaxVersion) AS {2} {3}", joinDataContainer.Alias,
                                groupExpression, joinDataContainer.Alias, Environment.NewLine);

            var equalExpression = joinDataContainer.InnerKeys.Select((t, i) => string.Format("{0}.{1} = {2}.{3}",
                                  _root.Alias, t, joinDataContainer.Alias, joinDataContainer.OuterKeys[i]));

            builder.AppendFormat("ON {0} {1}", string.Join(" AND ", equalExpression), Environment.NewLine);
        }

        private void CreateVersionJoin(StringBuilder builder, SqlViewDataContainer joinDataContainer)
        {
            switch (joinDataContainer.JoinType)
            {
                case JoinType.Inner:
                    builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);
                    break;
                case JoinType.Left:
                    builder.AppendFormat("LEFT JOIN (SELECT {0}", Environment.NewLine);
                    break;
                default:
                    throw new Exception(String.Format("Unsupported join: {0}", joinDataContainer.JoinType));

            }

            var isCommaNeeded = false;
            foreach (var property in joinDataContainer.Properties)
            {
                if (isCommaNeeded)
                {
                    builder.AppendFormat(", {0}", Environment.NewLine);
                }
                builder.AppendFormat("{0}inner.{1}", joinDataContainer.Alias, property);
                isCommaNeeded = true;
            }

            foreach (var outerKey in joinDataContainer.OuterKeys)
            {
                if (!joinDataContainer.Properties.Contains(outerKey))
                {
                    builder.AppendFormat(",{2}{0}inner.{1}", joinDataContainer.Alias, outerKey, Environment.NewLine);
                }
            }


            builder.Append(Environment.NewLine);

            builder.AppendFormat("FROM [{3}].[{0}] AS {1}inner {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);

            builder.AppendFormat("INNER JOIN (SELECT {0}", Environment.NewLine);

            builder.AppendFormat("{0},  MAX(VersionId) AS MaxVersion {1}", string.Join(",", joinDataContainer.OuterKeys), Environment.NewLine);

            builder.AppendFormat("FROM [{2}].[{0}] {1}", joinDataContainer.Table, Environment.NewLine, _schema);

            builder.AppendFormat("GROUP BY {0}) AS {1}innergrouped {2}", string.Join(",", joinDataContainer.OuterKeys), joinDataContainer.Alias, Environment.NewLine);

            var groupExpression = string.Join(" AND ",
                   joinDataContainer.OuterKeys.Select(s => string.Format("{0}inner.{1}= {0}innergrouped.{1}", joinDataContainer.Alias, s)));

            builder.AppendFormat("ON {1} AND {0}inner.VersionId = {0}innergrouped.MaxVersion) AS {2} {3}", joinDataContainer.Alias,
                                groupExpression, joinDataContainer.Alias, Environment.NewLine);

            var equalExpression = joinDataContainer.InnerKeys.Select((t, i) => string.Format("{0}.{1} = {2}.{3}",
                                  _root.Alias, t, joinDataContainer.Alias, joinDataContainer.OuterKeys[i]));

            builder.AppendFormat("ON {0} {1}", string.Join(" AND ", equalExpression), Environment.NewLine);
        }

        /// <summary>
        /// Добавляет join c таблицей-словарем в запрос
        /// </summary>
        /// <param name="builder">SQL запрос</param>
        /// <param name="joinDataContainer">Данные о join таблице</param>
        private void CreateDictionaryJoin(StringBuilder builder, SqlViewDataContainer joinDataContainer)
        {

            switch (joinDataContainer.JoinType)
            {
                case JoinType.Inner:
                    builder.AppendFormat("INNER JOIN [{3}].[{0}] as {1} {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);
                    break;
                case JoinType.Left:
                    builder.AppendFormat("LEFT JOIN [{3}].[{0}] as {1} {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);
                    break;
                default:
                    throw new Exception(String.Format("Unsupported join: {0}", joinDataContainer.JoinType));

            }

            var equalExpression = joinDataContainer.InnerKeys.Select((t, i) => string.Format("{0}.{1} = {2}.{3}",
                                  _root.Alias, t, joinDataContainer.Alias, joinDataContainer.OuterKeys[i]));

            builder.AppendFormat("ON {0} {1}", string.Join(" AND ", equalExpression), Environment.NewLine);
        }

        private void CreateDictionaryVersionedJoin(StringBuilder builder, SqlViewDataContainer joinDataContainer)
        {

            switch (joinDataContainer.JoinType)
            {
                case JoinType.Inner:
                    builder.AppendFormat("INNER JOIN [{3}].[{0}] as {1} {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);
                    break;
                case JoinType.Left:
                    builder.AppendFormat("LEFT JOIN [{3}].[{0}] as {1} {2}", joinDataContainer.Table, joinDataContainer.Alias, Environment.NewLine, _schema);
                    break;
                default:
                    throw new Exception(String.Format("Unsupported join: {0}", joinDataContainer.JoinType));

            }

            var equalExpression = joinDataContainer.InnerKeys.Select((t, i) => string.Format("{0}.{1} = {2}.{3}",
                                  _root.Alias, t, joinDataContainer.Alias, joinDataContainer.OuterKeys[i]));

            builder.AppendFormat("ON {0} {1}", string.Join(" AND ", equalExpression), Environment.NewLine);

            builder.AppendFormat("AND {0}.DeleteInVersionId is NULL {1}", joinDataContainer.Alias, Environment.NewLine);
        }


        /// <summary>
        /// Инициализирует главную таблицу для SQL view
        /// </summary>
        /// <param name="table">Базовая таблица</param>
        /// <param name="properties">Возвращаемые поля</param>
        /// <param name="key">Ключевое поле</param>
        /// <param name="isRegister">Реестр (наличие периодов действий в одной версии)</param>
        /// <param name="isSimple">генерации кода для выборкиа актуальных значений не требуется</param>
        /// <param name="isNotVersioned">признак того что сущность версионная</param>
        /// <returns>Инициализированный view контейнер</returns>
        public SqlViewModel Init(string table, string[] properties, string[] key, bool isRegister, bool isSimple = false, bool isNotVersioned = false)
        {
            if (string.IsNullOrEmpty(table)) throw new ArgumentNullException("table");
            if (key == null || key.Length == 0) throw new ArgumentNullException("key");
            if (properties == null) throw new ArgumentNullException("properties");

            _root = new SqlViewDataContainer
            {
                IsSimple = isSimple,
                IsRegister = isRegister,
                InnerKeys = key,
                Properties = properties,
                Table = table,
                IsNotVersioned = isNotVersioned,
                Alias = _pluralizationService.IsPlural(table) ? _pluralizationService.Singularize(table).ToLower() : table.ToLower()
            };
            return this;
        }


        /// <summary>
        /// Join c главной таблицей 
        /// </summary>
        /// <param name="innerTable">Присоединяймая таблица</param>
        /// <param name="properties">Возвращаемые поля присоединяймой таблицы</param>
        /// <param name="outerKey">Ключевые поля присоединяймой таблицы</param>
        /// <param name="innerKey">Ключевые поля базовой таблицы</param>
        /// <param name="isRegister">Поддержка версионности</param>
        /// <param name="alias">Псевданим для присоеденяймой таблицы, используется при множественных ссылках на одну таблицу</param>
        /// <returns>Инициализированный view контейнер</returns>
        public SqlViewModel Join(string innerTable, string[] properties, string[] outerKey, string[] innerKey, bool isRegister, string alias = null)
        {
            if (string.IsNullOrEmpty(innerTable)) throw new ArgumentNullException("innerTable");
            if (outerKey == null || outerKey.Length == 0) throw new ArgumentNullException("outerKey");
            if (innerKey == null || innerKey.Length == 0) throw new ArgumentNullException("innerKey");
            if (properties == null) throw new ArgumentNullException("properties");

            if (outerKey.Length != innerKey.Length)
                throw new Exception("Количество ключей не совпадает");

            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = _pluralizationService.IsPlural(innerTable)
                    ? _pluralizationService.Singularize(innerTable).ToLower()
                    : innerTable.ToLower();
            }

            _joined.Add(new SqlViewDataContainer
            {
                IsRegister = isRegister,
                InnerKeys = innerKey,
                OuterKeys = outerKey,
                Properties = properties,
                Table = innerTable,
                Alias = alias
            });
            return this;
        }

        public SqlViewModel LeftJoin(string innerTable, string[] properties, string[] outerKey, string[] innerKey, bool isRegister = false, string alias = null, IEnumerable<string> distinctProps = null)
        {
            if (string.IsNullOrEmpty(innerTable)) throw new ArgumentNullException("innerTable");
            if (outerKey == null || outerKey.Length == 0) throw new ArgumentNullException("outerKey");
            if (innerKey == null || innerKey.Length == 0) throw new ArgumentNullException("innerKey");
            if (properties == null) throw new ArgumentNullException("properties");

            if (outerKey.Length != innerKey.Length)
                throw new Exception("Количество ключей не совпадает");

            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = _pluralizationService.IsPlural(innerTable)
                    ? _pluralizationService.Singularize(innerTable).ToLower()
                    : innerTable.ToLower();
            }

            _joined.Add(new SqlViewDataContainer
            {
                IsRegister = isRegister,
                InnerKeys = innerKey,
                OuterKeys = outerKey,
                Properties = properties,
                Table = innerTable,
                Alias = alias,
                JoinType = JoinType.Left,
                DistinctProps = distinctProps
            });
            return this;
        }

        public SqlViewModel LeftSimpleJoin(string innerTable, string[] properties, string[] outerKey, string[] innerKey, string alias = null, params string[] uniqueProperties)
        {
            if (string.IsNullOrEmpty(innerTable)) throw new ArgumentNullException("innerTable");
            if (outerKey == null || outerKey.Length == 0) throw new ArgumentNullException("outerKey");
            if (innerKey == null || innerKey.Length == 0) throw new ArgumentNullException("innerKey");
            if (properties == null) throw new ArgumentNullException("properties");

            if (outerKey.Length != innerKey.Length)
                throw new Exception("Количество ключей не совпадает");

            if (string.IsNullOrWhiteSpace(alias))
            {
                alias = _pluralizationService.IsPlural(innerTable)
                    ? _pluralizationService.Singularize(innerTable).ToLower()
                    : innerTable.ToLower();
            }

            _joined.Add(new SqlViewDataContainer
            {
                IsSimple = true,
                InnerKeys = innerKey,
                OuterKeys = outerKey,
                Properties = properties,
                Table = innerTable,
                Alias = alias,
                JoinType = JoinType.Left,
                UniqueNamedProperties = uniqueProperties

            });
            return this;
        }
    }
}
