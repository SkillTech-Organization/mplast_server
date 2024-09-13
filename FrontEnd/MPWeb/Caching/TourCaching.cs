using MPWeb.Logic.Cache;

namespace MPWeb.Caching
{
    public class TourCaching
    {
        private static TourCaching instance;
        private TourTraceCacheThread m_ct = new TourTraceCacheThread(System.Threading.ThreadPriority.Normal);

        private TourCaching() {
            m_ct.Run();
        }

        public static TourCaching Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TourCaching();
                }
                return instance;
            }
        }
    }
}