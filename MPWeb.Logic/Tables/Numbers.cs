using MPWeb.Common.Attrib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using AzureTableStore;

namespace MPWeb.Logic.Tables
{
    public class Numbers : AzureTableObjBase
    {
        public class CUsedNumber
        {
            public int Number { get; set; }
            public long Ticks { get; set; }
            public string SessionID { get; set; }
            public string User { get; set; }            //Tájékoztató adat

            [IgnoreDataMember]
            public DateTime TimeStamp { get { return new DateTime(Ticks); } }
        }

        public const string PartitonConst = "NUMBERS";

        [DataMember]
        [AzureTablePartitionKeyAttr]
        public string PartitionKey { get; set; } = PartitonConst;

        private string m_code;
        [DataMember]
        [AzureTableRowKeyAttr]
        [GridColumnAttr(Header = "Code")]
        public string Code { get { return m_code; } set { m_code = value; NotifyPropertyChanged("Code"); } }

        private int m_number;
        [DataMember]
        public int Number
        {
            get { return m_number; }
            set
            {
                m_number = value;
                NotifyPropertyChanged("Number");
            }
        }



        private List<CUsedNumber> m_usedNumberList { get; set; } =  new List<CUsedNumber>();
        [DataMember]
        public List<CUsedNumber> UsedNumberList
        {
            get { return m_usedNumberList; }
            set
            {
                m_usedNumberList = value;
                NotifyPropertyChanged("UsedNumberList");
            }
        }

    }
}
