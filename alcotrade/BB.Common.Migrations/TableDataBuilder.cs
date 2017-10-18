using System.Collections.Generic;
using System.Text;

namespace BB.Common.Migrations
{
    public class DataInsertBuilder
    {
        private readonly DbMigration _migration;
        private readonly string _tableName;
        private readonly string[] _columns;

        public DataInsertBuilder(string tableName, string[] columns, DbMigration migration)
        {
            this._columns = columns;
            this._migration = migration;
            this._tableName = tableName;
        }

        public DataInsertBuilder InsertOrUpdate(params object[] values)
        {
            var op = CreateCommand(values);
            _migration.AddDataOperation(op);
            return this;
        }

        protected virtual DataSeedOperation CreateCommand(object[] values)
        {
            var ret = new DataSeedOperation();
            ret.Sql = string.Format("INSERT INTO {0} ({1}) Values(@{2})",
                _tableName, string.Join(",", _columns), string.Join(",@", _columns));

            ret.Prms = new Dictionary<string, object>(_columns.Length);
            for (var k = 0; k < _columns.Length; k++)
                ret.Prms.Add(_columns[k], values[k]);
            return ret;
        }
    }

    public class DataUpdateBuilder
    {
        private readonly DbMigration _migration;
        private readonly string _tableName;
        private readonly string[] _columns;
        private readonly string _keyColumn;

        public DataUpdateBuilder(string tableName, string keyColumn, string[] columns, DbMigration migration)
        {
            _columns = columns;
            _migration = migration;
            _tableName = tableName;
            _keyColumn = keyColumn;
        }

        public DataUpdateBuilder Update(object keyValue, params object[] values)
        {
            var op = CreateCommand(keyValue, values);
            _migration.AddDataOperation(op);
            return this;
        }

        protected virtual DataSeedOperation CreateCommand(object keyValue, params object[] values)
        {
            var ret = new DataSeedOperation();

            var sqlBuilder = new StringBuilder(string.Format("UPDATE {0} SET", _tableName));
            foreach (var column in _columns)
                sqlBuilder.AppendFormat(" {0} = @{0},", column);
            ret.Sql = sqlBuilder.ToString(0, sqlBuilder.Length - 1) + string.Format(" WHERE {0} = @{0}", _keyColumn);

            ret.Prms = new Dictionary<string, object>(_columns.Length);
            for (var k = 0; k < _columns.Length; k++)
                ret.Prms.Add(_columns[k], values[k]);
            ret.Prms.Add(_keyColumn, keyValue);

            return ret;
        }
    }
}
