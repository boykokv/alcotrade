using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.Interfaces;

namespace BB.Common.Migrations.MigrationOperations
{
    /// <summary>
    ///������������ ������ ��� ��� �������, ����� ����������� ������� �� �������
    /// </summary>
    public class RawSqlMigrationOperation : MigrationOperation, ITemplatable
    {
        private readonly string[] _rawSql;

        public RawSqlMigrationOperation(params string[] rawSql) : base(null)
        {
            _rawSql = rawSql;
        }

        public override bool IsDestructiveChange
        {
            get { return false; }
        }

        public IEnumerable<string> Template
        {
            get {return _rawSql; }
        }
    }
}