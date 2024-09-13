using MPWeb.Logic.Helpers;
using System.Collections.Generic;

namespace MPWeb.Models
{
    public class ServerUserContext
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int SessionTimeoutMinutes { get; set; }

        public List<string> Roles;

        public List<PMTracedTour> TourPointList { get; set; }
    }
}