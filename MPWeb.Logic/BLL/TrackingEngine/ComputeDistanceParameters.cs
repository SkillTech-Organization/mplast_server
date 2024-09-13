using System;

namespace MPWeb.Logic.BLL.TrackingEngine
{
    internal class ComputeParameters
    {
        public string TourID { get; set; }
        public string TruckRegNo { get; set; }
        public string TourColor { get; set; }
        public string RZNIdList { get; set; }
        public int Weight { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public DateTime TourStart { get; set; }
    }
}