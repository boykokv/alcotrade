using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BB.Common.Migrations.Utils;

namespace BB.Common.Migrations
{
    public class MigrationAssembly
    {
        private readonly IList<IMetadataMigration> _migrations;
		public virtual IEnumerable<string> MigrationIds
		{
			get
			{
				return (
					from t in this._migrations
                    select t.Id).ToList<string>().OrderBy(x => x.ToLowerInvariant(), new MigrationComparer()).ToList();
			}
		}
		
        protected MigrationAssembly()
		{
		}
		public MigrationAssembly(Assembly migrationsAssembly)
		{
            _migrations = migrationsAssembly.GetTypes()
		        .Where(
		            t =>
                        t.IsSubclassOf(typeof(DbMigration)) && typeof(IMetadataMigration).IsAssignableFrom(t) &&
		                    t.GetConstructor(Type.EmptyTypes) != null && !t.IsAbstract && !t.IsGenericType)
                .Select(s => (IMetadataMigration)Activator.CreateInstance(s))
		        .Where(t => !string.IsNullOrWhiteSpace(t.Id))
		        .OrderBy(t => t.Id).ToList();
		}
		public virtual string UniquifyName(string migrationName)
		{
			string uniqueName = migrationName;
			int num = 1;
			while (this._migrations.Any(m => string.Equals(m.GetType().Name, uniqueName, StringComparison.Ordinal)))
			{
				uniqueName = migrationName + num++;
			}
			return uniqueName;
		}

		public virtual DbMigration GetMigration(string migrationId)
		{
			var dbMigration = (DbMigration)this._migrations.SingleOrDefault(m => string.Equals(m.Id, migrationId, StringComparison.Ordinal));
			if (dbMigration != null)
			{
				dbMigration.Reset();
			}
			return dbMigration;
		}
    }
}
