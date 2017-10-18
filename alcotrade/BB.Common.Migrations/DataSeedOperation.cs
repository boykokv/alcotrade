using System.Collections.Generic;

namespace BB.Common.Migrations
{
    public class DataSeedOperation
    {
        public string Sql { get; set; }
        public Dictionary<string, object> Prms { get; set; }
    }
}
