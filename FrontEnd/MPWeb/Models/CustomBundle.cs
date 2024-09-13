using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MPWeb.Models
{

    public class BundleInformation
    {
        public List<CustomBundle> Bundles { get; set; }
    }

    public class CustomBundle
    {
        public string BundleName { get; set; }
        public string Path { get; set; }
        public Boolean IsLoaded { get; set; }
    }
}