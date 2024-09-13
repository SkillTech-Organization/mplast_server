using AzureTableStore;
using MPWeb.Logic.Tables;
using System.Collections.Generic;
using System.Linq;

namespace MPWeb.Logic.BLL
{
    public class BllWebTraceTourPoint : BllBase<PMTourPoint>
    {

        public BllWebTraceTourPoint(string p_user) : base(p_user)
        {
        }

        public List<PMTourPoint> RetrieveRange(List<AzureItemKeys> p_itemKeys)
        {
            List<PMTourPoint> res = AzureAccess.Instance.RetrieveRange<PMTourPoint>(p_itemKeys);
            if( res != null)
                res = res.OrderBy(o=>o.Order).ToList();
               
            return res;
        }
    }
}
