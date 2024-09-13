using AzureTableStore;
using MPWeb.Logic.Tables;
using MPWeb.Logic.BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.BLL
{
    public class BllLog : BllBase<Log>
    {
        public BllLog(string p_user)
            :base( p_user)
        {
         }
        public override void MaintainItem(Log p_obj)
        {
            base.MaintainItem(p_obj);
        }

    }
}
