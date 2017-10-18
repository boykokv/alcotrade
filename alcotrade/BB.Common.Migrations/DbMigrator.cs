using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Infrastructure.DependencyResolution;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Sql;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using BB.Core;
using BB.Core.Log;
using BB.Common.Migrations.Extensions;
using BB.Common.Migrations.Utils;

namespace BB.Common.Migrations
{
    /// <summary>
    ///     DbMigrator is used to apply existing migrations to a database.
    ///     DbMigrator can be used to upgrade and downgrade to any given migration.
    /// </summary>
    public class DbMigrator : MigratorBase
    {
        private const string changeSetTemplate =
            @"SELECT {1}.VersionId, '{0}' as TableName, '{1}' as VersionTableName, {0}C.[Count] as [ChangesCount], {0}D.[Count] as [DeleteCount], {0}A.[Count] as [AddCount]
							FROM {1}
							left join (SELECT t1.DeleteInVersionId as Version, COUNT(*) AS [Count]
							FROM {0} as t1 join {0} as t2 on {4} and t1.VersionId != t2.VersionId and t1.DeleteInVersionId is not null
							GROUP BY t1.DeleteInVersionId ) AS {0}C on {0}C.Version = {1}.VersionId
							left join (SELECT t1.DeleteInVersionId as Version, COUNT(*) AS [Count]
							FROM {0} as t1 left join (SELECT {2}, t1.VersionId FROM {0} as t1 join {0} as t2 on {4} and t1.VersionId != t2.VersionId and t1.DeleteInVersionId is not null) as tc on {3}
							where t1.DeleteInVersionId is not null and tc.VersionId is NULL
							GROUP BY t1.DeleteInVersionId) AS {0}D on {0}D.Version = {1}.VersionId
							left join (SELECT t1.VersionId as Version, COUNT(*) AS [Count] FROM {0} as t1
							left join (SELECT {2}, t1.VersionId FROM {0} as t1 join {0} as t2 on {4} and t1.VersionId != t2.VersionId and t1.DeleteInVersionId is not null) as tc on {3}
							where tc.VersionId is null
							GROUP BY t1.VersionId) AS {0}A on {0}A.Version = {1}.VersionId";

        private readonly bool _calledByCreateDatabase;
        private readonly DbMigrationsConfiguration _configuration;
        private readonly string _defaultSchema;

        private readonly Func<DbConnection, string, System.Data.Entity.Migrations.History.HistoryContext>
            _historyContextFactory;

        private readonly MigrationAssembly _migrationAssembly;
        private readonly DbProviderFactory _providerFactory;

        private readonly string _providerManifestToken;
        private readonly DbContextInfo _usersContextInfo;
        private MigrationSqlGenerator _sqlGenerator;

        /// <summary>
        ///     Initializes a new instance of the DbMigrator class.
        /// </summary>
        /// <param name="configuration">Configuration to be used for the migration process. </param>
        public DbMigrator(DbMigrationsConfiguration configuration)
            : this(configuration, null)
        {
            Check.NotNull(configuration, "configuration");
            Check.NotNull(configuration.ContextType, "configuration.ContextType");
            _historyContextFactory = configuration.GetHistoryContextFactory(_usersContextInfo.ConnectionProviderName);
        }

        internal DbMigrator(DbMigrationsConfiguration configuration, DbContext usersContext)
            : base(null)
        {
            Check.NotNull(configuration, "configuration");
            Check.NotNull(configuration.ContextType, "configuration.ContextType");
            _configuration = configuration;
            _calledByCreateDatabase = usersContext != null;
            if (_calledByCreateDatabase)
                _usersContextInfo = new DbContextInfo(usersContext.GetType());
            else
            {
                _usersContextInfo = configuration.TargetDatabase == null
                    ? new DbContextInfo(configuration.ContextType)
                    : new DbContextInfo(configuration.ContextType, configuration.TargetDatabase);
            }

            var context = usersContext ?? _usersContextInfo.CreateInstance();

            context.Database.CommandTimeout = 90;

            try
            {
                _migrationAssembly = new MigrationAssembly(_configuration.MigrationsAssembly);
                var connection = context.Database.Connection;
                _providerFactory = DbProviderServices.GetProviderFactory(connection);
                _providerManifestToken = DbConfiguration.DependencyResolver.GetService<IManifestTokenResolver>()
                    .ResolveManifestToken(connection);
                _defaultSchema = "dbo";
            }
            finally
            {
                if (usersContext == null)
                    context.Dispose();
            }
        }

        public bool SeedEnabled { get; set; }
        public bool SeedForTestEnabled { get; set; }

        /// <summary>
        ///     Gets the configuration that is being used for the migration process.
        /// </summary>
        public override DbMigrationsConfiguration Configuration
        {
            get { return _configuration; }
        }

        private MigrationSqlGenerator SqlGenerator
        {
            get
            {
                return _sqlGenerator ??
                       (_sqlGenerator =
                           _configuration.GetSqlGenerator(_usersContextInfo.ConnectionProviderName));
            }
        }

        /// <summary>
        ///     Gets all migrations that are defined in the configured migrations assembly.
        /// </summary>
        public override IEnumerable<string> GetLocalMigrations()
        {
            return _migrationAssembly.MigrationIds;
        }

        /// <summary>
        ///     Gets all migrations that have been applied to the target database.
        /// </summary>
        public override IEnumerable<string> GetDatabaseMigrations()
        {
            using (var historyContext = (HistoryContext) _historyContextFactory(CreateConnection(), _defaultSchema))
            {
                return
                    historyContext.Migration.Select(s => s.MigrationId)
                        .ToList()
                        .OrderBy(x => x.ToLowerInvariant(), new MigrationComparer())
                        .ToList();
            }
        }

        public IEnumerable<string> GetDatabaseMigrations(string migrationFilter, bool isVersion)
        {
            if (string.IsNullOrEmpty(migrationFilter))
                return GetDatabaseMigrations();

            using (var historyContext = (HistoryContext) _historyContextFactory(CreateConnection(), _defaultSchema))
            {
                var dbMigrations = historyContext.Migration.Where(
                    s =>
                        isVersion
                            ? s.MigrationId.ToLower().StartsWith(migrationFilter.ToLower())
                            : s.MigrationId.ToLower().Contains(migrationFilter.ToLower()))
                    .Select(s => s.MigrationId)
                    .ToList();

                return dbMigrations.OrderBy(x => x.ToLowerInvariant(), new MigrationComparer()).ToList();
            }
        }

        public string CheckMigrations(string migrationId, string dbMigrationId, bool isVersion)
        {
            var comparer = new MigrationComparer();
            var builder = new StringBuilder();
            using (var historyContext = (HistoryContext) _historyContextFactory(CreateConnection(), _defaultSchema))
            {
                var id = migrationId;
                var dbId = dbMigrationId;
                var migrations = _migrationAssembly.MigrationIds.Where(s => comparer.Compare(s, id) <= 0).ToList();
                var dbMigrations =
                    historyContext.Migration.ToList()
                        .Where(s => comparer.Compare(s.MigrationId, dbId) <= 0)
                        .ToList();

                var notSavedMigrations = migrations.Except(dbMigrations.Select(s => s.MigrationId)).ToList();
                var moreSavedMigrations = dbMigrations.Select(s => s.MigrationId).Except(migrations).ToList();
                if (notSavedMigrations.Any())
                {
                    builder.AppendLine("Обнаружены миграции, отсутствующие в базе данных:");
                    notSavedMigrations.Each(s => builder.AppendLine(s));
                }
                if (moreSavedMigrations.Any())
                {
                    builder.AppendLine("Обнаружены 'лишние' миграции в базе данных:");
                    moreSavedMigrations.Each(s => builder.AppendLine(s));
                }

                if (!notSavedMigrations.Any() && !moreSavedMigrations.Any())
                {
                    var hashDiff = new List<string>();
                    foreach (var row in dbMigrations)
                    {
                        var hash = GetMigrationHash(row.MigrationId);
                        if (hash != row.HashCode)
                            hashDiff.Add(row.MigrationId);
                    }

                    if (hashDiff.Any())
                    {
                        builder.AppendLine("Обнаружены расхождения в хеш-кодах миграций:");
                        hashDiff.Each(s => builder.AppendLine(s));
                    }
                    else
                        builder.AppendLine("Расхождений в миграциях не обнаружено");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        ///     Gets all migrations that are defined in the assembly but haven't been applied to the target database.
        /// </summary>
        public override IEnumerable<string> GetPendingMigrations()
        {
            using (var connection = CreateConnection())
            {
                connection.ConnectionString = _usersContextInfo.ConnectionString;
                using (var historyContext = (HistoryContext) _historyContextFactory(connection, _defaultSchema))
                {
                    var databaseMigrations = historyContext.Migration.Select(s => s.MigrationId);
                    return _migrationAssembly.MigrationIds
                        .Except(databaseMigrations)
                        .ToList()
                        .OrderBy(x => x.ToLowerInvariant(), new MigrationComparer())
                        .ToList();
                }
            }
        }

        public IEnumerable<string> GetMigrationsSince(string migrationId)
        {
            using (var connection = CreateConnection())
            {
                using (var historyContext = (HistoryContext) _historyContextFactory(connection, _defaultSchema))
                {
                    var comparer = new MigrationComparer();
                    var migrations = historyContext.Migration.Select(s => s.MigrationId).ToList();
                    if (!string.IsNullOrEmpty(migrationId))
                    {
                        migrations =
                            migrations.Where(
                                m => comparer.Compare(m.ToLowerInvariant(), migrationId.ToLowerInvariant()) > 0)
                                .ToList();
                    }
                    return migrations;
                }
            }
        }

        /// <summary>
        ///     Updates the target database to a given migration.
        /// </summary>
        /// <param name="targetMigration">The migration to upgrade/downgrade to. </param>
        public override void Update(string targetMigration)
        {
            //System.Data.Entity.Database.SetInitializer(new BlogContextCustomInitializer()); 
            EnsureDatabaseExists(() => UpdateInternal(targetMigration));
        }

        private void UpdateInternal(string targetMigration)
        {
            Upgrade(GetPendingMigrations(), targetMigration, GetLastInstalledMigrationId());
        }

        private string GetLastInstalledMigrationId()
        {
            using (var connection = CreateConnection())
            {
                connection.ConnectionString = _usersContextInfo.ConnectionString;
                using (var historyContext = (HistoryContext)_historyContextFactory(connection, _defaultSchema))
                {
                    return historyContext.Migration
                        .Select(s => s.MigrationId)
                        .ToList()
                        .OrderBy(x => x.ToLowerInvariant(), new MigrationComparer())
                        .LastOrDefault();
                }
            }
        }

        public void Downgrade(string targetMigrationId)
        {
            var enumerable = GetMigrationsSince(targetMigrationId).ToList();
            if (enumerable.Any())
                Downgrade(enumerable.OrderByDescending(s => s.ToLowerInvariant(), new MigrationComparer()));
        }

        internal void Upgrade(IEnumerable<string> pendingMigrations, string targetMigrationId, string lastMigrationId)
        {
            var lastMigration = (DbMigration) null;
            var tablesListForVersionChangesSetView = GetFullChangeSetCollection(lastMigrationId);

            foreach (var str in pendingMigrations)
            {
                Logger.Info("Установка миграции {0}", str);
                var migration = _migrationAssembly.GetMigration(str);
                ApplyMigration(migration, lastMigration, !migration.GetType().IsDefined(typeof (DontAffectHistoryAttribute), true));

                migration.MergeChangeSets(tablesListForVersionChangesSetView);

                lastMigration = migration;
                if (str.EqualsIgnoreCase(targetMigrationId))
                    break;
            }

            string values = string.Join(", ", tablesListForVersionChangesSetView.Keys);
            Logger.Info("Создание представления изменений в версии для таблиц {0}", values);

            BuildVersionChangesSetView(tablesListForVersionChangesSetView);
        }

        private void Downgrade(IEnumerable<string> pendingMigrations)
        {
            var tablesListForVersionChangesSetView = GetFullChangeSetCollection();

            for (var index = 0; index < pendingMigrations.Count(); ++index)
            {
                var migrationId = pendingMigrations.ElementAt(index);
                var migration = _migrationAssembly.GetMigration(migrationId);

                Logger.Info("Откат миграции {0}", migrationId);

                RevertMigration(migrationId, migration, GetTargetModel());

                migration.MergeChangeSets(tablesListForVersionChangesSetView);
            }

            string values = string.Join(", ", tablesListForVersionChangesSetView.Keys);
            Logger.Info("Создание представления изменений в версии для таблиц {0}", values);
            BuildVersionChangesSetView(tablesListForVersionChangesSetView);
        }

        private XDocument GetTargetModel()
        {
            XDocument targetModel;
            using (var context = _usersContextInfo.CreateInstance())
            {
                targetModel = context.GetModel();
            }
            return targetModel;
        }

        private void BuildVersionChangesSetView(IDictionary<string, ChangeSetModel> changeSets)
        {
            var deleteOperation = new SqlOperation("IF OBJECT_ID('vw_VersionChangesSet', 'V') IS NOT NULL DROP VIEW vw_VersionChangesSet;");

            var so = new SqlOperation((string.Format("CREATE VIEW [dbo].[vw_VersionChangesSet] AS {0}",
                string.Join(" union ",
                    changeSets.Select(
                        t =>
                            string.Format(changeSetTemplate, t.Value.Table, t.Value.VersionTable,
                                string.Join(", ", t.Value.Keys.Select(x => "t1." + x)),
                                string.Join(" and ", t.Value.Keys.Select(x => String.Format("t1.{0} = tc.{0}", x))),
                                string.Join(" and ", t.Value.Keys.Select(x => String.Format("t1.{0} = t2.{0}", x)))))))));

            var emptySo =
                new SqlOperation(
                    "CREATE VIEW [dbo].[vw_VersionChangesSet] AS SELECT NULL as VersionId, NULL as TableName, NULL as VersionTableName, 0 as [ChangesCount], 0 as [DeleteCount], 0 as [AddCount]");

            ExecuteOperations(String.Empty,
                GetTargetModel(),
                changeSets.Any() ? new[] {deleteOperation, so} : new[] {deleteOperation, emptySo},
                new List<MigrationOperation>(),
                false,
                false);
        }

        internal IDictionary<string, ChangeSetModel> GetFullChangeSetCollection(string lastMigrationId = null)
        {
            using (var connection = CreateConnection())
            {
                connection.ConnectionString = _usersContextInfo.ConnectionString;
                using (var historyContext = (HistoryContext) _historyContextFactory(connection, _defaultSchema))
                {
                    var changeSets = new Dictionary<string, ChangeSetModel>();
                    var installedMigrationsIds = historyContext.Migration
                        .Select(s => s.MigrationId)
                        .ToList()
                        .OrderBy(x => x.ToLowerInvariant(), new MigrationComparer())
                        .ToList();

                    foreach (var migrationId in installedMigrationsIds)
                    {
                        var migration = _migrationAssembly.GetMigration(migrationId);

                        if (migration != null)
                        {
                            migration.Up();

                            migration.MergeChangeSets(changeSets);
                            migration.ClearChangeSets();
                        }
                        else
                            Logger.Error(string.Format("Миграция указанная в таблице истории, почему то не найдена в коде: {0}", migrationId));
                    }

                    return changeSets;
                }
            }
        }

        internal void RevertMigration(string migrationId, DbMigration migration, XDocument targetModel)
        {
            var systemOperations = Enumerable.Empty<MigrationOperation>();
            migration.Down();
            ExecuteOperations(migrationId, targetModel, migration.Operations, systemOperations, true);
        }

        internal void ApplyMigration(DbMigration migration, DbMigration lastMigration, bool effectOnHistory)
        {
            var migrationMetadata = (IMetadataMigration) migration;

            XDocument targetModel;
            using (var context = _usersContextInfo.CreateInstance())
            {
                targetModel = context.GetModel();
            }

            migration.Up();
            ExecuteOperations(migrationMetadata.Id,
                targetModel,
                migration.Operations,
                new List<MigrationOperation>(),
                false,
                effectOnHistory);

            if (SeedEnabled)
                migration.Seed();
            if (SeedForTestEnabled)
                migration.SeedForTest();
            if (migration.DataOperations.Count() > 0)
                ExecuteDataOperations(migration.DataOperations);
        }

        private void ExecuteOperations(string migrationId,
            XDocument targetModel,
            IEnumerable<MigrationOperation> operations,
            IEnumerable<MigrationOperation> systemOperations,
            bool downgrading,
            bool effectOnHistory = true)
        {
            var migrationOperations = operations as IList<MigrationOperation> ?? operations.ToList();
            FillInForeignKeyOperations(migrationOperations, targetModel);
            var list1 =
                migrationOperations.OfType<CreateTableOperation>()
                    .SelectMany(ct => migrationOperations.OfType<AddForeignKeyOperation>(),
                        (ct, afk) => new
                        {
                            ct,
                            afk
                        })
                    .Where(param0 => param0.ct.Name.EqualsIgnoreCase(param0.afk.DependentTable))
                    .Select(param0 => param0.afk)
                    .ToList();
            var list2 = migrationOperations.Except(list1).Concat(list1).Concat(systemOperations).ToList();

            var enumerable = GenerateStatements(list2, migrationId);

            ExecuteStatements(enumerable);

            if (!effectOnHistory)
                return;

            using (var connection = _providerFactory.CreateConnection())
            {
                connection.ConnectionString = _usersContextInfo.ConnectionString;
                using (var historyContext = (HistoryContext) _historyContextFactory(connection, _defaultSchema))
                {
                    if (!downgrading)
                    {
                        var row = new MigrationHistoryRow
                        {
                            ContextKey = _configuration.ContextKey,
                            HashCode = GetMigrationHash(migrationId),
                            MigrationId = migrationId,
                            MigrationTime = DateTime.Now
                        };
                        historyContext.Migration.Add(row);
                    }
                    else
                    {
                        var row = new MigrationHistoryRow
                        {
                            ContextKey = _configuration.ContextKey,
                            MigrationId = migrationId
                        };
                        historyContext.Migration.Attach(row);
                        historyContext.Migration.Remove(row);
                    }

                    historyContext.SaveChanges();
                }
            }
        }

        private void ExecuteDataOperations(IEnumerable<DataSeedOperation> operations)
        {
            using (var connection = CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        foreach (var op in operations)
                        {
                            var comm = ConfigureCommand(transaction.Connection.CreateCommand(), op.Sql);

                            if (op.Prms != null && op.Prms.Count > 0)
                            {
                                foreach (var p in op.Prms)
                                {
                                    var prm = comm.CreateParameter();
                                    prm.ParameterName = p.Key;
                                    prm.Value = p.Value ?? DBNull.Value;
                                    comm.Parameters.Add(prm);
                                }
                            }
                            comm.Transaction = transaction;
                            comm.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        throw new CommitFailedException("Data seed commit failed", ex);
                    }
                }
            }
        }

        internal IEnumerable<MigrationStatement> GenerateStatements(IList<MigrationOperation> operations,
            string migrationId)
        {
            return SqlGenerator.Generate(operations, _providerManifestToken);
        }

        internal void ExecuteStatements(IEnumerable<MigrationStatement> migrationStatements)
        {
            var connection = CreateConnection();
            try
            {
                DbProviderServices.GetExecutionStrategy(connection)
                    .Execute(() => ExecuteStatementsInternal(migrationStatements, connection));
            }
            finally
            {
                if (connection != null)
                    connection.Dispose();
            }
        }

        private void ExecuteStatementsInternal(IEnumerable<MigrationStatement> migrationStatements,
            DbConnection connection)
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                foreach (var migrationStatement in migrationStatements)
                    ExecuteSql(transaction, migrationStatement);
                try
                {
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    throw new CommitFailedException("Commit failed", ex);
                }
            }
        }

        internal void ExecuteSql(DbTransaction transaction, MigrationStatement migrationStatement)
        {
            if (string.IsNullOrWhiteSpace(migrationStatement.Sql))
                return;
            if (!migrationStatement.SuppressTransaction)
            {
                using (
                    var interceptableDbCommand = ConfigureCommand(transaction.Connection.CreateCommand(),
                        migrationStatement.Sql))
                {
                    interceptableDbCommand.Transaction = transaction;
                    interceptableDbCommand.ExecuteNonQuery();
                }
            }
            else
            {
                using (var connection = CreateConnection())
                {
                    using (
                        var interceptableDbCommand = ConfigureCommand(connection.CreateCommand(),
                            migrationStatement.Sql))
                    {
                        connection.Open();
                        interceptableDbCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private DbCommand ConfigureCommand(DbCommand command, string commandText)
        {
            command.CommandText = commandText;
            if (_configuration.CommandTimeout.HasValue)
                command.CommandTimeout = _configuration.CommandTimeout.Value;
            return command;
        }

        private static void FillInForeignKeyOperations(IEnumerable<MigrationOperation> operations, XDocument targetModel)
        {
            foreach (var foreignKeyOperation1 in operations.OfType<AddForeignKeyOperation>().Where(fk =>
            {
                if (fk.PrincipalTable != null)
                    return !fk.PrincipalColumns.Any();
                return false;
            }))
            {
                var foreignKeyOperation = foreignKeyOperation1;
                var principalTable = GetStandardizedTableName(foreignKeyOperation.PrincipalTable);
                var entitySetName =
                    targetModel.Descendants(EdmXNames.Ssdl.EntitySetNames)
                        .Where(
                            es =>
                                new DatabaseName(es.TableAttribute(), es.SchemaAttribute()).ToString()
                                    .EqualsIgnoreCase(principalTable))
                        .Select(es => es.NameAttribute())
                        .SingleOrDefault();
                if (entitySetName != null)
                {
                    targetModel.Descendants(EdmXNames.Ssdl.EntityTypeNames)
                        .Single(et => et.NameAttribute().EqualsIgnoreCase(entitySetName))
                        .Descendants(EdmXNames.Ssdl.PropertyRefNames)
                        .Each(pr => foreignKeyOperation.PrincipalColumns.Add(pr.NameAttribute()));
                }
                else
                {
                    var createTableOperation =
                        operations.OfType<CreateTableOperation>()
                            .SingleOrDefault(ct => GetStandardizedTableName(ct.Name).EqualsIgnoreCase(principalTable));
                    if (createTableOperation == null || createTableOperation.PrimaryKey == null)
                    {
                        throw new MigrationsException(
                            string.Format(
                                "The Foreign Key on table '{0}' with columns '{1}' could not be created because the principal key columns could not be determined. Use the AddForeignKey fluent API to fully specify the Foreign Key.",
                                foreignKeyOperation.DependentTable,
                                foreignKeyOperation.DependentColumns.Join()));
                    }
                    createTableOperation.PrimaryKey.Columns.Each(c => foreignKeyOperation.PrincipalColumns.Add(c));
                }
            }
        }

        private static string GetStandardizedTableName(string tableName)
        {
            if (!string.IsNullOrWhiteSpace(DatabaseName.Parse(tableName).Schema))
                return tableName;
            return new DatabaseName(tableName, "dbo").ToString();
        }

        /// <summary>
        ///     Ensures that the database exists by creating an empty database if one does not
        ///     already exist. If a new empty database is created but then the code in mustSucceedToKeepDatabase
        ///     throws an exception, then an attempt is made to clean up (delete) the new empty database.
        ///     This avoids leaving an empty database with no or incomplete metadata (e.g. MigrationHistory)
        ///     which can then cause problems for database initializers that check whether or not a database
        ///     exists.
        /// </summary>
        internal void EnsureDatabaseExists(Action mustSucceedToKeepDatabase)
        {
            var flag = false;
            var databaseCreator = new DatabaseCreator(_configuration.CommandTimeout);
            using (var connection = CreateConnection())
            {
                var dbExists = false;
                using (var historyContext = (HistoryContext) _historyContextFactory(connection, _defaultSchema))
                {
                    if (historyContext.Database.Exists())
                        dbExists = true;
                }
                if (!dbExists)
                {
                    var cfg = new HistoryConfiguration
                    {
                        TargetDatabase =
                            new DbConnectionInfo(_usersContextInfo.ConnectionString, "System.Data.SqlClient")
                    };
                    var migrator = new System.Data.Entity.Migrations.DbMigrator(cfg);
                    migrator.Update();
                    flag = true;
                }
            }

            try
            {
                mustSucceedToKeepDatabase();
            }
            catch
            {
                if (flag)
                {
                    try
                    {
                        using (var connection = CreateConnection())
                        {
                            databaseCreator.Delete(connection);
                        }
                    }
                    catch {}
                }
                throw;
            }
        }

        private DbConnection CreateConnection()
        {
            var connection = _providerFactory.CreateConnection();
            connection.ConnectionString = _usersContextInfo.ConnectionString;
            return connection;
        }

        private string GetMigrationHash(string migrationId)
        {
            var comparer = new MigrationComparer();
            var migrationIds =
                _migrationAssembly.MigrationIds.Where(s => comparer.Compare(s, migrationId) <= 0)
                    .OrderBy(s => s, comparer);
            var migrations = migrationIds.Select(s => _migrationAssembly.GetMigration(s)).ToList();
            migrations.Each(s => s.Up());
            var statements = SqlGenerator.Generate(migrations.SelectMany(s => s.Operations),
                _providerManifestToken);
            var builder = new StringBuilder();
            statements.Each(s => builder.Append(s.Sql));
            return builder.ToString().CalculateMD5Hash();
        }
    }
}