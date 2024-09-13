using System;
using System.Collections.Generic;

namespace MPWeb.Logic.BLL.TrackingEngine
{
    public class VehicleTrackingRecord
    {
        public DateTime Timestamp { get; set; }
        public List<VehiclePositionData> TrackingData { get; set; }
    }
}
