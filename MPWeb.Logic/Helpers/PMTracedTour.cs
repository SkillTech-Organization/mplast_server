using System;
using System.Runtime.Serialization;

namespace MPWeb.Logic.Helpers
{
    [Serializable]
    public class PMTracedTour
    {
        [DataMember]
        public int TourID { get; set; }


        [DataMember]
        public int Order { get; set; }
    }
}
