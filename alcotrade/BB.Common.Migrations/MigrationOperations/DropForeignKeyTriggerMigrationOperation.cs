using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.Interfaces;

namespace BB.Common.Migrations.MigrationOperations
{
    /// <summary>
    /// Удаляет созданные foreign key триггеры 
    /// </summary>
    public class DropForeignKeyTriggerMigrationOperation : MigrationOperation, ITemplatable
    {
        /// <summary>
        /// Ссылаемая таблица
        /// </summary>
        public string ForeignKeyTable { get; set; }
        /// <summary>
        /// Ссылаемое поле
        /// </summary>
        public string ForeignKeyColumn { get; set; }

        public string PrimaryKeyColumn { get; set; }

        public string PrimaryKeyTable { get; private set; }

        /// <summary>
        /// Схема
        /// </summary>
        public string Schema { get; set; }

        public DropForeignKeyTriggerMigrationOperation(string foreignKeyTable, string foreignKeyColumn, string primaryKeyTable, string primaryKeyColumn, string schema = "dbo")
            : base(null)
        {
            ForeignKeyTable = foreignKeyTable;
            ForeignKeyColumn = foreignKeyColumn;
            PrimaryKeyColumn = primaryKeyColumn;
            PrimaryKeyTable = primaryKeyTable;
            Schema = schema;
        }

        /// <summary>
        /// Возвращает SQL троку для миграции
        /// </summary>
        public IEnumerable<string> Template
        {
            get
            {
                var result = new List<string>();
                result.Add(string.Format("IF  EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[{4}].[Check_InsertUpdate_{0}_{1}]'))  " + Environment.NewLine +
                                         "DROP TRIGGER [{4}].[Check_InsertUpdate_{0}_{1}]" + Environment.NewLine,
                    ForeignKeyTable,
                    ForeignKeyColumn,
                    PrimaryKeyTable,
                    PrimaryKeyColumn,
                    Schema));

                result.Add(string.Format("IF  EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[{4}].[Check_DeleteUpdate_{0}{1}]'))  " + Environment.NewLine +
                                         "DROP TRIGGER [{4}].[Check_DeleteUpdate_{0}{1}]" + Environment.NewLine,
                    ForeignKeyTable,
                    ForeignKeyColumn,
                    PrimaryKeyTable,
                    PrimaryKeyColumn,
                    Schema));

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
