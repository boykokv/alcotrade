using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Migrations.Builders;
using System.Data.Entity.Migrations.Model;
using System.Linq;
using System.Reflection;
using BB.Common.Migrations.Enums;
using BB.Common.Migrations.Extensions;
using BB.Common.Migrations.Utils;

namespace BB.Common.Migrations
{
    public abstract class DbMigration
    {
        private readonly IDictionary<string, ChangeSetModel> _changeSets4Add = new Dictionary<string, ChangeSetModel>();
        private readonly IList<string> _changeSets4Remove = new List<string>();
        private readonly List<DataSeedOperation> _dataOperations = new List<DataSeedOperation>();
        private readonly List<MigrationOperation> _operations = new List<MigrationOperation>();

        public IEnumerable<MigrationOperation> Operations
        {
            get { return _operations; }
        }

        public IEnumerable<DataSeedOperation> DataOperations
        {
            get { return _dataOperations; }
        }

        public abstract void Up();

        public virtual void Down() {}

        public virtual void Seed() {}

        public virtual void SeedForTest() {}

        protected void Procedure(MigrationActionType action, string procedureName, string bodySql, Action<string, string> createAction)
        {
            if (action == MigrationActionType.Up)
                createAction(procedureName, bodySql);
            else
                DropProcedure(procedureName);
        }

        protected void Table(MigrationActionType action, string tableName, Action<string> createAction)
        {
            if (action == MigrationActionType.Up)
                createAction(tableName);
            else
                DropTable(tableName);
        }

        protected internal DataInsertBuilder CreateInsert(string tableName, params string[] columns)
        {
            return new DataInsertBuilder(tableName, columns, this);
        }

        protected internal DataUpdateBuilder CreateUpdate(string tableName, string keyColumn, params string[] columns)
        {
            return new DataUpdateBuilder(tableName, keyColumn, columns, this);
        }

        protected internal void RawDataSeedSql(string sql)
        {
            AddDataOperation(new DataSeedOperation {Sql = sql});
        }

        protected internal TableBuilder<TColumns> CreateTable<TColumns>(string name,
            Func<ColumnBuilder, TColumns> columnsAction, string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            RuntimeFailureMethods.Requires(columnsAction != null, null, "columnsAction != null");
            name = string.Format("{0}.{1}", schema, name);
            var createTableOperation = new CreateTableOperation(name, anonymousArguments);
            AddOperation(createTableOperation);
            var columns = columnsAction(new ColumnBuilder());
            columns.GetType().GetProperties().Each(delegate(PropertyInfo p, int i)
            {
                var columnModel = p.GetValue(columns, null) as ColumnModel;
                if (columnModel != null)
                {
                    if (string.IsNullOrWhiteSpace(columnModel.Name))
                        columnModel.Name = p.Name;
                    createTableOperation.Columns.Add(columnModel);
                }
            }
                );
            return new TableBuilder<TColumns>(createTableOperation, this);
        }

        protected internal void CreateProcedure<TParameters>(string name, string bodySql,
            Func<ParameterBuilder, TParameters> parametersAction, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            RuntimeFailureMethods.Requires(parametersAction != null, null, "parametersAction != null");
            name = string.Format("{0}.{1}", schema, name);
            var createOperation = new CreateProcedureOperation(name, bodySql, anonymousArguments);
            AddOperation(createOperation);
            var parameters = parametersAction(new ParameterBuilder());
            parameters.GetType().GetProperties().Each(delegate(PropertyInfo p, int i)
            {
                var parameterModel = p.GetValue(parameters, null) as ParameterModel;
                if (parameterModel != null)
                {
                    if (string.IsNullOrWhiteSpace(parameterModel.Name))
                        parameterModel.Name = p.Name;
                    createOperation.Parameters.Add(parameterModel);
                }
            }
                );
        }

        protected internal void AddForeignKey(string dependentTable,
            string dependentColumn,
            string principalTable,
            string principalColumn = null,
            bool cascadeDelete = false,
            string name = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentTable),
                null,
                "!string.IsNullOrWhiteSpace(dependentTable)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentColumn),
                null,
                "!string.IsNullOrWhiteSpace(dependentColumn)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(principalTable),
                null,
                "!string.IsNullOrWhiteSpace(principalTable)");
            AddForeignKey(dependentTable,
                new[]
                {
                    dependentColumn
                },
                principalTable,
                (principalColumn != null)
                    ? new[]
                    {
                        principalColumn
                    }
                    : null,
                cascadeDelete,
                name,
                schema,
                anonymousArguments);
        }

        protected internal void AddForeignKey(string dependentTable,
            string[] dependentColumns,
            string principalTable,
            string[] principalColumns = null,
            bool cascadeDelete = false,
            string name = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentTable),
                null,
                "!string.IsNullOrWhiteSpace(dependentTable)");
            RuntimeFailureMethods.Requires(dependentColumns != null, null, "dependentColumns != null");
            RuntimeFailureMethods.Requires(dependentColumns.Any(), null, "dependentColumns.Any()");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(principalTable),
                null,
                "!string.IsNullOrWhiteSpace(principalTable)");
            dependentTable = string.Format("{0}.{1}", schema, dependentTable);
            principalTable = string.Format("{0}.{1}", schema, principalTable);
            var addForeignKeyOperation = new AddForeignKeyOperation(anonymousArguments)
            {
                DependentTable = dependentTable,
                PrincipalTable = principalTable,
                CascadeDelete = cascadeDelete,
                Name = name
            };
            dependentColumns.Each(c => addForeignKeyOperation.DependentColumns.Add(c)
                );
            if (principalColumns != null)
            {
                principalColumns.Each(c => addForeignKeyOperation.PrincipalColumns.Add(c)
                    );
            }
            AddOperation(addForeignKeyOperation);
        }

        protected internal void DropForeignKey(string dependentTable, string name, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentTable),
                null,
                "!string.IsNullOrWhiteSpace(dependentTable)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            dependentTable = string.Format("{0}.{1}", schema, dependentTable);
            var migrationOperation = new DropForeignKeyOperation(anonymousArguments)
            {
                DependentTable = dependentTable,
                Name = name
            };
            AddOperation(migrationOperation);
        }

        protected internal void DropForeignKey(string dependentTable,
            string dependentColumn,
            string principalTable,
            string principalColumn = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentTable),
                null,
                "!string.IsNullOrWhiteSpace(dependentTable)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentColumn),
                null,
                "!string.IsNullOrWhiteSpace(dependentColumn)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(principalTable),
                null,
                "!string.IsNullOrWhiteSpace(principalTable)");
            DropForeignKey(dependentTable,
                new[]
                {
                    dependentColumn
                },
                principalTable,
                schema,
                anonymousArguments);
        }

        protected internal void DropForeignKey(string dependentTable,
            string[] dependentColumns,
            string principalTable,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(dependentTable),
                null,
                "!string.IsNullOrWhiteSpace(dependentTable)");
            RuntimeFailureMethods.Requires(dependentColumns != null, null, "dependentColumns != null");
            RuntimeFailureMethods.Requires(dependentColumns.Any(), null, "dependentColumns.Any()");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(principalTable),
                null,
                "!string.IsNullOrWhiteSpace(principalTable)");
            dependentTable = string.Format("{0}.{1}", schema, dependentTable);
            var dropForeignKeyOperation = new DropForeignKeyOperation(anonymousArguments)
            {
                DependentTable = dependentTable,
                PrincipalTable = principalTable
            };
            dependentColumns.Each(delegate(string c) { dropForeignKeyOperation.DependentColumns.Add(c); }
                );
            AddOperation(dropForeignKeyOperation);

            principalTable = string.Format("{0}.{1}", schema, principalTable);

            var dropForeignKeyOperation1 = new DropForeignKeyOperation(anonymousArguments)
            {
                DependentTable = dependentTable,
                PrincipalTable = principalTable
            };

            dependentColumns.Each(delegate(string c) { dropForeignKeyOperation1.DependentColumns.Add(c); });

            AddOperation(dropForeignKeyOperation1);
        }

        protected internal void DropTable(string name, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            name = string.Format("{0}.{1}", schema, name);
            AddOperation(new DropTableOperation(name, anonymousArguments));
        }

        protected internal void DropProcedure(string name, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            name = string.Format("{0}.{1}", schema, name);
            AddOperation(new DropProcedureOperation(name, anonymousArguments));
        }

        protected internal void MoveTable(string name, string newSchema, string baseSchema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            name = string.Format("{0}.{1}", baseSchema, name);
            AddOperation(new MoveTableOperation(name, newSchema, anonymousArguments));
        }

        /// <summary>
        ///     Не надо это спользовать!!
        ///     RenameTable: удваивает название схемы в названии таблицы - жесть
        ///     Пример: dbo.table1 -> dbo.dbo.table2
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newName"></param>
        /// <param name="schema"></param>
        /// <param name="anonymousArguments"></param>
        protected internal void RenameTable(string name, string newName, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(newName),
                null,
                "!string.IsNullOrWhiteSpace(newName)");
            name = string.Format("{0}.{1}", schema, name);
            newName = string.Format("{0}.{1}", schema, newName);
            AddOperation(new RenameTableOperation(name, newName, anonymousArguments));
        }

        protected internal void RenameColumn(string table, string name, string newName, string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(newName),
                null,
                "!string.IsNullOrWhiteSpace(newName)");
            table = string.Format("{0}.{1}", schema, table);
            AddOperation(new RenameColumnOperation(table, name, newName, anonymousArguments));
        }

        protected internal void AddColumn(string table,
            string name,
            Func<ColumnBuilder, ColumnModel> columnAction,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            RuntimeFailureMethods.Requires(columnAction != null, null, "columnAction != null");
            var columnModel = columnAction(new ColumnBuilder());
            columnModel.Name = name;
            table = string.Format("{0}.{1}", schema, table);
            AddOperation(new AddColumnOperation(table, columnModel, anonymousArguments));
        }

        protected internal void DropColumn(string table, string name, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            table = string.Format("{0}.{1}", schema, table);
            AddOperation(new DropColumnOperation(table, name, anonymousArguments));
        }

        protected internal void AlterColumn(string table,
            string name,
            Func<ColumnBuilder, ColumnModel> columnAction,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            RuntimeFailureMethods.Requires(columnAction != null, null, "columnAction != null");
            var columnModel = columnAction(new ColumnBuilder());
            columnModel.Name = name;
            var isDestructiveChange = false;
            table = string.Format("{0}.{1}", schema, table);
            AddOperation(new AlterColumnOperation(table, columnModel, isDestructiveChange, anonymousArguments));
        }

        protected internal void AddPrimaryKey(string table,
            string column,
            string name = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(column),
                null,
                "!string.IsNullOrWhiteSpace(column)");
            AddPrimaryKey(table,
                new[]
                {
                    column
                },
                name,
                schema,
                anonymousArguments);
        }

        protected internal void AddPrimaryKey(string table,
            string[] columns,
            string name = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(columns != null, null, "columns != null");
            RuntimeFailureMethods.Requires(columns.Any(), null, "columns.Any()");
            table = string.Format("{0}.{1}", schema, table);
            var addPrimaryKeyOperation = new AddPrimaryKeyOperation(anonymousArguments)
            {
                Table = table,
                Name = name
            };
            columns.Each(c => addPrimaryKeyOperation.Columns.Add(c)
                );

            AddOperation(addPrimaryKeyOperation);
        }

        protected internal void DropPrimaryKey(string table, string name, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            table = string.Format("{0}.{1}", schema, table);
            var migrationOperation = new DropPrimaryKeyOperation(anonymousArguments)
            {
                Table = table,
                Name = name
            };
            AddOperation(migrationOperation);
        }

        protected internal void DropPrimaryKey(string table, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            table = string.Format("{0}.{1}", schema, table);
            var migrationOperation = new DropPrimaryKeyOperation(anonymousArguments)
            {
                Table = table
            };
            AddOperation(migrationOperation);
        }

        protected internal void CreateIndex(string table,
            string column,
            bool unique = false,
            string name = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(column),
                null,
                "!string.IsNullOrWhiteSpace(column)");
            CreateIndex(table,
                new[]
                {
                    column
                },
                unique,
                name,
                schema,
                anonymousArguments);
        }

        protected internal void CreateIndex(string table,
            string[] columns,
            bool unique = false,
            string name = null,
            string schema = "dbo",
            object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(columns != null, null, "columns != null");
            RuntimeFailureMethods.Requires(columns.Any(), null, "columns.Any()");
            table = string.Format("{0}.{1}", schema, table);
            var createIndexOperation = new CreateIndexOperation(anonymousArguments)
            {
                Table = table,
                IsUnique = unique,
                Name = name
            };
            columns.Each(c => createIndexOperation.Columns.Add(c)
                );
            AddOperation(createIndexOperation);
        }

        protected internal void DropIndex(string table, string name, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(name), null, "!string.IsNullOrWhiteSpace(name)");
            table = string.Format("{0}.{1}", schema, table);
            var migrationOperation = new DropIndexOperation(anonymousArguments)
            {
                Table = table,
                Name = name
            };
            AddOperation(migrationOperation);
        }

        protected internal void DropIndex(string table, string[] columns, string schema = "dbo", object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(table), null, "!string.IsNullOrWhiteSpace(table)");
            RuntimeFailureMethods.Requires(columns != null, null, "columns != null");
            table = string.Format("{0}.{1}", schema, table);
            var dropIndexOperation = new DropIndexOperation(anonymousArguments)
            {
                Table = table
            };
            columns.Each(c => dropIndexOperation.Columns.Add(c)
                );
            AddOperation(dropIndexOperation);
        }

        protected internal void Sql(string sql, bool suppressTransaction = false, object anonymousArguments = null)
        {
            RuntimeFailureMethods.Requires(!string.IsNullOrWhiteSpace(sql), null, "!string.IsNullOrWhiteSpace(sql)");
            AddOperation(new SqlOperation(sql, anonymousArguments)
            {
                SuppressTransaction = suppressTransaction
            });
        }

        /// <summary>
        ///     Добавление таблицы для подсчета изменений
        /// </summary>
        protected internal void AddChangeSetTable(string table, string versionTable, string[] columns)
        {
            RuntimeFailureMethods.Requires(columns != null, null, "columns != null");

            if (_changeSets4Add.ContainsKey(table))
                _changeSets4Add[table] = new ChangeSetModel {Keys = columns, Table = table, VersionTable = versionTable};
            else
                _changeSets4Add.Add(table, new ChangeSetModel {Keys = columns, Table = table, VersionTable = versionTable});
        }

        protected internal IDictionary<string, ChangeSetModel> MergeChangeSets(IDictionary<string, ChangeSetModel> tablesForView)
        {
            foreach (var tbl in _changeSets4Remove)
                tablesForView.Remove(tbl);

            foreach (var changeSetModel in _changeSets4Add)
            {
                if (tablesForView.ContainsKey(changeSetModel.Key))
                    tablesForView[changeSetModel.Key] = changeSetModel.Value;
                else
                    tablesForView.Add(changeSetModel);
            }

            return tablesForView;
        }

        /// <summary>
        ///     Удаление таблицы для подсчета
        /// </summary>
        protected internal void RemoveChangeSetTable(string table)
        {
            if (_changeSets4Add.ContainsKey(table))
                _changeSets4Add.Remove(table);
            else
                _changeSets4Remove.Add(table);
        }

        public void ClearChangeSets()
        {
            _changeSets4Add.Clear();
            _changeSets4Remove.Clear();
        }

        internal void AddOperation(MigrationOperation migrationOperation)
        {
            _operations.Add(migrationOperation);
        }

        internal void AddDataOperation(DataSeedOperation dataOperation)
        {
            _dataOperations.Add(dataOperation);
        }

        internal void Reset()
        {
            _operations.Clear();
			_dataOperations.Clear();
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