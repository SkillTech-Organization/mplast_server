using MPWeb.Common.Threading;
using MPWeb.Logic.BLL;
using MPWeb.Logic.BLL.TrackingEngine;
using System;
using System.Collections.Concurrent;
using System.Configuration;
using System.Threading;

namespace MPWeb.Logic.Cache
{
    public class VehicleTrackingCacheThread : ThreadBase
    {
        private int m_vehicleTrackingCacheRefreshIntervalSecs;
        private DBManager m_dbManager = new DBManager();

        private int TEST_VTR_IDX = 800;
       
        public VehicleTrackingCacheThread(ThreadPriority p_ThreadPriority) : base(p_ThreadPriority)
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["VehicleTrackingCacheRefreshIntervalSecs"]))
            {
                throw new Exception("Parameter m_vehicleTrackingCacheRefreshIntervalSecs is not set.");
            }
            m_vehicleTrackingCacheRefreshIntervalSecs = int.Parse(ConfigurationManager.AppSettings["VehicleTrackingCacheRefreshIntervalSecs"]);
        }

        protected override void DoWork()
        {
            var bllWebVehicleTrace = new BllWebVehicleTrace();

            while (EventStop == null || !EventStop.WaitOne(0, true))
            {
                //////////////////////////////////////////// TEST DATA ////////////////////////////////////////////////////////////////////////////////
                // TODO cicca
                //var trackingData = TestVehicleData.TestTrackingRecords2[TEST_VTR_IDX++].TrackingData;
                ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                  
                var trackingData = bllWebVehicleTrace.GetVehicleTrackingInfo(); 
                m_dbManager.StoreVehicleTrackingDataRecord(DateTime.UtcNow, trackingData);
                
                var updatedTrackingData = TrackingEngine.Instance.UpdateTrackingData(new VehicleTrackingRecord
                {
                    Timestamp = DateTime.UtcNow,
                    TrackingData = trackingData
                });
                
                
                if (updatedTrackingData != null)
                {
                   using (LockForCache lockObj = new LockForCache(VehicleTrackingCache.Locker))
                    {
                        VehicleTrackingCache.Instance.CachedPositionData = new ConcurrentBag<VehiclePositionData>(updatedTrackingData.UpdatedVehicleData);
                    }
                }
                
                Thread.Sleep(m_vehicleTrackingCacheRefreshIntervalSecs * 1000);
            }

            if (EventStop != null && EventStop.WaitOne(0, true))
            {
                EventStopped.Set();
                return;
            }
        }
    }
}
