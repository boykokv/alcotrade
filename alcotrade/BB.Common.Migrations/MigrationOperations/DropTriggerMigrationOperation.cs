using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.Interfaces;

namespace BB.Common.Migrations.MigrationOperations
{
    /// <summary>
    /// Удаляет созданные триггеры 
    /// </summary>
    public class DropTriggerMigrationOperation : MigrationOperation, ITemplatable
    {
        private readonly string _schema;
        private readonly string _triggerName;

        public DropTriggerMigrationOperation(string triggerName, string schema = "dbo")
            : base(null)
        {
            _schema = schema;
            _triggerName = triggerName;
        }

        /// <summary>
        /// Возвращает SQL троку для миграции
        /// </summary>
        public IEnumerable<string> Template
        {
            get
            {
                var result = new List<string>
                {
                    string.Format(
                        "IF  EXISTS (SELECT * FROM sys.triggers WHERE object_id = OBJECT_ID(N'[{0}].[{1}]'))  " +
                        Environment.NewLine +
                        "DROP TRIGGER [{0}].[{1}]" + Environment.NewLine,
                        _schema, _triggerName)
                };


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