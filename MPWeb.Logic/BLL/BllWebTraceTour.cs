using MPWeb.Logic.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using MPWeb.Logic.Helpers;
using Newtonsoft.Json;
using MPWeb.Logic.Tables;
using MPWeb.Logic.BLL.TrackingEngine;

namespace MPWeb.Logic.BLL
{
    public class BllWebTraceTour : BllBase<PMTour>
    {

        BllWebTraceTourPoint m_bllWebTraceTourPoint;

        public BllWebTraceTour(string p_user) : base(p_user)
        {
            m_bllWebTraceTourPoint = new BllWebTraceTourPoint(p_user);
        }

        public override PMTour Retrieve(object p_partitionKey, object p_rowKey)
        {
            var tour = base.Retrieve(p_partitionKey, p_rowKey);
            if (tour != null)
            {
                int total;
                var tp = m_bllWebTraceTourPoint.RetrieveList(out total, String.Format("PartitionKey eq '{0}' ", tour.ID)).ToList();
                if (tp != null)
                {
                    tour.TourPoints = tp.OrderBy(o=>o.Order).ToList();
                }
            }
            return PreprocessRawTour(tour);
        }

        public override List<PMTour> RetrieveList(out int Total, string p_where = "", string p_orderBy = "", int pageSize = 0, int page = 1)
        {
            var tourList = base.RetrieveList(out Total, p_where, p_orderBy);
            foreach (var tour in tourList)
            {
                int total;
                var tp = m_bllWebTraceTourPoint.RetrieveList(out total, String.Format("PartitionKey eq '{0}' ", tour.ID));
                if (tp != null)
                {
                    tour.TourPoints = tp.OrderBy(o => o.Order).ToList();
                }
            }
            return PreprocessRawTourList(tourList);
        }

        private List<PMTour> PreprocessRawTourList(List<PMTour> tourList)
        {
            foreach (var t in tourList)
            {
                PreprocessRawTour(t);
            }
            return tourList;
        }

        private PMTour PreprocessRawTour(PMTour t)
        {
            if (t.TruckRegNo.Contains("/"))
            {
                t.TruckRegNo = t.TruckRegNo.Split('/')[0];
            }
            if (t.TruckRegNo.Contains("\\"))
            {
                t.TruckRegNo = t.TruckRegNo.Split('\\')[0];
            }
            if (t.TruckRegNo.Contains("."))
            {
                t.TruckRegNo = t.TruckRegNo.Split('.')[0];
            }
            if (t.TruckRegNo.Contains(":"))
            {
                t.TruckRegNo = t.TruckRegNo.Split(':')[0];
            }
            if (t.TruckRegNo.Contains(","))
            {
                t.TruckRegNo = t.TruckRegNo.Split(',')[0];
            }

            /*Az új formátumú rendszámok miatt nem kell
            if (t.TruckRegNo.Length >= 7)
            {
                t.TruckRegNo = t.TruckRegNo.Substring(0, 7);
            }
            */
            /*
            int shiftHours = 0;
            t.Start = new DateTime(t.Start.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);
            t.End = new DateTime(t.End.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);
            t.Updated = new DateTime(t.Updated.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);

            foreach (var tp in t.TourPoints)
            {
                tp.ArrTime = new DateTime(tp.ArrTime.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);
                tp.ServTime = new DateTime(tp.ServTime.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);
                tp.DepTime = new DateTime(tp.DepTime.Subtract(new TimeSpan(shiftHours, 0, 0)).Ticks, DateTimeKind.Utc);
            }
            */
            //
            //t.Start = t.Start.ToUniversalTime();
            //t.End = t.End.ToUniversalTime();
            //t.Updated = t.Updated.ToUniversalTime();

            //foreach (var tp in t.TourPoints)
            //{
            //    tp.ArrTime = tp.ArrTime.ToUniversalTime();
            //    tp.ServTime = tp.ServTime.ToUniversalTime();
            //    tp.DepTime = tp.DepTime.ToUniversalTime();
            //}

            return t;
        }

        public List<PMTour> RetrieveFilteredCachedList(DateTime filterDate)
        {
            var retRaw = new List<PMTour>();
            //////////////////////////////////////////// TEST DATA ////////////////////////////////////////////////////////////////////////////////
            // TODO cicca
            //TESZT: retRaw = TestTourData.Test_LoadTourData2();
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
           /*->*/ using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
            {
                //              retRaw = ToursCache.Instance.Items.Where(x => x.Start.ToUniversalTime().Date.Equals(filterDate.Date)).ToList();


                var fdate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(filterDate, DateTimeKind.Unspecified), tz);

                retRaw = ToursCache.Instance.Items.Where(x =>
                                TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(x.Start, DateTimeKind.Unspecified), tz).Date.CompareTo(fdate.Date) == 0).ToList();

                if (retRaw.Count() == 0)
                {

                    //Ha nincs még feltöltve a cache, feltöltjük (tesztelésnél szükséges)
                    refreshToursCache();
                    retRaw = ToursCache.Instance.Items.Where(x =>
                                    TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(x.Start, DateTimeKind.Unspecified), tz).Date.CompareTo(fdate.Date) == 0).ToList();
                }

            }
            /**/
            return JsonConvert.DeserializeObject<List<PMTour>>(JsonConvert.SerializeObject(retRaw));
        }
        public List<PMTour> RetrieveCachedListByTourID(List<int> tourIdList)
        {
            var retRaw = new List<PMTour>();
            using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
            {
                retRaw = ToursCache.Instance.Items.Where(x => tourIdList.Any(a=>a == int.Parse(x.ID))).ToList();

                if (retRaw.Count() == 0)
                {

                    //Ha nincs még feltöltve a cache, feltöltjük (tesztelésnél szükséges)
                    refreshToursCache();
                    retRaw = ToursCache.Instance.Items.Where(x => tourIdList.Any(a => a == int.Parse(x.ID))).ToList();
                }
            }
            /**/
            return JsonConvert.DeserializeObject<List<PMTour>>(JsonConvert.SerializeObject(retRaw));
        }

        public List<PMTour> RetrieveFullCachedList()
        {
            var retRaw = new List<PMTour>();
            using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
            {
                retRaw = ToursCache.Instance.Items.ToList();

                if (retRaw.Count() == 0)
                {
                    //Ha nincs még feltöltve a cache, feltöltjük (tesztelésnél szükséges)
                    refreshToursCache();
                    retRaw = ToursCache.Instance.Items.ToList();
                }

            }
            /**/
            return JsonConvert.DeserializeObject<List<PMTour>>(JsonConvert.SerializeObject(retRaw));
        }

        public List<PMTour> RetrieveProcessedCachedList(DateTime filterDate)
        {
            var retTourList = RetrieveFilteredCachedList(filterDate);
            ProcessTourList(retTourList);
            return retTourList;
        }
        public List<PMTour> RetrieveFullProcessedCachedList()
        {
            var retTourList = RetrieveFullCachedList();
            ProcessTourList(retTourList);
            return retTourList;
        }
        public List<PMTour> RetrieveCachedCompletionList(List<PMTracedTour> tourPointFilterList = null)
        {
            var ret = new List<PMTour>();
            using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
            {
                var tmpRet = TrackingEngine.TrackingEngine.Instance.ProcessedTourDataCache;
                if (tmpRet != null)
                {
                    ret = JsonConvert.DeserializeObject<List<PMTour>>(JsonConvert.SerializeObject(tmpRet));
                    ProcessTourList(ret);
                }
            }

            if (tourPointFilterList != null)
            {
                ret = FilterTourList(ret, tourPointFilterList);
            }

            return ret;
        }

        public List<PMTour> RetrieveCachedListForTempUser(List<PMTracedTour> tourPointFilterList)
        {
            var tmpTourList = new List<PMTour>();
            using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
            {
                tmpTourList = JsonConvert.DeserializeObject<List<PMTour>>(
                     JsonConvert.SerializeObject(ToursCache.Instance.Items.ToList()));
            }

            ProcessTourList(tmpTourList);

            return FilterTourList(tmpTourList, tourPointFilterList);
        }

        private void ProcessTourList(List<PMTour> tourList)
        {
            ConvertTimesToLocalTimeZone(tourList);
        }

        private void ConvertTimesToLocalTimeZone(List<PMTour> tourList)
        {
 
            
            TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById("Central Europe Standard Time");
 /*
            foreach (var t in tourList)
            {
                
                t.Start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(t.Start, DateTimeKind.Unspecified), tz); 
                t.End = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(t.End, DateTimeKind.Unspecified), tz);
                t.Updated = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(t.Updated, DateTimeKind.Unspecified), tz);
                foreach (var tp in t.TourPoints)
                {

                    tp.ArrTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.ArrTime, DateTimeKind.Unspecified), tz);  
                    tp.ServTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.ServTime, DateTimeKind.Unspecified), tz); 
                    tp.DepTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.DepTime, DateTimeKind.Unspecified), tz); 

                    if (tp.RealArrTime != null)
                    {
                        tp.RealArrTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.RealArrTime, DateTimeKind.Unspecified), tz); 
                    }
                    if (tp.RealServTime != null)
                    {
                        tp.RealServTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.RealServTime, DateTimeKind.Unspecified), tz); 
                    }
                    if (tp.RealDepTime != null)
                    {
                        tp.RealDepTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.RealDepTime, DateTimeKind.Unspecified), tz); 
                    }
                    if (tp.PredictedArrTime != null)
                    {
                        tp.PredictedArrTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.PredictedArrTime, DateTimeKind.Unspecified), tz); 
                    }
                    if (tp.PredictedServTime != null)
                    {
                        tp.PredictedServTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.PredictedServTime, DateTimeKind.Unspecified), tz); 
                    }
                    if (tp.PredictedDepTime != null)
                    {
                        tp.PredictedDepTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(tp.PredictedDepTime, DateTimeKind.Unspecified), tz); 
                    }
                }
            }
*/
            foreach (var t in tourList)
            {

                t.Start = TimeZoneInfo.ConvertTimeFromUtc(t.Start, tz);
                t.End = TimeZoneInfo.ConvertTimeFromUtc(t.End, tz);
                t.Updated = TimeZoneInfo.ConvertTimeFromUtc(t.Updated, tz);
                foreach (var tp in t.TourPoints)
                {

                    tp.ArrTime = TimeZoneInfo.ConvertTimeFromUtc(tp.ArrTime, tz);
                    tp.ServTime = TimeZoneInfo.ConvertTimeFromUtc(tp.ServTime, tz);
                    tp.DepTime = TimeZoneInfo.ConvertTimeFromUtc(tp.DepTime, tz);

                    if (tp.RealArrTime != null)
                    {
                        tp.RealArrTime = TimeZoneInfo.ConvertTimeFromUtc(tp.RealArrTime, tz);
                    }
                    if (tp.RealServTime != null)
                    {
                        tp.RealServTime = TimeZoneInfo.ConvertTimeFromUtc(tp.RealServTime, tz);
                    }
                    if (tp.RealDepTime != null)
                    {
                        tp.RealDepTime = TimeZoneInfo.ConvertTimeFromUtc( tp.RealDepTime, tz);
                    }
                    if (tp.PredictedArrTime != null)
                    {
                        tp.PredictedArrTime = TimeZoneInfo.ConvertTimeFromUtc( tp.PredictedArrTime, tz);
                    }
                    if (tp.PredictedServTime != null)
                    {
                        tp.PredictedServTime = TimeZoneInfo.ConvertTimeFromUtc( tp.PredictedServTime, tz);
                    }
                    if (tp.PredictedDepTime != null)
                    {
                        tp.PredictedDepTime = TimeZoneInfo.ConvertTimeFromUtc( tp.PredictedDepTime, tz);
                    }
                }
            }


        }

        private List<PMTour> FilterTourList(List<PMTour> tourList, List<PMTracedTour> tourPointFilterList)
        {
            var retTourList = new List<PMTour>();
            foreach (var t in tourList)
            {
                if (tourPointFilterList.Find(x => x.TourID == int.Parse(t.ID)) != null)
                {
                    var tpIds = tourPointFilterList.Where(y => y.TourID == int.Parse(t.ID)).Select(x => x.Order);
                    var maxOrder = tpIds.Max();
                    var tmpTPList = new List<PMTourPoint>();

                    for (var i = 0; i <= maxOrder; i++)
                    {
                        var tp = t.TourPoints[i];
                        if (!tpIds.Contains(tp.Order))
                        {
                            tp.Name = "";
                            tp.Addr = "";
                            tp.ArrTime = DateTime.MinValue;
                            tp.DepTime = DateTime.MinValue;
                            tp.Distance = 0;
                            tp.PredictedArrTime = DateTime.MinValue;
                            tp.PredictedDepTime = DateTime.MinValue;
                            tp.PredictedServTime = DateTime.MinValue;
                            tp.RealArrTime = DateTime.MinValue;
                            tp.RealDepTime = DateTime.MinValue;
                            tp.RealServTime = DateTime.MinValue;
                            tp.ServTime = DateTime.MinValue;
                            tp.IsEverVisible = false;
                        }
                        tmpTPList.Add(tp);
                    }
                    t.TourPoints = tmpTPList;
                    retTourList.Add(t);
                }
            }
            return retTourList;
        }

        private void refreshToursCache()
        {
            int Total;
            BllWebTraceTour bllWebTraceTour = new BllWebTraceTour("CICCA");
            var retRawX = bllWebTraceTour.RetrieveList(out Total);
            ToursCache.Instance.Items = new System.Collections.Concurrent.ConcurrentBag<PMTour>(retRawX);
        }
    }
}
