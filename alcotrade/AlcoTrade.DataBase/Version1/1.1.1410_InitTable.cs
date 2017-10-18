using BB.Common.Migrations;

namespace AlcoTrade.DataBase.Version1
{
    public class InitialTables : BaseMigration, IMetadataMigration
    {
        public override void Up()
        {
            
        }

        public string Id
        {
            get
            {
                return "1.1.1410_InitTable";
            }
        }

        public override void Down()
        {
        }
    }
}
