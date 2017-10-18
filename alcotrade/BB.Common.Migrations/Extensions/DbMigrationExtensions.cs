using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.MigrationOperations;
using BB.Common.Migrations.Models;

namespace BB.Common.Migrations.Extensions
{
    public static class DbMigrationExtensions
    {
        public static void ForeignKeyTrigger(this DbMigration migration, string foreignKeyTable, string foreignKeyColumn, string primaryKeyTable, string primaryKeyColumn, string schema = "dbo")
        {
            var operartions = migration.Operations as IList<MigrationOperation>;
            
            if (operartions != null)
                operartions.Add(new ForeignKeyTriggerMigrationOperation(foreignKeyTable, foreignKeyColumn, primaryKeyTable, primaryKeyColumn,schema));
        }

        public static void DropForeignKeyTrigger(this DbMigration migration, string foreignKeyTable, string foreignKeyColumn, string primaryKeyTable, string primaryKeyColumn, string schema = "dbo")
        {
            var operartions = migration.Operations as IList<MigrationOperation>;
            if (operartions != null)
                operartions.Add(new DropForeignKeyTriggerMigrationOperation(foreignKeyTable, foreignKeyColumn, primaryKeyTable, primaryKeyColumn, schema));
        }

        public static void DropTrigger(this DbMigration migration, string triggerName, string schema = "dbo")
        {
            var operartions = migration.Operations as IList<MigrationOperation>;
            if (operartions != null)
                operartions.Add(new DropTriggerMigrationOperation(triggerName, schema));
        }


        public static void ActualEntitiesView(this DbMigration migration, SqlViewModel queryModel)
        {
            var operartions = migration.Operations as IList<MigrationOperation>;
            if (operartions != null)
                operartions.Add(new ActualEntitiesViewMigrationOperation(queryModel));
        }

        public static void RawSql(this DbMigration migration, params string[] rawSql)
        {
            var operartions = migration.Operations as IList<MigrationOperation>;
            if (operartions != null)
                operartions.Add(new RawSqlMigrationOperation(rawSql));
        }

        public static void DropView(this DbMigration migration, string viewName, string schema = "dbo")
        {
            if (viewName == null) throw new ArgumentNullException("viewName");
            var operartions = migration.Operations as IList<MigrationOperation>;
            if (operartions != null)
                operartions.Add(new DropViewMigrationOperation(viewName, schema));
        }
    }
}
