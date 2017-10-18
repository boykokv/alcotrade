using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cabinet.Common.Migrations;

namespace Cabinet.DataBase.Version1
{
    public class MessageUnread: BaseMigration, IMetadataMigration
    {
        public override void Up()
        {
            AddColumn("Messages", "Readed", c => c.Boolean(nullable:false, defaultValue:false));
        }

        public string Id
        {
            get
            {
                return "1.7.2202_MessageUnread";
            }
        }
    }
}
