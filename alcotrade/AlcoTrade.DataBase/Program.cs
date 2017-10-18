using BB.Common.Migrations.Utils;
using BB.Core.Log;

namespace AlcoTrade.DataBase
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.TurnOnConsoleOutput();
            DbManager<CabinetContext, Configuration>.Run(args);
        }
    }
}
