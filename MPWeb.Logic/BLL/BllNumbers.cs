using MPWeb.Logic.Tables;
using System.Collections.Generic;

namespace MPWeb.Logic.BLL
{
    public class BllNumbers : BllBase<Numbers>
    {
        public BllNumbers() : base("")
        {
        }

        public Numbers Retrieve(object rowkey)
        {
            Numbers number =  base.Retrieve(Numbers.PartitonConst, rowkey);
            if (number.UsedNumberList == null)      //ha null, akkor hozzuk létre!
            {
                number.UsedNumberList = new List<Numbers.CUsedNumber>();
            }
            return number;
        }
    }
}
