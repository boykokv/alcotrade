using System.Data.Entity.Infrastructure;
using System.Xml.Linq;

namespace BB.Common.Migrations.Extensions
{
    internal static class DbModelExtensions
    {
        public static XDocument GetModel(this DbModel model)
        {
            return DbContextExtensions.GetModel(w => EdmxWriter.WriteEdmx(model, w));
        }
    }
}
