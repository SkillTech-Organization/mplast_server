using System;

namespace MPWeb.Common.Attrib
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AzureTableFieldAttr: Attribute
    {
        public string FieldName { get; set; }

        public AzureTableFieldAttr()
            : base()
        {
            FieldName = "";
        }
       

        public AzureTableFieldAttr(string p_name)
        {
            FieldName = p_name;
        }
    }
}
