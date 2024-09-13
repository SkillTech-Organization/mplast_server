using MPWeb.Logic.Cache;
using MPWeb.Logic.Tables;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace MPWeb.Logic.BLL.TrackingEngine
{
    public class TrackingEngine
    {
        private static TrackingEngine instance;
        private bool m_TrackingEnginePathModeRelative;
        private bool m_TrackingEngineLogComputations;
        private string m_PMTourMapDirPath;
        private string m_PMTourMapIniDirPath;
        private double m_epsilonTourPointCompletedFastInKm = 2.0;
        private string m_trackingEngineLogDirPath;
        private string m_ComputationLogFilePath;
        private string m_TPCompletionLogFilePath;
        private DateTime m_currentDateOfProcessing;
        private BllWebTraceTour m_bllWTT;

        private static readonly object processedTourCacheLock = new object();
        private List<PMTour> m_processedTourDataCache = new List<PMTour>();
        public List<PMTour> ProcessedTourDataCache
        {
            get
            {
                lock (processedTourCacheLock)
                {
                    return JsonConvert.DeserializeObject<List<PMTour>>(JsonConvert.SerializeObject(m_processedTourDataCache));
                }
            }
            private set
            {
                lock (processedTourCacheLock)
                {
                    m_processedTourDataCache = value;
                }
            }
        }
        private bool m_processTourDataCacheInitialized { get; set; } = false;

        public int MaxProcessingThreads { get; set; }

        private DBManager m_dbManager;

        public static TrackingEngine Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TrackingEngine();
                }
                return instance;
            }
        }


        private TrackingEngine(string mapDirPath = null)
        {
            InitTrackingEngine(mapDirPath = null);
        }

        public void InitTrackingEngine(string mapDirPath = null)
        {
            this.ProcessedTourDataCache = new List<PMTour>();

            m_bllWTT = new BllWebTraceTour(Environment.MachineName);

            MaxProcessingThreads = Environment.ProcessorCount;
            MaxProcessingThreads = 1;

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["TrackingEnginePathModeRelative"]))
            {
                throw new Exception("ERROR: Application parameter TrackingEnginePathModeRelative is not set.");
            }
            m_TrackingEnginePathModeRelative = bool.Parse(
                ConfigurationManager.AppSettings["TrackingEnginePathModeRelative"]);

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["TrackingEnginePathModeRelative"]))
            {
                throw new Exception("ERROR: Application parameter TrackingEngineLogComputations is not set.");
            }
            m_TrackingEngineLogComputations = bool.Parse(
                ConfigurationManager.AppSettings["TrackingEngineLogComputations"]);

            if (mapDirPath == null)
            {
                if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["TrackingEnginePMapMapDirPath"]))
                {
                    throw new Exception("ERROR: Application parameter TrackingEnginePMapMapDirPath is not set.");
                }
                if (m_TrackingEnginePathModeRelative)
                {
                    m_PMTourMapDirPath = System.Web.Hosting.HostingEnvironment.MapPath(
                    "~" + ConfigurationManager.AppSettings["TrackingEnginePMapMapDirPath"]);
                }
                else
                {
                    m_PMTourMapDirPath = ConfigurationManager.AppSettings["TrackingEnginePMapMapDirPath"];
                }
            }
            else
            {
                m_PMTourMapDirPath = mapDirPath;
            }

            if (!Directory.Exists(m_PMTourMapDirPath))
            {
                throw new Exception("ERROR: Directory given by TrackingEnginePMapMapDirPath parameter does not exist: " +
                    ConfigurationManager.AppSettings["TrackingEnginePMapMapDirPath"]);
            }

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["TrackingEnginePMapIniDirPath"]))
            {
                throw new Exception("ERROR: Application parameter TrackingEnginePMapIniDirPath is not set.");
            }
            if (m_TrackingEnginePathModeRelative)
            {
                m_PMTourMapIniDirPath = System.Web.Hosting.HostingEnvironment.MapPath(
                    "~" + ConfigurationManager.AppSettings["TrackingEnginePMapIniDirPath"]);
            }
            else
            {
                m_PMTourMapIniDirPath = ConfigurationManager.AppSettings["TrackingEnginePMapIniDirPath"];
            }

            if (!Directory.Exists(m_PMTourMapIniDirPath))
            {
                throw new Exception("ERROR: Directory given by TrackingEnginePMapIniDirPath parameter does not exist: " +
                    ConfigurationManager.AppSettings["TrackingEnginePMapIniDirPath"]);
            }

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["EpsilonTourPointCompletedFastInKm"]))
            {
                throw new Exception("ERROR: Application parameter EpsilonTourPointCompletedFastInKm is not set.");
            }
            m_epsilonTourPointCompletedFastInKm = double.Parse(
                ConfigurationManager.AppSettings["EpsilonTourPointCompletedFastInKm"]);

            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["TrackingEngineLogDirPath"]))
            {
                throw new Exception("ERROR: Application parameter TrackingEngineLogDirPath is not set.");
            }
            if (m_TrackingEnginePathModeRelative)
            {
                m_trackingEngineLogDirPath = System.Web.Hosting.HostingEnvironment.MapPath(
                    "~" + ConfigurationManager.AppSettings["TrackingEngineLogDirPath"]);
            }
            else
            {
                m_trackingEngineLogDirPath = ConfigurationManager.AppSettings["TrackingEngineLogDirPath"];
            }

            if (!Directory.Exists(m_trackingEngineLogDirPath))
            {
                try
                {
                    Directory.CreateDirectory(m_trackingEngineLogDirPath);
                }
                catch (Exception e)
                {
                    throw new Exception("ERROR: Could not create log directory specified by TrackingEngineLogDirPath parameter: " + e.ToString());
                }
            }

            // TODO cicca
            /*-> 
            if (!FastSelfTest())
            {
                throw new Exception("ERROR: FastSelfTest FAILED. Please read logs in: " + m_trackingEngineLogDirPath);
            }

             */
            using (LockForCache lockObj = new LockForCache(VehicleTrackingCache.Locker))
            {
                VehicleTrackingCache.Instance.CachedPositionData = new System.Collections.Concurrent.ConcurrentBag<VehiclePositionData>();
            }
            m_dbManager = new DBManager();
            m_currentDateOfProcessing = DateTime.UtcNow.Date;

            m_processTourDataCacheInitialized = InitializeProcessTourDataCacheForDay(m_currentDateOfProcessing, "Constructor");
        }

        private void InitializeTrackingEngineLogFileNames(DateTime dt, string initalizationInitiator)
        {
            string computationLogFileName = "CL_" + dt.Year + "-" + dt.Month + "-" + dt.Day
                + "T" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "_" + new Random().Next() +
                "_[" + initalizationInitiator + "]" + ".txt";
            m_ComputationLogFilePath = m_trackingEngineLogDirPath + "\\" + computationLogFileName;
            string tpCompletionLogFileName = "TPCL_" + dt.Year + "-" + dt.Month + "-" + dt.Day
                + "T" + dt.Hour + "_" + dt.Minute + "_" + dt.Second + "_" + new Random().Next() +
                "_[" + initalizationInitiator + "]" + ".txt";
            m_TPCompletionLogFilePath = m_trackingEngineLogDirPath + "\\" + tpCompletionLogFileName;
        }

        /// <summary>
        /// Fast-TESTS to check for possible 3rd party inclusion errors
        /// </summary>
        /// <returns>self test result</returns>
        private bool FastSelfTest()
        {
            string logFileName = "" + DateTime.UtcNow.Year + "-" + DateTime.UtcNow.Month + "-" + DateTime.UtcNow.Day
                + "_" + DateTime.UtcNow.Hour + "" + DateTime.UtcNow.Minute + DateTime.UtcNow.Second + ".txt";
            string logFilePath = m_trackingEngineLogDirPath + "\\" + logFileName;
            using (StreamWriter logStream = new StreamWriter(logFilePath))
            {
                PMMapPoint testPoint1 = new PMMapPoint
                {
                    Lat = 47.1506631111111,
                    Lng = 18.343794
                };
                PMMapPoint testPoint2 = new PMMapPoint
                {
                    Lat = 47.1507555555556,
                    Lng = 18.3439433611111
                };

                try
                {
                    // Budapest {Lat: 47.4984, Lng: 19.0408)
                    // Szeged {Lat: 46.2536, Lng: 20.1461)
                    // Distance between them in km: 162 km
                    double epsilon = 1.0;
                    double d = ComputeFastDistanceInKm(
                        new PMMapPoint { Lat = 47.4984, Lng = 19.0408 },
                        new PMMapPoint { Lat = 46.2536, Lng = 20.1461 });
                    double delta = Math.Abs(d - 162);
                    if (delta <= epsilon)
                    {
                        logStream.WriteLine(DateTime.UtcNow.ToString() + ": FastSelfTest/ComputeFastDistanceInKm TEST PASSED. " +
                            "Computed values: Distance in km: " + d + " epsilon: " + epsilon + " delta: " + delta);
                    }
                    else
                    {
                        logStream.WriteLine(DateTime.UtcNow.ToString() + ": FastSelfTest/ComputeFastDistanceInKm TEST FAILED. Epsilon: " +
                            epsilon + "Delta: " + delta);
                    }

                    bool computeSuccess = PMRoute.RouteFuncs.GetDistance(m_PMTourMapIniDirPath, "DB0", m_PMTourMapDirPath,
                    testPoint1.Lat, testPoint1.Lng, testPoint2.Lat, testPoint2.Lng, "1,2,3,4,5,6,7,8,9,10,12,12,13,14,15",
                    0, 0, 0, out int dist, out int dur);


                    if (computeSuccess)
                    {
                        MapDistance computedDistance = new MapDistance
                        {
                            Distance = dist,
                            Duration = dur
                        };
                        logStream.WriteLine(DateTime.UtcNow.ToString() + ": FastSelfTest/GetDistance TEST PASSED. " +
                            "Computed values: Distance: " + computedDistance.Distance + ". Duration: " +
                             computedDistance.Duration + ".");
                    }
                    else
                    {
                        logStream.WriteLine(DateTime.UtcNow.ToString() + ": FastSelfTest/GetDistance TEST FAILED. Error unknown.");
                    }
                }
                catch (Exception e)
                {
                    logStream.WriteLine(DateTime.UtcNow.ToString() + ": FastSelfTest/GetDistance TEST FAILED: " + e.ToString());
                    return false;
                }
            }
            return true;
        }


        private bool InitializeProcessTourDataCacheForDay(DateTime currentDay, string initializationInitiator)
        {
            if (m_TrackingEngineLogComputations)
            {
                InitializeTrackingEngineLogFileNames(DateTime.UtcNow, initializationInitiator);
            }

            //////////////////////////////////////////// TEST DATA ////////////////////////////////////////////////////////////////////////////////
            // TODO cicca
            //var trackingData = TestVehicleData.Test_LoadVehicleTrackingRecordListFromInterval(0, TestVehicleData.TestVehicleDataStr.Length);
            //var trackingData = TestVehicleData.Test_LoadVehicleTrackingRecordListFromInterval(TestVehicleData.TestVehicleDataStr2, 0,
            //     TestVehicleData.TestVehicleDataStr2.Length);

            //EZ KELL teszthez
            //var tourData = TestTourData.Test_LoadTourData2();
            //var trackingData = m_dbManager.LoadVehicleTrackingRecordList(
            //    new DateTime(currentDay.Year, currentDay.Month, 5, 0, 0, 0, DateTimeKind.Utc),
            //    new DateTime(currentDay.Year, currentDay.Month, 5, 23, 59, 59, DateTimeKind.Utc));
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            /*->*/
            List<VehicleTrackingRecord> trackingData = m_dbManager.LoadVehicleTrackingRecordList(
                new DateTime(currentDay.Year, currentDay.Month, currentDay.Day, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(currentDay.Year, currentDay.Month, currentDay.Day, 23, 59, 59, DateTimeKind.Utc));
            List<PMTour> tourData = m_bllWTT.RetrieveFilteredCachedList(currentDay);
            /**/
            if (tourData.Count == 0)
            {
                return false;
            }

            List<PMTour> preprocessedTourData = PreprocessTourData(tourData);

            ProcessedTourDataCache = ComputeTourCompletion(preprocessedTourData, trackingData);

            return true;
        }

        public class UpdatedTrackingData
        {
            public List<PMTour> UpdatedTourData { get; set; }
            public List<VehiclePositionData> UpdatedVehicleData { get; set; }
        }

        public UpdatedTrackingData UpdateTrackingData(VehicleTrackingRecord vtr)
        {
            if (DateTime.UtcNow.Date.CompareTo(m_currentDateOfProcessing) > 0)
            {
                ProcessedTourDataCache = null;
                m_currentDateOfProcessing = DateTime.UtcNow.Date;
                m_processTourDataCacheInitialized = InitializeProcessTourDataCacheForDay(m_currentDateOfProcessing,
                    "UpdateTrackingData_(DateTime.UtcNow greather than m_currentDateOfProcessing): ");


                m_dbManager.DeleteVehicleTrackingDataRecord(DateTime.UtcNow.Date.AddDays(-5));
            }

            if (!m_processTourDataCacheInitialized)
            {
                m_processTourDataCacheInitialized = InitializeProcessTourDataCacheForDay(m_currentDateOfProcessing,
                    "UpdateTrackingData_(!m_processTourDataCacheInitialized)");
                return null;
            }

            if (ProcessedTourDataCache == null)
            {
                return null;
            }


            //            var updatedTourData = UpdateTourData(ProcessedTourDataCache, vtr);
            List<PMTour> updatedTourData = ComputeTourCompletion(ProcessedTourDataCache, new[] { vtr }.ToList());
            /*
                        var xPEB735 = updatedTourData.Where(w => w.TruckRegNo.StartsWith("PEB-735")).FirstOrDefault();
                        if (xPEB735 != null)
                            LogTourCompletionProgress(xPEB735.TourPoints, xPEB735.TruckRegNo);
            */
            ProcessedTourDataCache = updatedTourData;

            return new UpdatedTrackingData
            {
                UpdatedTourData = ProcessedTourDataCache,
                UpdatedVehicleData = vtr.TrackingData
            };
        }


        private List<PMTour> PreprocessTourData(List<PMTour> tourData)
        {
            foreach (PMTour td in tourData)
            {
                foreach (PMTourPoint tp in td.TourPoints)
                {
                    tp.ParsedPosition = JsonConvert.DeserializeObject<PMMapPoint>(tp.Position);
                }
            }
            return tourData;
        }

        private Dictionary<string, List<VehiclePositionData>> GetVehicleList(List<VehicleTrackingRecord> trackingRecords)
        {
            Dictionary<string, List<VehiclePositionData>> vehicleList = new Dictionary<string, List<VehiclePositionData>>();
            foreach (VehicleTrackingRecord tr in trackingRecords)
            {
                foreach (VehiclePositionData td in tr.TrackingData)
                {
                    if (!vehicleList.ContainsKey(td.Device))
                    {
                        vehicleList.Add(td.Device, new List<VehiclePositionData>
                        {
                            td
                        });
                    }
                    else
                    {
                        vehicleList[td.Device].Add(td);
                    }
                }
            }
            return vehicleList;
        }

        private List<PMTour> ComputeTourCompletion(List<PMTour> tourData, List<VehicleTrackingRecord> trackingRecords)
        {
            Dictionary<string, List<VehiclePositionData>> vehicleList = GetVehicleList(trackingRecords);
            foreach (PMTour t in tourData)
            {
                if (vehicleList.ContainsKey(t.TruckRegNo))
                {
                    {
                        List<VehiclePositionData> vehicleData = vehicleList[t.TruckRegNo];
                        ProcessTour(t, vehicleData);
                    }
                }
            }
            return tourData;
        }

        /// <summary>
        /// Process tour completion for tour given in parameter
        /// </summary>
        /// <param name="tour">tour to operate on</param>
        /// <param name="vehicleData">tracking data to compute from</param>
        private void ProcessTour(PMTour tour, List<VehiclePositionData> vehicleData)
        {
            if (tour.TourPoints.Count < 3)
            {
                LogComputationError("tour.TourPoints.Count < 3 for tour (" + tour.ID + ")!");
                return;
            }
            ProcessTourPointList(tour.TourPoints, vehicleData, new ComputeParameters
            {
                TourColor = tour.TourColor,
                TourID = tour.ID,
                TruckRegNo = tour.TruckRegNo
            });
        }

        /// <summary>
        /// Process tour completion for given tour point list, vehicle tracking data and compute parameters
        /// </summary>
        /// <param name="tpList">tour point list to operate on</param>
        /// <param name="vehicleData">vehicle tracking data to compute completion from</param>
        /// <param name="cp">computing parameters</param>
        private void ProcessTourPointList(List<PMTourPoint> tpList, List<VehiclePositionData> vehicleData, ComputeParameters cp)
        {
            DateTime compStart = DateTime.UtcNow;


            tpList = tpList.OrderBy(o => o.Order).ToList();

            if (m_TrackingEngineLogComputations)
            {
                LogTourCompletionProgress(tpList, cp.TruckRegNo);
            }

            for (int vi = 0; vi < vehicleData.Count; vi++)
            {
                ProcessTrackingRecord(tpList, vehicleData[vi], cp, (vi == vehicleData.Count - 1));
            }

            if (m_TrackingEngineLogComputations)
            {
                LogTourCompletionProgress(tpList, cp.TruckRegNo);
            }
            DateTime compEnd = DateTime.UtcNow;
        }

        private void ProcessTrackingRecord(List<PMTourPoint> tpList, VehiclePositionData vd, ComputeParameters cp, bool predict)
        {

            //           if (vd.Device == "KVS-483")
            //                Console.WriteLine("dd2");

            if (tpList[0].TpStatus == PMTourPoint.enTpStatuses.White)
                tpList[0].TpStatus = PMTourPoint.enTpStatuses.Blue; // 0. statusza alapbol kek(ha nem teljesített)


            vd.TourID = cp.TourID;
            vd.TourColor = cp.TourColor;
            //       if (vd.Device.StartsWith("9"))
            //           Console.WriteLine("c");
            // I. fazis ////////////////////////////////////////////////////////////////////////////////////////////////////


            //2.KÉK->ZÖLD: 
            PMTourPoint completed = ProcessCompleted(tpList, vd, cp);

            //3.FEHÉR->KÉK:Sorrendben első FEHÉR túrapont , amelyik KÉKíthető
            if (completed == null || completed != tpList.First())        //Ha a raktári kilépést zöldítettük, nem szabad
                ProcessNextUnderCompletion(tpList, vd, cp);             //kékíthető túrapontot keresni, mert akkor megtalálja 
                                                                        // a túra végét (és az nem jó nekünk)


            // II. fazis ////////////////////////////////////////////////////////////////////////////////////////////////////
            int latestCompletionTP = GetLatestCompletionTourPointIdx(tpList);
            int nextTPToComplete = latestCompletionTP >= 0 ? latestCompletionTP + 1 : -1;

            for (int i = 0; i < tpList.Count; i++)
            {
                if (tpList[i].TpStatus == PMTourPoint.enTpStatuses.Yellow)
                {
                    if (i > 0)
                    {
                        DateTime calcTime = tpList[i - 1].RealDepTime.Add(tpList[i].ArrTime.Subtract(tpList[i - 1].DepTime));
                        setTourPointTimes(tpList[i], calcTime);
                    }
                }

                if (predict)
                {
                    if (tpList[i].TpStatus == PMTourPoint.enTpStatuses.White && tpList[0].TpStatus == PMTourPoint.enTpStatuses.Green)   //Érkezési idől számítása csak a raktári kilépések után
                    {
                        if (i == nextTPToComplete)
                        {

                            if (tpList[latestCompletionTP].TpStatus == PMTourPoint.enTpStatuses.Blue)
                            {
                                //Teljesítés közben vagyunk-e?
                                //Ha a legutolsó teljesítés KÉK, akkor az érkezés a KÉK pont depTime + két pont közötti menetidő

                                DateTime calcTime = tpList[latestCompletionTP].RealDepTime.Add(tpList[nextTPToComplete].ArrTime.Subtract(tpList[latestCompletionTP].DepTime));
                                setTourPointTimes(tpList[i], calcTime);

                            }
                            else
                            {
                                //Teljesítésen kívül vagyunk
                                MapDistance pmDistance = ComputeDistance(new PMMapPoint { Lat = vd.Latitude, Lng = vd.Longitude },
                                    new PMMapPoint { Lat = tpList[i].ParsedPosition.Lat, Lng = tpList[i].ParsedPosition.Lng }, cp);
                                if (pmDistance != null && pmDistance.Distance != 0)
                                {
                                    DateTime calcTime = vd.Time.AddMinutes(pmDistance.Duration);
                                    setTourPointTimes(tpList[i], calcTime);
                                }

                            }
                        }

                        else
                        {
                            //Következő utáni túrapont becslése az előző depTime és a menetidő alapján számolódik
                            DateTime calcTime = tpList[i - 1].PredictedDepTime.Add(tpList[i].ArrTime.Subtract(tpList[i - 1].DepTime));
                            setTourPointTimes(tpList[i], calcTime);
                        }
                    }

                }
            }


            if (predict)
            {
                vd.PreviousTPCompletion = tpList[latestCompletionTP].RealDepTime;
                vd.TourStart = tpList.First().RealDepTime;
                vd.Delay = new TimeSpan();
                vd.IsDedayed = false;
                if (nextTPToComplete > 0 && nextTPToComplete < tpList.Count())
                {
                    vd.PredictedNextTPCompletion = tpList[nextTPToComplete].TpStatus == PMTourPoint.enTpStatuses.White ? tpList[nextTPToComplete].PredictedArrTime : tpList[nextTPToComplete].RealArrTime;
                    vd.Delay = vd.PredictedNextTPCompletion.Subtract(tpList[nextTPToComplete].ArrTime);
                    if (vd.Delay.Minutes < 0)
                    {
                        vd.Delay = new TimeSpan();
                    }
                    else
                    {
                        vd.IsDedayed = true;
                    }
                }
            }
        }



        private int GetLatestCompletionTourPointIdx(List<PMTourPoint> tpList)
        {
            for (int i = tpList.Count - 1; i > -1; i--)
            {
                if (tpList[i].TpStatus == PMTourPoint.enTpStatuses.Blue || tpList[i].TpStatus == PMTourPoint.enTpStatuses.Green)
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary>
        /// KÉK->ZÖLD
        /// </summary>
        /// <param name="tpList"></param>
        /// <param name="vd"></param>
        /// <param name="cp"></param>
        /// <returns></returns>
        private PMTourPoint ProcessCompleted(List<PMTourPoint> tpList, VehiclePositionData vd, ComputeParameters cp)
        {

            PMTourPoint tpCompleting = tpList.Where(w => w.TpStatus == PMTourPoint.enTpStatuses.Blue).FirstOrDefault();
            if (tpCompleting != null)
            {

                double distance = ComputeFastDistanceInKm(tpCompleting.ParsedPosition, new PMMapPoint
                {
                    Lat = vd.Latitude,
                    Lng = vd.Longitude
                });

                if (distance > m_epsilonTourPointCompletedFastInKm)
                {
                    tpCompleting.TpStatus = PMTourPoint.enTpStatuses.Green;

                    MapDistance pmDistance = ComputeDistance(
                        new PMMapPoint { Lat = tpCompleting.ParsedPosition.Lat, Lng = tpCompleting.ParsedPosition.Lng },
                        new PMMapPoint { Lat = vd.Latitude, Lng = vd.Longitude }, cp);


                    if (tpCompleting == tpList.First())
                    {
                        //Raktári indulás (túrakezdés) 

                        if (pmDistance != null && pmDistance.Distance > 0)
                        {
                            // A trackingdata időpontja és a menetidő alapján visszaszámoljuk az indulást
                            //
                            tpCompleting.RealDepTime = vd.Time.AddMinutes(-1 * pmDistance.Duration);
                            tpCompleting.RealServTime = tpCompleting.RealDepTime.Subtract(tpCompleting.DepTime.Subtract(tpCompleting.ServTime));
                            tpCompleting.RealArrTime = tpCompleting.RealServTime;
                        }
                        else
                        {

                            // ha nem találtunk távolságadatot, akkor jobb híjján a trackingdata időpontja 
                            // a raktári távozás
                            //
                            tpCompleting.RealArrTime = vd.Time.Subtract(tpCompleting.DepTime.Subtract(tpCompleting.ServTime));
                            tpCompleting.RealServTime = tpCompleting.RealArrTime;
                            tpCompleting.RealDepTime = vd.Time;
                            //TODO: logolni !!!

                        }

                    }
                    else
                    {
                        if (pmDistance != null && pmDistance.Distance > 0)
                        {
                            //menetidő alapján visszaszámoljuk a tényleges távozást
                            //
                            tpCompleting.RealDepTime = vd.Time.AddMinutes(-1 * pmDistance.Duration);
                        }
                        else
                        {
                            //Ha nics menetidőadat és nem mentünk nagyon messzo a túraponttól 
                            //(Y*2-n belül vagyunk), akkor a távozás ideje a tracking data-val egyezik meg.
                            if (distance < m_epsilonTourPointCompletedFastInKm * 2)
                                tpCompleting.RealDepTime = vd.Time;

                            //TODO: logolni !!!
                        }

                        //Kiszolgálási idők igazítása (ha kell)
                        if (tpCompleting.RealServTime.CompareTo(tpCompleting.RealDepTime) > 0)
                        {
                            tpCompleting.RealServTime = tpCompleting.RealDepTime.Subtract(tpCompleting.DepTime.Subtract(tpCompleting.ServTime));
                            if (tpCompleting.RealServTime.CompareTo(tpCompleting.RealArrTime) < 0)
                            {
                                tpCompleting.RealServTime = tpCompleting.RealArrTime;
                            }
                        }

                    }

                    //        tpCompleting.RealDepTime = vd.Time;
                    if (m_TrackingEngineLogComputations)
                    {
                        LogComputationToFile(tpCompleting, vd, cp, distance, (distance < m_epsilonTourPointCompletedFastInKm * 2));
                    }

                    return tpCompleting;
                }
                return null;
            }
            return null;
        }


        /// <summary>
        /// Sorrendben első FEHÉR túrapont, amelyik KÉKíthető 
        /// Feltétel: 
        ///     - Y távolságra megközelítettünk egy pontot?
        ///     - Ha raktári érkezést (utolsó tp) közelítettül meg, akkor a rakátri távozás (első tp) ZÖLD?
        /// </summary>
        /// <param name="tpList"></param>
        /// <returns></returns>
        private PMTourPoint ProcessNextUnderCompletion(List<PMTourPoint> tpList, VehiclePositionData vd, ComputeParameters cp)
        {

            List<int> pointIdxList = new List<int>();
            foreach (PMTourPoint tp in tpList)
            {
                if (tp.TpStatus == PMTourPoint.enTpStatuses.White || tp.TpStatus == PMTourPoint.enTpStatuses.Yellow)
                {

                    double distance = ComputeFastDistanceInKm(tp.ParsedPosition, new PMMapPoint
                    {
                        Lat = vd.Latitude,
                        Lng = vd.Longitude
                    });


                    if (distance <= m_epsilonTourPointCompletedFastInKm)             //Y távolságra megközelítettünk egy pontot?
                    {
                        bool bValidPt = false;
                        if (tp == tpList.Last())              //Ha raktári érkezést (utolsó tp) közelítettük meg, akkor a rakári távozás (első tp) ZÖLD?
                        {
                            if (tpList.First().TpStatus == PMTourPoint.enTpStatuses.Green)  //Teljesült a rakátri kilépés?
                            {
                                tp.TpStatus = PMTourPoint.enTpStatuses.Green;
                                tp.RealArrTime = vd.Time;
                                tp.RealServTime = vd.Time;
                                tp.RealDepTime = vd.Time;
                                bValidPt = true;
                            }
                        }
                        else
                        {
                            //2018.04.24, Papp Gábor:
                            // A pakolásaink sajnos hosszúak emiatt leállítják a motort, így ezzel nem lenne gond. Ettől függetlenül ki 
                            // tudom adni az összes gépjárművezetőnek, hogy minden túraponthoz történő megérkezés után legalább 6 percre 
                            //vegye le a gyújtást.



                            if (!tpList.Any(a => a.TpStatus == PMTourPoint.enTpStatuses.Blue) && vd.Ignition.ToUpper() == "OFF")
                            {
                                tp.TpStatus = PMTourPoint.enTpStatuses.Blue;
                                setTourPointTimes(tp, vd.Time);
                                bValidPt = true;
                            }
                            else
                            {
                                //                  tp.TpStatus = PMTourPoint.enTpStatuses.Yellow;
                            }
                        }

                        if (bValidPt)
                        {
                            tpList.ForEach(item =>
                            {
                                //Ha van a pont előtt Kék túrapont, akkor azt ZÖLDítsük ki. (mivel adathiány miatt pontatlan a valós servTime megállapítása, az ilyen eseményt célszerű logolni)
                                if (item.TpStatus == PMTourPoint.enTpStatuses.Blue && item.Order < tp.Order)
                                {
                                    item.TpStatus = PMTourPoint.enTpStatuses.Green;
                                }

                                //Ha van a pont előtt FEHÉR túrapont, akkor azt Sárgítsuk
                                if (item.TpStatus == PMTourPoint.enTpStatuses.White && item.Order < tp.Order)
                                {
                                    item.TpStatus = PMTourPoint.enTpStatuses.Yellow;
                                }

                                //A pont utáni túrapontokat FEHÉRítsük ki (mert sorrendben teljesítünk)
                                if (item.Order > tp.Order)
                                {
                                    item.TpStatus = PMTourPoint.enTpStatuses.White;
                                }

                            });

                            return tp;
                        }
                        return null;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Túraidők kiszámolása
        /// </summary>
        /// <param name="p_tp"></param>
        /// <param name="p_dt"></param>
        private void setTourPointTimes(PMTourPoint p_tp, DateTime p_dt)
        {
            DateTime xArrTime = p_dt;
            DateTime xServTime = p_dt;
            DateTime xDepTime = p_dt;

            if (p_tp.ArrTime.CompareTo(p_tp.ServTime) < 0)          //A tervezett ArrTime és SrvTime különbségéből látjuk a korábbi érkezést!!
            {
                // terv szerint nyitas elott erkezik a jarmu
                if (xArrTime.CompareTo(p_tp.ServTime) < 0)
                {
                    // valoban is korabban erkezik
                    xServTime = p_tp.ServTime;
                    xDepTime = p_tp.DepTime;
                }
                else
                {
                    xServTime = xArrTime;
                    xDepTime = xArrTime.Add(p_tp.DepTime.Subtract(p_tp.ServTime));
                }
            }
            else
            {
                // terv szerint nyitas utánra erkezik a jarmu
                xServTime = xArrTime;               //A 
                xDepTime = xArrTime.Add(p_tp.DepTime.Subtract(p_tp.ServTime));
            }

            if (p_tp.UnderCompletion || p_tp.Uncertain)
            {
                p_tp.RealArrTime = xArrTime;
                p_tp.RealServTime = xServTime;
                p_tp.RealDepTime = xDepTime;

                //Ha a nyitva tartás miatt az SRV time későbbi, mint az indulás, a valós idők alpján
                //

                if (p_tp.RealServTime.CompareTo(p_tp.RealDepTime) > 0)
                {
                    p_tp.RealServTime = p_tp.RealDepTime.Subtract(p_tp.DepTime.Subtract(p_tp.ServTime));
                    if (p_tp.RealServTime.CompareTo(p_tp.RealArrTime) < 0)
                    {
                        p_tp.RealServTime = p_tp.RealArrTime;
                    }
                }
            }

            //Predicted időket minden esetben töltjük
            p_tp.PredictedArrTime = xArrTime;
            p_tp.PredictedServTime = xServTime;
            p_tp.PredictedDepTime = xDepTime;


            if (p_tp.PredictedServTime.CompareTo(p_tp.PredictedDepTime) > 0)
            {
                p_tp.PredictedServTime = p_tp.PredictedDepTime.Subtract(p_tp.DepTime.Subtract(p_tp.ServTime));
                if (p_tp.PredictedServTime.CompareTo(p_tp.PredictedArrTime) < 0)
                {
                    p_tp.PredictedServTime = p_tp.PredictedArrTime;
                }
            }

        }


        private List<int> GetUnderCompletionTourPoints(List<PMTourPoint> tpList)
        {
            List<int> idxList = new List<int>();
            for (int i = 0; i < tpList.Count; i++)
            {
                if (tpList[i].TpStatus == PMTourPoint.enTpStatuses.Blue)
                {
                    idxList.Add(i);
                }
            }
            return idxList;
        }


        private void LogComputationError(string message)
        {
            using (FileStream logFile = new FileStream(m_ComputationLogFilePath, FileMode.Append))
            {
                using (StreamWriter logStream = new StreamWriter(logFile))
                {
                    logStream.WriteLine("[" + DateTime.UtcNow.ToString() + "][ERROR]: " + message);
                }
            }
        }

        private void LogComputationToFile(PMTourPoint tp, VehiclePositionData vd, ComputeParameters cp, double distance, bool completionByPos = false)
        {

            using (FileStream logFile = new FileStream(m_ComputationLogFilePath, FileMode.Append))
            {
                using (StreamWriter logStream = new StreamWriter(logFile))
                {
                    logStream.WriteLine("[" + DateTime.UtcNow.ToString() + "]: " + "Completed" + (completionByPos ? "(!)" : "") +
                        ",truck " + cp.TruckRegNo + ",order: " + tp.Order + ",TourID: " + tp.TourID +
                        ",vd.Time: " + vd.Time + ",ArrTime: " + tp.ArrTime + ",ServTime: " + tp.ServTime + ",DepTime: " + tp.DepTime +
                        ",DISTANCE: " + distance + ",RealArrTime: " + tp.RealArrTime + ",RealServTime: " + tp.RealServTime +
                        ",RealDepTime: " + tp.RealDepTime + ",device: " + vd.Device);
                }
            }
        }

        /// <summary>
        /// Log the state of the TPlist completion given as parameter to file.
        /// </summary>
        /// <param name="tpList"></param>
        private void LogTourCompletionProgress(List<PMTourPoint> tpList, string RegNo = "")
        {
            using (FileStream logFile = new FileStream(m_TPCompletionLogFilePath, FileMode.Append))
            {
                using (StreamWriter logStream = new StreamWriter(logFile))
                {
                    logStream.WriteLine("==========================================================================================");
                    logStream.WriteLine("[" + DateTime.UtcNow.ToString() + (!string.IsNullOrEmpty(RegNo) ? ", RegNo:" + RegNo + " " : "") + "]");

                    foreach (PMTourPoint tp in tpList)
                    {
                        logStream.WriteLine("TourID: " + tp.TourID + ", tp.Order: " + tp.Order + ", position: " + tp.ParsedPosition.Lat.ToString() + "," + tp.ParsedPosition.Lng.ToString());
                        logStream.WriteLine("Status: " + (tp.Completed ? "COMPLETED" : tp.Uncertain ? "UNCERTAIN"
                            : tp.UnderCompletion ? "UNDER COMPLETION" : "UNCOMPLETED"));
                        logStream.WriteLine("ArrTime: " + tp.ArrTime);
                        logStream.WriteLine("ServTime: " + tp.ServTime);
                        logStream.WriteLine("DepTime: " + tp.DepTime);
                        if (tp.TpStatus == PMTourPoint.enTpStatuses.White)
                        {
                            logStream.WriteLine("PredictedArrTime: " + tp.PredictedArrTime);
                            logStream.WriteLine("PredictedServTime: " + tp.PredictedServTime);
                            logStream.WriteLine("PredictedDepTime: " + tp.PredictedDepTime);
                        }
                        else
                        {
                            logStream.WriteLine("RealArrTime: " + tp.RealArrTime);
                            logStream.WriteLine("RealServTime: " + tp.RealServTime);
                            logStream.WriteLine("RealDepTime: " + tp.RealDepTime);
                        }
                        logStream.WriteLine("------------------------------------------------------------------------------------------");
                    }
                }
            }
        }
        private MapDistance ComputeDistance(PMMapPoint source, PMMapPoint destination, ComputeParameters cdparams)
        {
            MapDistance retDist = new MapDistance();

            bool computeSuccess = PMRoute.RouteFuncs.GetDistance(m_PMTourMapIniDirPath, "DB0", m_PMTourMapDirPath,
                source.Lat, source.Lng, destination.Lat, destination.Lng, cdparams.RZNIdList,
                cdparams.Weight, cdparams.Height, cdparams.Width, out int dist, out int dur);

            if (!computeSuccess)
            {
                return null;
                //throw new Exception("ERROR while computing point-to-point distance with PMRoute.RouteFuncs.GetDistance.");
            }

            return new MapDistance
            {
                Distance = dist,
                Duration = dur
            };
        }

        private double ComputeFastDistanceInKm(PMMapPoint source, PMMapPoint destination)
        {
            int R = 6371; // Radius of the earth in km
            double dLat = Deg2rad(destination.Lat - source.Lat);  // deg2rad below
            double dLon = Deg2rad(destination.Lng - source.Lng);
            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Deg2rad(source.Lat)) * Math.Cos(Deg2rad(destination.Lat)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double d = R * c; // Distance in km
            return d;
        }

        private double Deg2rad(double deg)
        {
            return deg * (Math.PI / 180);
        }

        public bool SetManualTrackingData(DateTime TourStartLocalTime, DateTime EventTimestampLocalTime, string Device, double Latitude, double Longitude)
        {


            //looking for tourpoint 
            PMTourPoint currTp = null;
            using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
            {
                List<PMTour> vehicleTour = TrackingEngine.Instance.ProcessedTourDataCache.Where(w => w.TruckRegNo == Device).ToList();
                if (vehicleTour != null)
                {
                    vehicleTour = PreprocessTourData(vehicleTour);
                    foreach (PMTour tour in vehicleTour)
                    {
                        currTp = tour.TourPoints.Where(w => Math.Round(w.ParsedPosition.Lat, 3) == Math.Round(Latitude, 3) &&
                                                               Math.Round(w.ParsedPosition.Lng, 3) == Math.Round(Longitude, 3)).FirstOrDefault();
                    }
               }
            }


            VehiclePositionData trackingData = new VehiclePositionData()
            {
                TourStart = TourStartLocalTime.ToUniversalTime(),
                Time = EventTimestampLocalTime.ToUniversalTime(),
                Device = Device,
                Latitude = Latitude,
                Longitude = Longitude,
                Ignition = "OFF",
                IsDedayed = false,
                Distance = 0
            };

            refreshManualTrackingData(trackingData);
            
            /*************
            var TimestampLocalTimeDeparture = EventTimestampLocalTime;
            if (currTp != null)
                TimestampLocalTimeDeparture = TimestampLocalTimeDeparture.Add(currTp.DepTime.Subtract(currTp.ServTime));
            else
                TimestampLocalTimeDeparture = TimestampLocalTimeDeparture.Add(new TimeSpan(0,15,0));


            VehiclePositionData trackingDataDeparture = new VehiclePositionData()
            {
                TourStart = TourStartLocalTime.ToUniversalTime(),
                Time = TimestampLocalTimeDeparture.ToUniversalTime(),
                Device = Device,
                Latitude = Latitude - 0.00001,
                Longitude = Longitude - 0.00001,
                Ignition = "ON",
                IsDedayed = false,
                Distance = 0
            };
            refreshManualTrackingData(trackingDataDeparture);
            */
            return true;
        }

        private void refreshManualTrackingData(VehiclePositionData trackingData)
        {

            manualTrackingDataLog("trackingData=" + JsonConvert.SerializeObject(trackingData));
            var vtr = new VehicleTrackingRecord
            {
                Timestamp = trackingData.Time,
                TrackingData = new List<VehiclePositionData>()
            };
            vtr.TrackingData.Add(trackingData);

            DBManager dbManager = new DBManager();
            dbManager.StoreVehicleTrackingDataRecord_manual(trackingData.Time, trackingData);


            List<PMTour> updatedTourData = ComputeTourCompletion(ProcessedTourDataCache, new List<VehicleTrackingRecord> { vtr });
            ProcessedTourDataCache = updatedTourData;

        }

        private void manualTrackingDataLog(string p_txt)
        {
            var logFilePath = m_trackingEngineLogDirPath + "\\" + "SetManualTrackingData.log";
            using (var logStream = new StreamWriter(logFilePath, true))
            {
                logStream.WriteLine(p_txt);
            }
        }


        private void generateTestData(PMTour t, List<VehiclePositionData> vehicleData)
        {
            VehiclePositionData vdProto = vehicleData.First();
            vehicleData.Clear();

            //0. raktáron állunk tesztadat
            PMTourPoint whsOut = t.TourPoints.First();
            VehiclePositionData vt = vdProto.ShallowCopy();
            vt.Time = whsOut.DepTime.AddMinutes(-10);
            vt.Latitude = whsOut.ParsedPosition.Lat;
            vt.Longitude = whsOut.ParsedPosition.Lng;
            vt.Device = "0. raktáron állunk tesztadat";
            vehicleData.Add(vt);

            //1. raktárt elhagytuk tesztadat
            vt = vdProto.ShallowCopy();
            vt.Time = whsOut.DepTime.AddMinutes(5);
            vt.Latitude = 47.126114;
            vt.Longitude = 18.363248;
            vt.Device = "1. raktárt elhagytuk tesztadat";
            vehicleData.Add(vt);

            PMTourPoint ptReach = t.TourPoints[3];
            //2. 4. túrapont melletti elhaladás
            vt = vdProto.ShallowCopy();
            vt.Time = ptReach.DepTime.AddMinutes(5);
            vt.Latitude = ptReach.ParsedPosition.Lat;
            vt.Longitude = ptReach.ParsedPosition.Lng;
            vt.Device = "2. 4. túrapont melletti elhaladás";
            vehicleData.Add(vt);

            PMTourPoint pt = t.TourPoints[2];
            //3. 2. túrapont teljesítés kezdete
            vt = vdProto.ShallowCopy();
            vt.Time = pt.DepTime.AddMinutes(5);
            vt.Latitude = pt.ParsedPosition.Lat;
            vt.Longitude = pt.ParsedPosition.Lng;
            vt.Device = "3. 2. túrapont teljesítés kezdete";
            vehicleData.Add(vt);

            // Latitude: 1 deg = 110.574 km
            // Longitude: 1 deg = 111.320*cos(latitude) km

            pt = t.TourPoints[2];
            //4. 2. túrapont teljesítés vége koordináta alapján 2 Y-on belül vagyunk (1 fok kb 111 km)
            vt = vdProto.ShallowCopy();
            vt.Time = pt.DepTime.AddMinutes(35);
            vt.Latitude = pt.ParsedPosition.Lat + (1.0 / 111 * m_epsilonTourPointCompletedFastInKm * 1.3);
            vt.Longitude = pt.ParsedPosition.Lng + (1.0 / 111 * m_epsilonTourPointCompletedFastInKm * 1.3);
            vt.Device = "4. 2. túrapont teljesítés vége koordináta alapján  (1 fok kb 111 km)";
            vehicleData.Add(vt);

            pt = t.TourPoints[3];
            //5. 3. túrapont teljesítés előrejelzése
            vt = vdProto.ShallowCopy();
            vt.Time = pt.DepTime.AddMinutes(35);
            vt.Latitude = pt.ParsedPosition.Lat - (1.0 / 111 * 10);
            vt.Longitude = pt.ParsedPosition.Lng - (1.0 / 111 * 10);
            vt.Device = "5. 3. túrapont teljesítés előrejelzése";
            vehicleData.Add(vt);

            pt = t.TourPoints[3];
            //6. 3. túrapont teljesítés kezdete Y-on belüli érkezés 
            vt = vdProto.ShallowCopy();
            vt.Time = pt.DepTime.AddMinutes(5);
            vt.Latitude = pt.ParsedPosition.Lat - (1.0 / 111);
            vt.Longitude = pt.ParsedPosition.Lng - (1.0 / 111);
            vt.Device = "6. 3. túrapont teljesítés kezdete, Y-on belüli érkezés";
            vehicleData.Add(vt);

            pt = t.TourPoints[3];
            //7. 3. túrapont teljesítés vége 2 Y-on kivüli
            vt = vdProto.ShallowCopy();
            vt.Time = pt.DepTime.AddMinutes(5);
            vt.Latitude = pt.ParsedPosition.Lat - (1.0 / 111 * 10);
            vt.Longitude = pt.ParsedPosition.Lng - (1.0 / 111 * 10);
            vt.Device = "7. 3. túrapont teljesítés vége 2 Y-on kivüli";
            vehicleData.Add(vt);


            PMTourPoint ptReach2 = t.TourPoints[6];
            //8. 6. túrapont melletti elhaladás
            vt = vdProto.ShallowCopy();
            vt.Time = ptReach2.DepTime.AddMinutes(5);
            vt.Latitude = ptReach2.ParsedPosition.Lat;
            vt.Longitude = ptReach2.ParsedPosition.Lng;
            vt.Device = "8. 6. túrapont melletti elhaladás";
            vehicleData.Add(vt);

            PMTourPoint ptWhs = t.TourPoints.Last();

            //9. raktári visszaérkezés, 6. túrapont teljesítéssel
            vt = vdProto.ShallowCopy();
            vt.Time = ptWhs.DepTime.AddMinutes(5);
            vt.Latitude = ptWhs.ParsedPosition.Lat;
            vt.Longitude = ptWhs.ParsedPosition.Lng;
            vt.Device = "9. raktári visszaérkezés, 6. túrapont teljesítéssel";
            vehicleData.Add(vt);

        }

    }
}
