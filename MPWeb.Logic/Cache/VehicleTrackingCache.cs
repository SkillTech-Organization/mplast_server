using MPWeb.Logic.BLL;
using System;

namespace MPWeb.Logic.Cache
{
    public class VehicleTrackingCache
    {
        public static object Locker = new object();
        public System.Collections.Concurrent.ConcurrentBag<VehiclePositionData> CachedPositionData = null;
        private static readonly Lazy<VehicleTrackingCache> m_instance = new Lazy<VehicleTrackingCache>( () => new VehicleTrackingCache(), true);

        static public VehicleTrackingCache Instance
        {
            get
            {
                return m_instance.Value;
            }
        }

        private VehicleTrackingCache()
        {
            CachedPositionData = new System.Collections.Concurrent.ConcurrentBag<VehiclePositionData>();
        }
    }
}
