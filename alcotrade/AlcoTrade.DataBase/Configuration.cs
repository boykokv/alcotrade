using System.Data.Entity;
using System.Data.Entity.Migrations;
using BB.Common.Migrations;
using BB.Common.Migrations.MigrationGenerator;

namespace AlcoTrade.DataBase
{
    public sealed class Configuration : DbMigrationsConfiguration<CabinetContext>
    {
        public Configuration()
        {
            ContextKey = ContextType.FullName;

            SetHistoryContextFactory("System.Data.SqlClient", (connection, s) => new HistoryContext(connection, s));

            SetSqlGenerator("System.Data.SqlClient", new MigrationGenerator());
        }
    }

    public class CabinetContext : DbContext
    {

    }
}
