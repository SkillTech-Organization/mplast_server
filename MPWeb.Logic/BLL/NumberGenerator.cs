
using AzureTableStore;
using MPWeb.Logic.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.BLL
{
    public class NumberGenerator
    {
        //Lazy objects are thread safe, double checked and they have better performance than locks.
        //see it: http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Lazy<NumberGenerator> m_instance = new Lazy<NumberGenerator>(() => new NumberGenerator(), true);

        static public NumberGenerator Instance                   //inicializálódik, ezért biztos létrejon az instance osztály)
        {
            get
            {
                return m_instance.Value;            //It's thread safe!
            }
        }

        private const int ExpiredInMinutes = 30;
        /// <summary>
        /// Vissazadja a kódhoz tartozo kovetkezo sorszamot
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string GetNext(string p_code, string p_sessionID, string p_user)
        {
            lock (this)
            {

                int retNumber = 0;
                p_code = p_code.ToUpper();
                BllNumbers bllNumber = new BllNumbers();
                var number = bllNumber.Retrieve(p_code);
                if (number == null)
                {
                    number = new Numbers() { Code = p_code };
                    number.Number = 0;
                }

                //teszthez:number.UsedNumberList.OrderBy( o=>o.TimeStamp).Select(s=>s.Number.ToString() + "-->" +  s.TimeStamp.ToString()).ToList()
                //
                
                Numbers.CUsedNumber oldestExpired = number.UsedNumberList.Where(w => DateTime.UtcNow.Ticks - w.Ticks > TimeSpan.TicksPerMinute * ExpiredInMinutes)
                                    //                                       .OrderBy(o => o.Ticks).FirstOrDefault();
                                    .OrderBy(o => o.Number).FirstOrDefault();
                if (oldestExpired == null)
                {
                    number.Number += 1;
                    number.UsedNumberList.Add(new Numbers.CUsedNumber()
                    { Number = number.Number, Ticks = DateTime.UtcNow.Ticks, SessionID = p_sessionID, User = p_user });
                    retNumber = number.Number;
                }
                else
                {
                    retNumber = oldestExpired.Number;
                    oldestExpired.Ticks = DateTime.UtcNow.Ticks;
                    oldestExpired.SessionID = p_sessionID;
                    number.State = AzureTableStore.AzureTableObjBase.enObjectState.Modified;
                }
                bllNumber.MaintainItem(number);

                return $"{retNumber:D4}";
            }
        }

        public bool FinalizeNumber(string p_code, int p_num, bool p_rollback = false)
        {
            lock (this)
            {
                BllNumbers bllNumber = new BllNumbers();
                var number = bllNumber.Retrieve(p_code);
                if (number != null)
                {


                    Numbers.CUsedNumber usedNum = number.UsedNumberList.Where(w => w.Number == p_num).FirstOrDefault();
                    if (usedNum != null)
                    {
                        if (p_rollback)
                        {
                            if (number.Number == p_num)     //ha a legutolsó számot rollbackoljuk, akkor az törölhető az UsedNumberList-ből
                                number.UsedNumberList.RemoveAll(w => w.Number == p_num);
                            else
                                usedNum.Ticks = 0;            //ellenben azonnal újra felhasználhatóvá tesszük
                            number.State = AzureTableObjBase.enObjectState.Modified;
                        }
                        else
                        {
                            //A szám felhasználása végleges, töröljük a  UsedNumberList-ből
                            number.UsedNumberList.RemoveAll(w => w.Number == p_num);
                            number.State = AzureTableObjBase.enObjectState.Modified;
                        }

                    }
                    else
                    {
                        if (!p_rollback)
                        {
                            var msg = String.Format("{0} counter has alerady been finalized. Number:{1}", p_code, p_num);
                     //       InvoiceHandler.WriteLog(AzureTableStore.AzureTableStore.Instance.NextID(), DateTime.UtcNow.Ticks, "FinalizeNumber", "ERROR", "", DateTime.Now.ToString("yyyy-MM-dd HH:MM:ss"), "", msg);
                        }

                    }

                    if (p_rollback)
                    {
                        if (number.Number == p_num)
                            number.Number = p_num - 1;
                    }
                    bllNumber.MaintainItem(number);

                }
                else
                {
                    throw new Exception(String.Format("Invalid code:{0}", p_code));
                }
                return true;
            }
        }


        public string SafeGetNext(string p_code, string p_sessionID, string p_user)
        {
            string num = "";
            string res = "???";
            try
            {
                num = NumberGenerator.Instance.GetNext(p_code, p_sessionID, p_user);
                res = p_code + num;
            }
            catch (Exception ex)
            {
                NumberGenerator.Instance.FinalizeNumber(p_code, Int32.Parse(num), true);
                throw;
            }
            return res;
        }
    }
}
