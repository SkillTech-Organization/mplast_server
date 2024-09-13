using MPWeb.Logic.Cache;

namespace MPWeb.Caching
{
    public class VehicleTrackingDataCaching
    {
        private static VehicleTrackingDataCaching instance;
        private VehicleTrackingCacheThread m_ct = new VehicleTrackingCacheThread(System.Threading.ThreadPriority.Normal);

        private VehicleTrackingDataCaching()
        {
            m_ct.Run();
        }

        public static VehicleTrackingDataCaching Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VehicleTrackingDataCaching();
                }
                return instance;
            }
        }

        public void StopThread()
        {
            if( m_ct != null)
            {
                m_ct.Stop();
                m_ct = null;
            }
        }


        public void RestartThread()
        {
            StopThread();
            m_ct = new VehicleTrackingCacheThread(System.Threading.ThreadPriority.Normal);
            m_ct.Run();
        }
    }

}