using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.Interfaces;

namespace BB.Common.Migrations.MigrationOperations
{
    public class ForeignKeyTriggerMigrationOperation : MigrationOperation, ITemplatable
    {
        /// <summary>
        /// Таблица на которую ссылается ForeignKeyTable
        /// </summary>
        public string PrimaryKeyTable { get; private set; }
        /// <summary>
        /// Поле на которое ссылается ForeignKeyColumn
        /// </summary>
        public string PrimaryKeyColumn { get; set; }
        /// <summary>
        /// Ссылаемая таблица
        /// </summary>
        public string ForeignKeyTable { get; set; }
        /// <summary>
        /// Ссылаемое поле
        /// </summary>
        public string ForeignKeyColumn { get; set; }

        /// <summary>
        /// Схема
        /// </summary>
        public string Schema { get; set; }

        public ForeignKeyTriggerMigrationOperation(string foreignKeyTable, string foreignKeyColumn, string primaryKeyTable, string primaryKeyColumn, string schema = "dbo")
            : base(null)
        {
            ForeignKeyTable = foreignKeyTable;
            ForeignKeyColumn = foreignKeyColumn;
            PrimaryKeyTable = primaryKeyTable;
            PrimaryKeyColumn = primaryKeyColumn;
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
                result.Add(string.Format("CREATE TRIGGER [{4}].[Check_InsertUpdate_{0}_{1}] " + Environment.NewLine +
                                       "ON  [{4}].[{0}] " + Environment.NewLine +
                                       "AFTER INSERT,UPDATE " + Environment.NewLine +
                                       "AS " + Environment.NewLine +
                                       "SET NOCOUNT ON;" + Environment.NewLine +
                                       "IF NOT EXISTS(select 1 from inserted) RETURN;"+Environment.NewLine +
                                       "IF  EXISTS (SELECT 1 FROM inserted as i WHERE i.{1} is NULL) RETURN;" + Environment.NewLine +
                                       "IF NOT EXISTS (SELECT 1 FROM {4}.{2} t " + Environment.NewLine +
                                                      "JOIN inserted AS i ON t.{3} = i.{1}) " + Environment.NewLine +
                                       "BEGIN " + Environment.NewLine +
                                           "RAISERROR ('{0}.{1} doesnot exist',1,1); " + Environment.NewLine +
                                           "ROLLBACK TRANSACTION; " + Environment.NewLine +
                                           "RETURN ;" + Environment.NewLine +
                                       "END  " + Environment.NewLine,

                                       ForeignKeyTable,
                                       ForeignKeyColumn,
                                       PrimaryKeyTable,
                                       PrimaryKeyColumn,
                                       Schema
                     ));
                result.Add(string.Format("CREATE TRIGGER [{4}].[Check_DeleteUpdate_{0}{1}] " + Environment.NewLine +
                                      "ON  [{4}].[{2}] " + Environment.NewLine +
                                      "AFTER DELETE,UPDATE " + Environment.NewLine +
                                      "AS " + Environment.NewLine +
                                      "SET NOCOUNT ON;" + Environment.NewLine +
                                      "IF (NOT EXISTS(select 1 from inserted)) AND (NOT EXISTS(select 1 from deleted)) RETURN;" + Environment.NewLine +
                                      "IF NOT EXISTS (SELECT 1 FROM {4}.{2} as p" + Environment.NewLine +
                                               "JOIN deleted as d on p.{3} = d.{3})" + Environment.NewLine +
                                      "BEGIN" + Environment.NewLine +
                                          "IF EXISTS (SELECT 1 FROM {4}.{0} as f" + Environment.NewLine +
                                                     "JOIN deleted as d on f.{1} = d.{3})" + Environment.NewLine +
                                          "BEGIN" + Environment.NewLine +
                                              "RAISERROR ('Record cannot be deleted because it used in {4}.{0}',1,1);" + Environment.NewLine +
                                              "ROLLBACK TRANSACTION;" + Environment.NewLine +
                                              "RETURN ;" + Environment.NewLine +
                                          "END" + Environment.NewLine +
                                      "END;" + Environment.NewLine,

                                      ForeignKeyTable,
                                      ForeignKeyColumn,
                                      PrimaryKeyTable,
                                      PrimaryKeyColumn,
                                      Schema
                    ));

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
