using AzureTableStore;
using Newtonsoft.Json;
using MPWeb.Common.Attrib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.Tables
{
    public class Log : AzureTableObjBase
    {

        public enum enLogTypes
        {
            [Description("NOTYPE")]
            NOTYPE,
            [Description("SYSTEM")]
            SYSTEM,
            [Description("USER")]
            USER
        }
        public Log()
        {
            m_type = enLogTypes.NOTYPE;
        }

        [IgnoreDataMember]
        private string m_functionName;
        [JsonProperty("FunctionName")]
        [AzureTablePartitionKeyAttr]
        [GridColumnAttr(Header = "FunctionName", Order = 1)]
        public string FunctionName
        {
            get { return m_functionName; }
            set { m_functionName = value; NotifyPropertyChanged("FunctionName"); }
        }

        [IgnoreDataMember]
        private string m_ID { get; set; }
        [JsonProperty("ID")]
        [AzureTableRowKeyAttr]
        [GridColumnAttr(Header = "ID", Order = 2)]
        public string ID
        {
            get { return m_ID; }
            set
            {
                m_ID = value;
                NotifyPropertyChanged("ID");
            }
        }

        [IgnoreDataMember]
        private enLogTypes m_type { get; set; }

        [DataMember]
        public string Type
        {
            get { return Enum.GetName(typeof(enLogTypes), m_type); }
            set
            {
                if (value != null)
                    m_type = (enLogTypes)Enum.Parse(typeof(enLogTypes), value);
                else
                    m_type = enLogTypes.NOTYPE;

                NotifyPropertyChanged("Type");
            }
        }

        [JsonProperty("TypeX")]
        [GridColumnAttr(Header = "Type")]
        public string TypeX { get { return m_type.ToString(); } }


        private string m_Text { get; set; }
        [DataMember]
        [JsonProperty("Text")]
        [GridColumnAttr(Header = "Text", Order = 3)]
        public string Text
        {
            get { return m_ID; }
            set
            {
                m_Text = value;
                NotifyPropertyChanged("Text");
            }
        }


    }
}
