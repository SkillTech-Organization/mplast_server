using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using AzureTableStore;
using MPWeb.Common.Attrib;

namespace MPWeb.Logic.Tables
{
    [Serializable]
    [DataContract(Namespace = "Tour")]
    public class PMTour : AzureTableObjBase
    {
        public const string PartitonConst = "TOUR";

        [DataMember]
        [AzureTablePartitionKeyAttr]
        public string PartitionKey { get; set; } = PartitonConst;

        private string m_ID { get; set; }
        [DataMember]
        [AzureTableRowKeyAttr]
        public string ID
        {
            get { return m_ID; }
            set
            {
                m_ID = value;
                NotifyPropertyChanged("ID");
            }
        }

        [DataMember]
        public string Carrier { get; set; }
        [DataMember]
        public string TruckRegNo { get; set; }
        [DataMember]
        public string RZN_ID_LIST { get; set; }
        [DataMember]
        public int TruckWeight { get; set; }
        [DataMember]
        public int TruckHeight { get; set; }
        [DataMember]
        public int TruckWidth { get; set; }
        [DataMember]
        public DateTime Start { get; set; }
        [DataMember]
        public DateTime End { get; set; }
        [DataMember]
        public double TourLength { get; set; }
        [DataMember]
        public double Qty { get; set; }
        [DataMember]
        public double Vol { get; set; }
        [DataMember]
        public double Toll { get; set; }

        [IgnoreDataMember]
        [JsonProperty("TourPointCnt")]
        public int TourPointCnt {
            get
            {
                return TourPoints.Count();
            }
        }          

        [DataMember]
        public string TourColor { get; set; }
        [DataMember]
        public string TruckColor { get; set; }
        [IgnoreDataMember]
        [JsonProperty("TourPoints")]
        public List<PMTourPoint> TourPoints { get; set; } = new List<PMTourPoint>();
    }
}
