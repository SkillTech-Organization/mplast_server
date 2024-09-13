using Newtonsoft.Json;
using System;

namespace MPWeb.Logic.BLL
{
    public class VehiclePositionData
    {
        [JsonProperty("tourID")]
        public string TourID { get; set; }
        [JsonProperty("tourStart")]
        public DateTime TourStart { get; set; }
        [JsonProperty("tourColor")]
        public string TourColor { get; set; }
        [JsonProperty("device")]
        public string Device { get; set; }
        [JsonProperty("time")]
        public DateTime Time { get; set; }
        [JsonProperty("latitude")]
        public double Latitude { get; set; }
        [JsonProperty("longitude")]
        public double Longitude { get; set; }
        [JsonProperty("direction")]
        public int Direction { get; set; }
        [JsonProperty("ignition")]
        public string Ignition { get; set; }
        [JsonProperty("speed")]
        public double Speed { get; set; }
        [JsonProperty("odometer")]
        public double Odometer { get; set; }
        [JsonProperty("isDelayed")]
        public bool IsDedayed { get; set; }
        [JsonProperty("delay")]
        public TimeSpan Delay { get; set; }
        [JsonProperty("distance")]
        public int Distance { get; set; }
        [JsonProperty("previousTPCompletion")]
        public DateTime PreviousTPCompletion { get; set; }
        [JsonProperty("predictedNextTPCompletion")]
        public DateTime PredictedNextTPCompletion { get; set; }


        public VehiclePositionData ShallowCopy()
        {
            return (VehiclePositionData)this.MemberwiseClone();
        }
    }
}
