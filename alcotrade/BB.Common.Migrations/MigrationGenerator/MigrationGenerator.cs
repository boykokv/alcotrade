using System.Data.Entity.Migrations.Model;
using System.Data.Entity.SqlServer;
using BB.Common.Migrations.Interfaces;

namespace BB.Common.Migrations.MigrationGenerator
{
    /// <summary>
    /// Служит для обработки кастомных MigrationOperation
    /// </summary>
    public class MigrationGenerator : SqlServerMigrationSqlGenerator
    {
        protected override void Generate(MigrationOperation migrationOperation)
        {
            var operation = migrationOperation as ITemplatable;
            if (operation != null)
            {
                foreach (var query in operation.Template)
                {
                    using (var writer = Writer())
                    {
                        writer.WriteLine(query);
                        Statement(writer);
                    }
                }
            }
        }
    }
}
