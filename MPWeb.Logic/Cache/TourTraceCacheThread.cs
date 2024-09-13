using MPWeb.Common.Threading;
using MPWeb.Logic.BLL;
using MPWeb.Logic.Tables;
using System;
using System.Configuration;
using System.Threading;

namespace MPWeb.Logic.Cache
{
    public class TourTraceCacheThread : ThreadBase
    {
        private int m_tourTraceCacheRefreshIntervalSecs;
        public TourTraceCacheThread(ThreadPriority p_ThreadPriority) : base(p_ThreadPriority)
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["TourTraceCacheRefreshIntervalSecs"]))
            {
                throw new Exception("Parameter m_vehicleTrackingCacheRefreshIntervalSecs is not set.");
            }
            m_tourTraceCacheRefreshIntervalSecs = int.Parse(ConfigurationManager.AppSettings["TourTraceCacheRefreshIntervalSecs"]);
        }

        protected override void DoWork()
        {
            BllWebTraceTour bllWebTraceTour = new BllWebTraceTour(m_WorkingThread.Name);

            int Total;
            while (EventStop == null || !EventStop.WaitOne(0, true))
            {
                var lstFull = bllWebTraceTour.RetrieveList(out Total);
                using (LockForCache lockObj = new LockForCache(ToursCache.Locker))
                {
                    ToursCache.Instance.Items = new System.Collections.Concurrent.ConcurrentBag<PMTour>(lstFull);
                }
                Thread.Sleep(m_tourTraceCacheRefreshIntervalSecs * 1000);
            }

            if (EventStop != null && EventStop.WaitOne(0, true))
            {
                EventStopped.Set();
                return;
            }
        }

    }
}
