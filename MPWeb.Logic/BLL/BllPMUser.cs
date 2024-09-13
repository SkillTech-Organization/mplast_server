using MPWeb.Logic.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.BLL
{
    public class BllPMUser : BllBase<PMUser>
    {
        public BllPMUser(string p_user)
            :base( p_user)
        {
        }
        public override void MaintainItem(PMUser p_obj)
        {
            base.MaintainItem(p_obj);
        }
    }
}
