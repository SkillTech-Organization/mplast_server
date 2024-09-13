using Microsoft.Owin;
using MPWeb.Caching;
using MPWeb.Logic.BLL.TrackingEngine;
using Owin;

[assembly: OwinStartup(typeof(MPWeb.Startup))]
namespace MPWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // preload tourCaching instance to start caching thread
            var tourCachingLoader = TourCaching.Instance;
            var trackingEngine = TrackingEngine.Instance;
            var vehicleTrackingLoager = VehicleTrackingDataCaching.Instance;
        }
    }
}
