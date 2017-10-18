using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.Interfaces;
using BB.Common.Migrations.Models;

namespace BB.Common.Migrations.MigrationOperations
{
    public class ActualEntitiesViewMigrationOperation : MigrationOperation, ITemplatable
    {
        private readonly SqlViewModel _queryModel;

        public ActualEntitiesViewMigrationOperation(SqlViewModel queryModel)
            : base(null)
        {
            _queryModel = queryModel;
        }

        /// <summary>
        /// Возвращает SQL троку для миграции
        /// </summary>
        public IEnumerable<string> Template
        {
            get
            {
                var result = new[] { _queryModel.CreateView() };
                return result;
            }
        }

        /// <summary>
        /// Gets a value indicating if this operation may result in data loss.
        /// </summary>
        public override bool IsDestructiveChange
        {
            get { return false; }
        }
    }
}
