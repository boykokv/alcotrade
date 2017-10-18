using System;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using BB.Core;
using BB.Core.Log;
using BB.Extentions;
using CommandLine;

namespace BB.Common.Migrations.Utils
{
    public class DbManager<TContext, TMigrationConfig>
        where TContext : DbContext, new()
        where TMigrationConfig : DbMigrationsConfiguration<TContext>, new()
    {
        static DbManager()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
        }

        public static int Run(string[] args)
        {
            var exitCode = 0;

            var options = new DbManagerOptions();

            Failover.Execute(() =>
            {
                Parser.Default.ParseArguments(args, options);

                if (!CheckInitOptions(options))
                {
                    Logger.Info(options.GetUsage());
                    return;
                }

                if (options.ShowAll)
                {
                    ShowAll(options);
                    return;
                }


                if (options.Backup)
                {
                    BackupDatabase(options);
                    return;
                }

                if (options.RestoreBackup)
                {
                    RestoreBackupDatabase(options);
                    return;
                }

                if (options.Upgrade)
                {
                    UpdateDatabase(options);
                    return;
                }
                if (options.Downgrade)
                {
                    DowngradeDatabase(options);
                    return;
                }
                if (options.Check)
                {
                    Logger.Info(Check(options));
                    return;
                }
                if (options.Recreate)
                {
                    DropDataBase();
                    UpdateDatabase(options);
                }
                if (options.DropDatabase)
                    DropDataBase();
            }, exception => { exitCode = -1; });

            if (options.Wait)
                Console.ReadKey();

            return exitCode;
        }

        private static void ShowAll(DbManagerOptions options)
        {
            var migrator = new DbMigrator(new TMigrationConfig());

            Logger.Info("Возможный следующие миграции:");
            migrator.GetLocalMigrations().ForEach(m => Logger.Info(m));

            Logger.Info("Миграции примененные к базе данных:");
            migrator.GetDatabaseMigrations().ForEach(m => Logger.Info(m));
        }

        /// <summary>
        ///     Проверяет параметры инициализации
        /// </summary>
        /// <param name="options">Параметры командной строки</param>
        private static bool CheckInitOptions(DbManagerOptions options)
        {
            if (!options.IsOneActionSelected)
            {
                Logger.Info("Не выбрано не одной или выбрано более чем одна операция.");
                return false;
            }

            if (options.Backup || options.RestoreBackup)
            {
                if (String.IsNullOrEmpty(options.BackupDirPath))
                {
                    Logger.Info("Не указан путь к директории где должен храниться бэкап данных БД.");
                    return false;
                }
            }

            Logger.Info(options.GetLogMessage());

            if (ConfigurationManager.ConnectionStrings.Count == 0)
            {
                Logger.Info("Не обнаружено строк подключения");
                return false;
            }

            Logger.Info("Cтроки подключения:");

            foreach (var connectionString in ConfigurationManager.ConnectionStrings)
                Logger.Info(connectionString.ToString());

            return true;
        }

        /// <summary>
        ///     Откат базы данных
        /// </summary>
        /// <param name="options">Опции командной строки</param>
        private static void DowngradeDatabase(DbManagerOptions options)
        {
            Logger.Info("Откат БД : ");

            var migrator = new DbMigrator(new TMigrationConfig());

            var migrationIdProvider = new MigrationIdProvider();

            var targetMigrationId = migrationIdProvider.GetMigrationId(migrator, options.Version, options.Migration, true);

            migrator.Downgrade(targetMigrationId);

            Logger.Info(
                string.IsNullOrEmpty(targetMigrationId)
                    ? "Успешно проведен откат к чистой БД"
                    : "Успешно проведен откат к миграции {0}",
                targetMigrationId);
        }

        /// <summary>
        ///     Установка миграций
        /// </summary>
        /// <param name="options">Опции командной строки</param>
        private static void UpdateDatabase(DbManagerOptions options)
        {
            Logger.Info("Обновление БД : ");

            var migrator = new DbMigrator(new TMigrationConfig());
            migrator.SeedEnabled = options.SeedEnabled;
            migrator.SeedForTestEnabled = options.SeedForTestEnabled;

            var migrationIdProvider = new MigrationIdProvider();

            var targetMigrationId = migrationIdProvider.GetMigrationId(migrator, options.Version, options.Migration, false);

            migrator.Update(targetMigrationId);

            Logger.Info("Успешно проведено обновление до миграции {0}", targetMigrationId);
        }

        /// <summary>
        ///     Удаление базы данных
        /// </summary>
        private static void DropDataBase()
        {
            Logger.Info("Удаление БД : ");

            using (var context = new TContext())
            {
                if (context.Database.Exists())
                {
                    // Только так грубо удалось удалять базу с обрывом подключений.
                    var connection = context.Database.Connection;
                    var databaseName = connection.Database.Replace("[", "[[").Replace("]", "]]");
                    var commandText = string.Format(@"use master
                                                      ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE 
                                                      DROP DATABASE [{0}]", databaseName);
                    var command = connection.CreateCommand();
                    command.CommandText = commandText;
                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        connection.Close();
                    }
                    Logger.Info("База данных успешно удалена");
                }
                else
                    Logger.Info("База данных не обнаружена");
            }
        }

        /// <summary>
        /// </summary>
        private static void BackupDatabase(DbManagerOptions options)
        {
            Logger.Info("Создание бэкапа данных БД: ");

            if (!Directory.Exists(options.BackupDirPath))
                Directory.CreateDirectory(options.BackupDirPath);

            using (var context = new TContext())
            {
                if (context.Database.Exists())
                {
                    var connection = context.Database.Connection;
                    var databaseName = connection.Database.Replace("[", "[[").Replace("]", "]]");

                    var backupPathWithFileName = Path.Combine(options.BackupDirPath, String.Format("{0}.bak", databaseName));
                    var commandText = string.Format(@" use master 
                                                        BACKUP DATABASE [{0}]
                                                        TO DISK = '{1}'
                                                           WITH FORMAT,
                                                              MEDIANAME = '{2}',
                                                              NAME = '{2}';", databaseName, backupPathWithFileName, databaseName);
                    var command = connection.CreateCommand();

                    if (options.CommandTimeoutInSeconds.HasValue)
                    {
                        command.CommandTimeout = options.CommandTimeoutInSeconds.Value;
                    }
                    
                    command.CommandText = commandText;
                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                    }
                    finally
                    {
                        connection.Close();
                    }
                    Logger.Info("Бэкап данных БД создан успешно");
                }
                else
                    Logger.Info("База данных не обнаружена");
            }
        }

        private static void RestoreBackupDatabase(DbManagerOptions options)
        {
            Logger.Info("Восстановление данных БД из бэкапа: ");

            if (!Directory.Exists(options.BackupDirPath))
            {
                Logger.Error(String.Format("Указанная директория {0} не существует", options.BackupDirPath));
                return;
            }

            using (var context = new TContext())
            {
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(context.Database.Connection.ConnectionString)
                {
                    InitialCatalog = "master"
                };

                var connectionString = sqlConnectionStringBuilder.ToString();

                var connection = new SqlConnection(connectionString);

                var databaseName = context.Database.Connection.Database.Replace("[", "[[").Replace("]", "]]");
                var backupPathWithFileName = Path.Combine(options.BackupDirPath, String.Format("{0}.bak", databaseName));

                Logger.Info("Путь к бэкапу {0}", backupPathWithFileName);

                var commandBuilder = new StringBuilder(@" use master ");

                commandBuilder.AppendFormat(@"RESTORE DATABASE [{0}]
                                                           FROM DISK = '{1}' 
                                                        WITH REPLACE;

                                                        ALTER DATABASE [{0}] SET MULTI_USER ", databaseName, backupPathWithFileName);

                var command = connection.CreateCommand();

                if (options.CommandTimeoutInSeconds.HasValue)
                {
                    command.CommandTimeout = options.CommandTimeoutInSeconds.Value;
                }

                command.CommandText = commandBuilder.ToString();
                try
                {
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                finally
                {
                    connection.Close();
                }
                Logger.Info("Данные БД успешно восстановлены из бэкапа");
            }
        }

        /// <summary>
        ///     Проверяет установленные миграции исходя из параметров командной строки
        /// </summary>
        /// <param name="options">Опции командной строки</param>
        /// <returns>Результат проверки миграций</returns>
        private static string Check(DbManagerOptions options)
        {
            Logger.Info("Проверка установленных миграций : ");

            var migrationChecker = new MigrationChecker<TContext>(new TMigrationConfig());

            if (!string.IsNullOrEmpty(options.Migration))
                return migrationChecker.CheckByMigrationName(options.Migration);

            if (!string.IsNullOrEmpty(options.Version))
                return migrationChecker.CheckByVersion(options.Version);

            return migrationChecker.CheckLastMigration();
        }

        /// <summary>
        /// Логирование ошибок.
        /// </summary>
        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs exception)
        {
            var exceptionObject = exception.ExceptionObject as Exception;
            Logger.Log(exceptionObject);
        }
    }
}