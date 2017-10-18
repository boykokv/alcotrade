using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.SqlServer;

namespace BB.Common.Migrations
{
    public class NonSystemTableSqlGenerator : SqlServerMigrationSqlGenerator
    {
        protected override void Generate(HistoryOperation historyOperation)
        {
           
        }
    }

    public sealed class HistoryConfiguration : DbMigrationsConfiguration<HistoryContext>
    {


        public HistoryConfiguration()
        {
            ContextKey = "US.Model.HistoryContext";
            SetHistoryContextFactory("System.Data.SqlClient", (connection, s) => new HistoryContext(connection, s));
            SetSqlGenerator("System.Data.SqlClient", new NonSystemTableSqlGenerator());
            AutomaticMigrationsEnabled = true;
        }
    }
}
