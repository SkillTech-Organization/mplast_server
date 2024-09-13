using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MPWeb.Common.Attrib;
using AzureTableStore;
using System.ComponentModel;

namespace MPWeb.Logic.Tables
{
    [Serializable]
    [DataContract(Namespace = "TourPoint")]
    public class PMTourPoint : AzureTableObjBase
    {
        public object lockObj = new Object();

        public enum enTourPointTypes
        {
            [Description("Warehouse")]
            WHS,
            [Description("Depot")]
            DEP
        }

        //Túrapont státuszok:
        // - FEHÉR: nem teljesített
        // - KÉK: teljesítés alatt
        // - ZÖLD:teljesített
        // - SÁRGA: feltételezve teljesített

        public enum enTpStatuses
        {
            [Description("White")]
            White,
            [Description("Blue")]
            Blue,
            [Description("Green")]
            Green,
            [Description("Yellow")]
            Yellow,

        }
        [DataMember]
        [AzureTablePartitionKeyAttr]
        public string TourID { get; set; }
        [DataMember]
        [AzureTableRowKeyAttr]
        public int Order { get; set; }
        [DataMember]
        public double Distance { get; set; }
        [DataMember]
        public DateTime ArrTime { get; set; }
        [DataMember]
        public DateTime ServTime { get; set; }
        [DataMember]
        public DateTime DepTime { get; set; }
        [DataMember]
        public DateTime PredictedArrTime { get; set; }
        [DataMember]
        public DateTime PredictedServTime { get; set; }
        [DataMember]
        public DateTime PredictedDepTime { get; set; }
        [DataMember]
        public DateTime RealArrTime { get; set; }
        [DataMember]
        public DateTime RealServTime { get; set; }
        [DataMember]
        public DateTime RealDepTime {
            get;
            set;
        }
        [DataMember]
        public string Code { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Addr { get; set; }
        [DataMember]
        public string Position { get; set; }
        [DataMember]
        public PMMapPoint ParsedPosition { get; set; }
        [DataMember]
        public string OrdNum { get; set; }
        [DataMember]
        public bool IsEverVisible { get; set; } = true;
        [DataMember]
        public bool Completed { get; set; }
        [DataMember]
        public bool UnderCompletion { get; set; }
        [DataMember]
        public bool Uncertain { get; set; }
        [DataMember]
        public DateTime Open { get; set; }
        [DataMember]
        public DateTime Close { get; set; }

   
        public enTpStatuses TpStatus            //Ez nem DataMember !!
        {
            get
            {
                if (UnderCompletion )
                    return enTpStatuses.Blue;
                if (Completed)
                    return enTpStatuses.Green;
                if (Uncertain )
                    return enTpStatuses.Yellow;

                return enTpStatuses.White;
            }
            set
            {
                switch( value)
                {
                    case enTpStatuses.White:
                        UnderCompletion = false;
                        Completed = false;
                        Uncertain = false;
                        RealArrTime = DateTime.MinValue;
                        RealServTime = DateTime.MinValue;
                        RealDepTime = DateTime.MinValue;
                        break;
                    case enTpStatuses.Blue:
                        UnderCompletion = true;
                        Completed = false;
                        Uncertain = false;
                        break;
                    case enTpStatuses.Green:
                        UnderCompletion = false;
                        Completed = true;
                        Uncertain = false;
                        break;
                    case enTpStatuses.Yellow:
                        UnderCompletion = false;
                        Completed = false;
                        Uncertain = true;
                        break;
                }
            }

        }

        [DataMember]
        public bool IsDelayed { get; set; }
        [DataMember]
        public TimeSpan Delay { get; set; }
        [DataMember]
        public List<PMMapPoint> MapPoints { get; set; } = new List<PMMapPoint>();
        [IgnoreDataMember]
        private enTourPointTypes m_type { get; set; }
        [DataMember]
        public string Type
        {
            get { return Enum.GetName(typeof(enTourPointTypes), m_type); }
            set
            {
                if (value != null)
                    m_type = (enTourPointTypes)Enum.Parse(typeof(enTourPointTypes), value);
                else
                    m_type = enTourPointTypes.DEP;

                NotifyPropertyChanged("Type");
            }
        }
    }
}
