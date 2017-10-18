using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using BB.Common.Migrations.Extensions;

namespace BB.Common.Migrations
{
    /// <summary>
    /// Проверяет установленные миграции в БД
    /// </summary>
    public class MigrationChecker<T> where T : DbContext
    {
        private readonly DbMigrationsConfiguration<T> _configuration;

        public MigrationChecker(DbMigrationsConfiguration<T> configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Проверяет установлена ли последняя миграция
        /// </summary>
        /// <returns>Результат проверки миграций</returns>
        public string CheckLastMigration()
        {
            var migrator = new DbMigrator(_configuration);

            var targetMigration = migrator.GetLocalMigrations().ToList();

            var targetMigrationDb = migrator.GetDatabaseMigrations(null, false).ToList();

            return migrator.CheckMigrations(targetMigration.LastOrDefault(), targetMigrationDb.LastOrDefault(), false);
        }


        /// <summary>
        /// Проверяет наличие миграции в БД по ее версии
        /// </summary>
        /// <param name="version">Версия миграции</param>
        /// <returns>Результат проверки миграций</returns>
        public string CheckByVersion(string version)
        {
            var migrator = new DbMigrator(_configuration);

            var targetMigration =
                 migrator.GetLocalMigrations()
                     .Where(s => s.ToLowerInvariant().StartsWith(version.ToLowerInvariant()))
                     .ToList();

            if (targetMigration.Count == 0)
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, version));

            var targetMigrationDb = migrator.GetDatabaseMigrations(version, true).ToList();

            if (targetMigrationDb.Count == 0)
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, version));

            return migrator.CheckMigrations(targetMigration.Last(), targetMigrationDb.Last(), true);
        }


        /// <summary>
        /// Ищет конкретную миграцию в БД по имени миграции
        /// </summary>
        /// <param name="migration">Название миграции</param>
        /// <returns>Результат проверки миграций</returns>
        public string CheckByMigrationName(string migration)
        {
            var migrator = new DbMigrator(_configuration);

            var targetMigration =
                 migrator.GetLocalMigrations()
                     .Where(s => s.ToLowerInvariant().Contains(migration.ToLowerInvariant()))
                     .ToList();

            if (targetMigration.Count > 1)
            {
                throw new MigrationsException(string.Format(Constants.NotUniqMigration,
                    Environment.NewLine,
                    targetMigration.Join(separator: Environment.NewLine)));
            }

            if (targetMigration.Count == 0)
            {
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, migration));
            }

            var targetMigrationDb = migrator.GetDatabaseMigrations(migration, false).ToList();

            if (targetMigrationDb.Count > 1)
            {
                throw new MigrationsException(string.Format(Constants.NotUniqMigration,
                    Environment.NewLine,
                    targetMigrationDb.Join(separator: Environment.NewLine)));
            }

            if (targetMigrationDb.Count == 0)
            {
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, migration));
            }

            return migrator.CheckMigrations(targetMigration.First(), targetMigrationDb.First(), false);
        }
    }
}
