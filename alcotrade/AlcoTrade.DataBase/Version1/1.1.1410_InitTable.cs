using BB.Common.Migrations;

namespace AlcoTrade.DataBase.Version1
{
    public class InitialTables : BaseMigration, IMetadataMigration
    {
        public override void Up()
        {
            CreateTable("Goods", c => new
            {
                Id = c.Int(nullable: false, identity: true),
                Name = c.String(nullable: false),
                GoodId = c.Int(nullable: false)
            }).PrimaryKey(t => t.Id);

            CreateTable("GoodGroups", c => new
            {
                Id = c.Int(nullable: false, identity: true),
                Name = c.String(nullable: false),
            }).PrimaryKey(t => t.Id);

            AddForeignKey("Goods", "GoodId", "GoodGroups", "Id");

            CreateTable("Bottle", c => new
            {
                Id = c.Long(nullable: false, identity: true),
                Name = c.DateTime(nullable: false),
            }).PrimaryKey(t => t.Id);

            
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
