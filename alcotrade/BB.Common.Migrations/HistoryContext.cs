using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Migrations.History;
using System.Data.SqlClient;

namespace BB.Common.Migrations
{
    public class HistoryContext : System.Data.Entity.Migrations.History.HistoryContext 
    {
        public HistoryContext(DbConnection dbConnection, string defaultSchema)
            : base(dbConnection, defaultSchema)
        {
        }

        public HistoryContext() : this(new SqlConnection(), null)
        {
            
        }

        public IDbSet<MigrationHistoryRow> Migration { get; set; }
        
        
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HistoryRow>().HasKey(row => new { row.MigrationId, row.ContextKey });
            modelBuilder.Entity<HistoryRow>().ToTable("Migration");
            modelBuilder.Entity<HistoryRow>().Property(row => row.Model).IsOptional();
            modelBuilder.Entity<MigrationHistoryRow>().ToTable("Migration");
        }
    }

    public class MigrationHistoryRow : HistoryRow
    {
        public string HashCode { get; set; }
        public DateTime MigrationTime { get; set; }
    }
}
