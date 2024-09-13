using MPWeb.Logic.Tables;
using System;

namespace MPWeb.Logic.Cache
{
    public class ToursCache
    {
        public static object Locker = new object();
        public System.Collections.Concurrent.ConcurrentBag<PMTour> Items = null;
        private static readonly Lazy<ToursCache> m_instance = new Lazy<ToursCache>(() => new ToursCache(), true);

        static public ToursCache Instance                   
        {
            get
            {
                return m_instance.Value;
            }
        }

        private ToursCache()
        {
             Items = new System.Collections.Concurrent.ConcurrentBag<PMTour>();
        }
    }
}
