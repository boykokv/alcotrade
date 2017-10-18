using System.Data.Common;
using System.Data.Entity;

namespace BB.Common.Migrations.Utils
{
    public class DatabaseCreator
    {
        private readonly int? _commandTimeout;

        public DatabaseCreator(int? commandTimeout)
        {
            this._commandTimeout = commandTimeout;
        }

        public virtual bool Exists(DbConnection connection)
        {
            using (var emptyContext = new DbContext(connection, false))
            {
                emptyContext.Database.CommandTimeout = this._commandTimeout;
                return emptyContext.Database.Exists();
            }
        }

        public virtual void Create(DbConnection connection)
        {
            using (var emptyContext = new DbContext(connection, false))
            {
                emptyContext.Database.CommandTimeout = this._commandTimeout;
                emptyContext.Database.Create();
            }
        }

        public virtual void Delete(DbConnection connection)
        {
            using (var emptyContext = new DbContext(connection, false))
            {
                emptyContext.Database.CommandTimeout = this._commandTimeout;
                emptyContext.Database.Delete();
            }
        }
    }
}