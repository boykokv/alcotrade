using System;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using BB.Common.Migrations.Extensions;

namespace BB.Common.Migrations
{
    /// <summary>
    /// Предоставляет Id миграции
    /// </summary>
    public class MigrationIdProvider
    {
        /// <summary>
        /// Возвращает название миграции
        /// </summary>
        /// <param name="migrator">Мигратор</param>
        /// <param name="version">Номер версии</param>
        /// <param name="migration">Название миграции</param>
        /// <param name="downgrade">Является ли операция миграции</param>
        /// <returns>Id миграции</returns>
        public string GetMigrationId(DbMigrator migrator, string version, string migration, bool downgrade)
        {
            if (migrator == null)
                throw new ArgumentNullException("migrator");

            if (!string.IsNullOrEmpty(migration))
                return GetMigrationIdByMigrationName(migrator, migration);

            if (!string.IsNullOrEmpty(version))
                return GetMigrationIdByMigrationVersion(migrator, version);

            return GetLastMigrationId(migrator, downgrade);
        }

        /// <summary>
        ///     Возвращает Id для последней миграции
        /// </summary>
        /// <param name="migrator">Мигратор</param>
        /// <param name="downgrade">Является ли операция миграции</param>
        /// <returns>Id миграции</returns>
        private string GetLastMigrationId(DbMigrator migrator, bool downgrade)
        {
            var targetMigration = migrator.GetLocalMigrations().ToList();
            if (targetMigration.Count == 0)
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, null));

            return downgrade ? null : targetMigration.Last();
        }

        /// <summary>
        ///     Возвращает Id миграции по версии миграции
        /// </summary>
        /// <param name="migrator">Мигратор</param>
        /// <param name="version">Номер версии</param>
        /// <returns>Id миграции</returns>
        private string GetMigrationIdByMigrationVersion(DbMigrator migrator, string version)
        {
            var targetMigration =
                migrator.GetLocalMigrations()
                    .Where(s => s.ToLowerInvariant().StartsWith(version.ToLowerInvariant()))
                    .ToList();

            if (targetMigration.Count == 0)
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, version));

            return targetMigration.Last();
        }

        /// <summary>
        ///     Возвращает Id миграции по имени
        /// </summary>
        /// <param name="migrator">Мигратор</param>
        /// <param name="migration">Название миграции</param>
        /// <returns>Id миграции</returns>
        private string GetMigrationIdByMigrationName(DbMigrator migrator, string migration)
        {
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
                throw new MigrationsException(string.Format(Constants.MigrationNotFound, migration));

            return targetMigration.First();
        }
    }
}