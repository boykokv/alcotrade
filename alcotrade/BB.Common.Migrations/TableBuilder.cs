using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Linq.Expressions;
using BB.Common.Migrations.Extensions;
using BB.Common.Migrations.Utils;

namespace BB.Common.Migrations
{
    public class ProcedureBuilder<TParameters>
    {
        private readonly CreateProcedureOperation _createProcedureOperation;
        private readonly DbMigration _migration;
        public ProcedureBuilder(CreateProcedureOperation createProcedureOperation, DbMigration migration)
        {
            RuntimeFailureMethods.Requires(createProcedureOperation != null, null, "createProcedureOperation != null");
            this._createProcedureOperation = createProcedureOperation;
            this._migration = migration;
        }
    }

    public class TableBuilder<TColumns>
    {
        private readonly CreateTableOperation _createTableOperation;
        private readonly DbMigration _migration;
        public TableBuilder(CreateTableOperation createTableOperation, DbMigration migration)
        {
            RuntimeFailureMethods.Requires(createTableOperation != null, null, "createTableOperation != null");
            this._createTableOperation = createTableOperation;
            this._migration = migration;
        }
        public TableBuilder<TColumns> PrimaryKey(Expression<Func<TColumns, object>> keyExpression, string name = null, object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(keyExpression != null, null, "keyExpression != null");
            var addPrimaryKeyOperation = new AddPrimaryKeyOperation(anonymousArguments)
            {
                Name = name
            };
            (
                keyExpression.GetPropertyAccessList().Select(p => p.First().Name)).Each(delegate(string c)
                {
                    addPrimaryKeyOperation.Columns.Add(c);
                }
            );
            this._createTableOperation.PrimaryKey = addPrimaryKeyOperation;
            return this;
        }
        public TableBuilder<TColumns> Index(Expression<Func<TColumns, object>> indexExpression, bool unique = false, object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(indexExpression != null, null, "indexExpression != null");
            var createIndexOperation = new CreateIndexOperation(anonymousArguments)
            {
                Table = this._createTableOperation.Name,
                IsUnique = unique
            };
            (
                indexExpression.GetPropertyAccessList().Select(p => p.First().Name)).Each(delegate(string c)
                {
                    createIndexOperation.Columns.Add(c);
                }
            );
            this._migration.AddOperation(createIndexOperation);
            return this;
        }
        public TableBuilder<TColumns> ForeignKey(string principalTable, Expression<Func<TColumns, object>> dependentKeyExpression, bool cascadeDelete = false, string name = null, object anonymousArguments = null)
        {
            return ForeignKey(principalTable, null, dependentKeyExpression, cascadeDelete, name, anonymousArguments);
        }

        public TableBuilder<TColumns> ForeignKey(string principalTable, IEnumerable<string> principalColumns, Expression<Func<TColumns, object>> dependentKeyExpression, bool cascadeDelete = false, string name = null,
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(principalTable), null, "!string.IsNullOrWhiteSpace(principalTable)");
            RuntimeFailureMethods.Requires(dependentKeyExpression != null, null, "dependentKeyExpression != null");
            var addForeignKeyOperation = new AddForeignKeyOperation(anonymousArguments)
            {
                Name = name,
                PrincipalTable = principalTable,
                DependentTable = this._createTableOperation.Name,
                CascadeDelete = cascadeDelete
            };
            if (principalColumns != null)
            {
                principalColumns.Each(addForeignKeyOperation.PrincipalColumns.Add);
            }
            (
                dependentKeyExpression.GetPropertyAccessList().Select(p => p.First().Name)).Each(
                    c => addForeignKeyOperation.DependentColumns.Add(c)
                );
            this._migration.AddOperation(addForeignKeyOperation);
            return this;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string ToString()
        {
            return base.ToString();
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new Type GetType()
        {
            return base.GetType();
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected new object MemberwiseClone()
        {
            return base.MemberwiseClone();
        }
    }
}