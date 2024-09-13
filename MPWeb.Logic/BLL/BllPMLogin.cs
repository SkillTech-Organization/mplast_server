using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.BLL
{
    public class BllPMLogin : BllBase<PMLogin>
    {

        public BllPMLogin(string p_user)
            : base(p_user)
        {
        }
        public override void MaintainItem(PMLogin p_obj)
        {
            base.MaintainItem(p_obj);
        }

        public List<PMLogin> GetLogins(long p_ticksFrom, long p_ticksTo)
        {
            int total = 0;
            var lstLogins = RetrieveList(out total, String.Format("RowKey ge '{0}' and RowKey le '{1}'", p_ticksFrom, p_ticksTo));

            var res = lstLogins.GroupBy(g1 => new { g1.Date, g1.OrdNum, g1.Name, g1.Addr })
                .Select(s => new PMLogin()
                { Date = s.Key.Date,
                    OrdNum = s.Key.OrdNum,
                    Name = s.Key.Name,
                    Addr = s.Key.Addr,
                    Count = s.Count()
                }).OrderBy(o => o.Date).ThenBy(t => t.OrdNum).ToList();
            return res;
        }

        public string GetLoginsCSV(long p_ticksFrom, long p_ticksTo)
        {
            var lst = GetLogins(p_ticksFrom, p_ticksTo);
            DateTime from = DateTime.SpecifyKind(new DateTime(p_ticksFrom), DateTimeKind.Utc);
            DateTime to = DateTime.SpecifyKind(new DateTime(p_ticksTo), DateTimeKind.Utc);
            string res = "Belépések " + from.ToLocalTime().ToString("yyyy.MM.dd") + "-tól " + to.ToLocalTime().ToString("yyyy.MM.dd") + "-ig\n";

           res += "Dátum;Megrendelés;Ügyfélnév;Cím;Belépések száma\n";
            lst.ForEach(item =>
               res += item.Date + ";" + item.OrdNum + ";" + item.Name + ";" + item.Addr + ";" + item.Count.ToString() + "\n");
            /*
                        var utf8 = Encoding.UTF8;
                        byte[] utfBytes = utf8.GetBytes(res);
                        res = utf8.GetString(utfBytes, 0, utfBytes.Length);
            */


            //https://stackoverflow.com/questions/6588068/which-encoding-opens-csv-files-correctly-with-excel-on-both-mac-and-windows
            /*
            Encoding iso = Encoding.GetEncoding("Windows-1250");
            byte[] Bytes = iso.GetBytes(res);
            res = iso.GetString(Bytes, 0, Bytes.Length);
            */
            /*
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            byte[] Bytes = iso.GetBytes(res);
            res = iso.GetString(Bytes, 0, Bytes.Length);
            */
            res = res.Replace("Á", "A");
            res = res.Replace("Í", "I");
            res = res.Replace("Ű", "U");
            res = res.Replace("Ő", "O");
            res = res.Replace("Ü", "U");
            res = res.Replace("Ö", "O");
            res = res.Replace("Ú", "U");
            res = res.Replace("Ó", "O");
            res = res.Replace("É", "E");

            res = res.Replace("á", "a");
            res = res.Replace("í", "i");
            res = res.Replace("ű", "u");
            res = res.Replace("ő", "o");
            res = res.Replace("ü", "u");
            res = res.Replace("ö", "o");
            res = res.Replace("ú", "u");
            res = res.Replace("ó", "o");
            res = res.Replace("é", "e");
            
            Encoding ascii = Encoding.ASCII;
            byte[] Bytes = ascii.GetBytes(res);
            res = ascii.GetString(Bytes, 0, Bytes.Length);
            

            return res;
        }
            


    }

}
