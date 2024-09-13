using MPWeb.Logic.BLL;
using MPWeb.Logic.BLL.TrackingEngine;
using MPWeb.Logic.Cache;
using MPWeb.Logic.Helpers;
using MPWeb.Logic.Tables;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPBackendTest
{
    public class Program
    {
        static void Main(string[] args)
        {

                DBManager dbm = new DBManager(@"d:\Temp\MPlastWeb\vehicle_tracking_cache.db");
            var recordList = dbm.LoadVehicleTrackingRecordList(new DateTime(2019, 04, 23, 0, 0, 1), new DateTime(2019, 04, 25, 0, 0, 0));

            /*
             * Rendszám:	KRJ-275
            Raktárból kilépés:	05:12
            Előző túrapontról elindulás:	07:52
            Következő túrapontra várható érkezés:	07:53
            Gyújtás:	KI
            Sebesség:	0 km/h

            47.150848, 18.3434953611111

            */

            DateTime TourStartLocalTime = DateTime.Now.Date;
            DateTime TimestampLocalTime = DateTime.Now;
            string Device = "RDY-342";
            double Latitude = 47.417851;
            double Longitude = 16.935364;
            //47.2518889,18.629118
            //47.251864,18.628976}"
            TrackingEngine.Instance.SetManualTrackingData(TourStartLocalTime, TimestampLocalTime, Device, Latitude, Longitude);


        }

        public static void  AuthTest()
        {
            var tokenOK = @"8vkmZgtm%2bkZPHY%2bVVcJseroryY0jF%2bDrEbDo43PC9%2b8%3d";
            var tokenERR = @"neLa%2beRqTfwiE6rVVpH%2bvLn%2bWo6KeFrEh6dCUDBgFKc%3d";

            var bllAuth = new BllAuth();

            var token = Uri.UnescapeDataString(tokenERR);
            var res = bllAuth.AuthenticateUserByToken(token);
        }
        public static void WebVehicleTraceTest()
        {
            var bllWVT = new BllWebVehicleTrace();
            var filterDate = DateTime.UtcNow;

            VehicleTrackingCacheThread vt = new VehicleTrackingCacheThread(ThreadPriority.Normal);
            vt.Run();

            bllWVT.RetriveCachedVehicleData(filterDate.ToUniversalTime());
        }

        public static void InitTrackingEngineTest()
        {
            BllWebTraceTour bllWebTraceTour = new BllWebTraceTour("TEST");
            int Total;
            var retRawX = bllWebTraceTour.RetrieveList(out Total);
            ToursCache.Instance.Items = new System.Collections.Concurrent.ConcurrentBag<PMTour>(retRawX);

            TrackingEngine.Instance.InitTrackingEngine();
        }
        public static void LoginTest()
        {
            var bll = new BllPMLogin("test");
            var item1 = new PMLogin()
            {
                Ticks = DateTime.Now.Ticks.ToString(),
                Date = DateTime.Now.ToString("yyyy.MM.dd"),
                DateTime = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss"),
                OrdNum = "ORD1",
                Name = "Name1",
                Addr = "Addr1"
            };
            bll.MaintainItem(item1);
            Thread.Sleep(1000);     //1 mp várás
            var item2 = new PMLogin()
            {
                Ticks = DateTime.Now.Ticks.ToString(),
                Date = DateTime.Now.ToString("yyyy.MM.dd"),
                DateTime = DateTime.Now.ToString("yyyy.MM.dd hh:mm:ss"),
                OrdNum = "ORD2",
                Name = "Name2",
                Addr = "Addr2"
            };
            bll.MaintainItem(item2);

            var lst = bll.GetLogins(DateTime.Now.AddHours(-1).Ticks, DateTime.Now.Ticks);
            Console.WriteLine(bll.GetLoginsCSV(DateTime.Now.AddHours(-1).Ticks, DateTime.Now.Ticks));

        }
        public static void TrackingEngineTest()
        {
            //BllWebTraceTour bllWebTraceTour = new BllWebTraceTour("test");
            //int Total;
            //DateTime dtStart = DateTime.Now;
            //var ll = bllWebTraceTour.RetrieveList(out Total);
            //Console.WriteLine(String.Format("RetrieveList időtartam:{0}", (DateTime.Now - dtStart).ToString()));
            //TourTraceCacheThread ct = new TourTraceCacheThread(System.Threading.ThreadPriority.Normal);
            //ct.Run();
            //Thread.Sleep(12 * 1000);
            //var ll2 = bllWebTraceTour.RetrieveCachedList();

            //DBManager dbm = new DBManager(@"c:\Users\user\Desktop\vehicle_tracking_cache.db");
            //var recordList = dbm.LoadVehicleTrackingRecordList(new DateTime(2017, 11, 24, 0, 0, 1), new DateTime(2017, 11, 25, 0, 0, 0));

            var bllWTT = new BllWebTraceTour(Environment.MachineName);
            //  bllWTT.RetrieveCachedCompletionList();
            // bllWTT.RetrieveCachedList(new DateTime(2017, 3, 29));


            var tE = TrackingEngine.Instance;

            VehicleTrackingCacheThread vt = new VehicleTrackingCacheThread(ThreadPriority.Normal);
            vt.Run();

        }
    }
}
