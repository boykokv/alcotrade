using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BB.Common.Migrations.Enums;
using BB.Common.Migrations.Extensions;
using BB.Common.Migrations.Models;

namespace BB.Common.Migrations
{
    public abstract class BaseMigration : DbMigration
    {
        protected const int Checksum = 1;

        protected void DropActualView(string tableName)
        {
            this.DropView(tableName.GetViewName());
        }

        protected void RawSql(params string[] rawSql)
        {
            (this as DbMigration).RawSql(rawSql);
        }

        protected void ActualView(MigrationActionType action, string tableName, string[] properties, string[] keys, Func<SqlViewModel, SqlViewModel> sqlAction = null, bool isRegister = false, bool isSimple = false)
        {
            if (action == MigrationActionType.Up)
            {
                var viewModel = new SqlViewModel(tableName.GetViewName()).Init(tableName, properties, keys, isRegister, isSimple, true);
                this.ActualEntitiesView(sqlAction != null ? sqlAction(viewModel) : viewModel);
            }
            else
            {
                this.DropView(tableName.GetViewName());
            }
        }

        protected void ActualView(MigrationActionType action, string tableName, string viewName, string[] properties, string[] keys, Func<SqlViewModel, SqlViewModel> sqlAction = null, bool isRegister = false, bool isSimple = false)
        {
            if (action == MigrationActionType.Up)
            {
                var viewModel = new SqlViewModel(viewName.GetViewName()).Init(tableName, properties, keys, isRegister, isSimple, true);
                this.ActualEntitiesView(sqlAction != null ? sqlAction(viewModel) : viewModel);
            }
            else
            {
                this.DropView(tableName.GetViewName());
            }
        }

    }
}
