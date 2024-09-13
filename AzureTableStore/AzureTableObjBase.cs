using MPWeb.Common.Attrib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace AzureTableStore
{
    [DataContract(Name = "AzureTableObjBase")]
    public class AzureTableObjBase : INotifyPropertyChanged, IDataErrorInfo
    {
        [IgnoreDataMember]
        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;
        public enum enObjectState
        {
            [Description("N")]
            New,               //not saved
            [Description("S")]
            Stored,
            [Description("M")]
            Modified,         //not saved
            [Description("I")]
            Inactive              //maybe saved but selected for deleting
        }


        public event PropertyChangedEventHandler PropertyChanged;


        // This method is called by the Set accessor of each property. 
        // The CallerMemberName attribute that is applied to the optional propertyName 
        // parameter causes the property name of the caller to be substituted as an argument. 
        public void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
            bool isDataMember = false;
            var pi = this.GetType().GetProperty(propertyName);
            if (pi != null)
            {
                isDataMember = Attribute.IsDefined(pi, typeof(DataMemberAttribute));

                var isPartitionKey = Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr));
                if (isPartitionKey && OriPartitionKey == null)
                {
                    OriPartitionKey = (string)pi.GetValue(this);
                }

                var isRowKey = Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr));
                if (isRowKey && OriRowKey == null)
                {
                    OriRowKey = (string)pi.GetValue(this);
                }

            }

            if (isDataMember && propertyName != "State" && this.m_State != enObjectState.Inactive && this.m_State != enObjectState.New)
                State = enObjectState.Modified;



        }
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            if(PropertyChanged != null)
                this.NotifyPropertyChanged(propertyName);
            return true;
        }
        public AzureTableObjBase()
        {
            m_State = enObjectState.New;
        }

        [IgnoreDataMember]
        protected string OriPartitionKey { get; set; } = null;

        [IgnoreDataMember]
        protected string OriRowKey { get; set; } = null;

        [IgnoreDataMember]
        public bool NewState
        {
            get { return m_State == enObjectState.New; }
        }

        [IgnoreDataMember]
        public bool ActiveState
        {
            get { return m_State != enObjectState.Inactive; }
        }

        [IgnoreDataMember]
        public bool StoredState
        {
            get { return m_State == enObjectState.Stored; }
        }

        [IgnoreDataMember]
        public bool UnSavedState
        {
            get { return m_State != enObjectState.Stored; }
        }

        [IgnoreDataMember]
        public bool ModifiedState
        {
            get { return m_State == enObjectState.Modified; }
        }

        [IgnoreDataMember]
        public bool ChangedState
        {
            get { return m_State == enObjectState.Modified || m_State == enObjectState.New; }
        }

        [IgnoreDataMember]
        private enObjectState m_State = enObjectState.New;
        [DataMember]
        [XmlElement(ElementName = "State")]
        [IgnoreDataMember]
        public enObjectState State {
            get { return m_State; }
            set {
                m_State = value;
                NotifyPropertyChanged("State");
                NotifyPropertyChanged("NewState");
                NotifyPropertyChanged("ActiveState");
                NotifyPropertyChanged("StoredState");
                NotifyPropertyChanged("UnSavedState");
                NotifyPropertyChanged("ModifiedState");
                NotifyPropertyChanged("ChangedState");
            }
        }


        #region IDataErrorInfo Members

        [IgnoreDataMember]
        public string Error
        {
            get
            {
                return null;
            }
        }


        /// <summary>
        /// Examines the property that was changed and provides the
        /// correct error message based on some rules
        /// </summary>
        /// <param name="name">The property that changed</param>
        /// <returns>a error message string</returns>
        [IgnoreDataMember]
        public string this[string name]
        {
            get
            {

                string result = null;
                var pi = this.GetType().GetProperty(name);

                var context = new ValidationContext(this, null, null);
                context.MemberName = pi.Name;
                var results = new List<ValidationResult>();
                var v = pi.GetValue(this, null);
                var isValid = Validator.TryValidateProperty(v, context, results);
                if (!isValid)
                {

                    foreach (var validationResult in results)
                    {
                        if (result != null)
                            result += Environment.NewLine;
                        else
                            result = "";
                        result = validationResult.ErrorMessage;
                    }
                }
                return result;
            }
        }
        #endregion

        [IgnoreDataMember]
        public string LocalizedDateTimeFormat
        {
            get
            {
                /*
                if (CultureInfo.TwoLetterISOLanguageName == "fr")
                    return "MM/dd/yyyy HH:mm";
                else if (CultureInfo.TwoLetterISOLanguageName == "en")
                    return "MM/dd/yyyy HH:mm";
                    */
                return "yyyy.MM.dd HH:mm";
            }
        }

        [IgnoreDataMember]
        public DateTime m_Created;

        [DataMember]
        [XmlElement(ElementName = "Created")]
        public DateTime Created { get { return m_Created; } set { m_Created = value; Updated = value; } }

        [DataMember]
        [XmlElement(ElementName = "Updated")]
        public DateTime Updated { get; set; }
        [DataMember]
        [XmlElement(ElementName = "Creator")]
        public string Creator { get; set; }
        [DataMember]
        [XmlElement(ElementName = "Updater")]
        public string Updater { get; set; }

        protected static Dictionary<string, string> GetEnumToDictionary<T>(T[] p_banned = null)
        {
            var dic = Enum.GetValues(typeof(T))
               .Cast<T>().Where(w => p_banned == null || !p_banned.Contains(w))
       
               .ToDictionary(k => k.ToString(), v => GetEnumDescription(v as Enum));
            return dic;
        }
        protected static string GetEnumDescription(Enum p_value)
        {
            FieldInfo fi = p_value.GetType().GetField(p_value.ToString());

            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false);

            if (attributes != null &&
                attributes.Length > 0)
                return attributes[0].Description;
            else
                return p_value.ToString();
        }
    }

    public class HeaderInfo
    {
        public string HeaderName { get; set; }
        public int startColumn { get; set; }
        public int endColumn { get; set; }
        public List<HeaderDetail> headerDetails { get; set; }

        public HeaderInfo()
        {
            this.headerDetails = new List<HeaderDetail>();
        }
    }

    public class HeaderDetail
    {
        public string HeaderDetailName { get; set; }
        public int HeaderDetailColumn { get; set; }
    }
}
