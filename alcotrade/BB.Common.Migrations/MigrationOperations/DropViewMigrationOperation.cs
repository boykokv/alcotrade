using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Model;
using BB.Common.Migrations.Interfaces;

namespace BB.Common.Migrations.MigrationOperations
{
    public class DropViewMigrationOperation : MigrationOperation, ITemplatable
    {
        /// <summary>
        /// Имя SQL VIEW
        /// </summary>
        public string ViewName { get; private set; }
    

        /// <summary>
        /// Схема
        /// </summary>
        public string Schema { get; set; }

        public DropViewMigrationOperation(string viewName, string schema = "dbo")
            : base(null)
        {
            ViewName = viewName;
            Schema = schema;
        }

        public override bool IsDestructiveChange
        {
            get { return false; }
        }

        public IEnumerable<string> Template
        {
            get
            {
                var result = new List<string>
                             {
                                 string.Format(
                                     "IF  EXISTS (SELECT * FROM sys.views WHERE object_id = OBJECT_ID(N'[{2}].[{0}]')){1}" +
                                     "DROP VIEW [{2}].[{0}]", ViewName, Environment.NewLine,Schema)
                             };
                return result;
            }
        }
    }
}
