using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace MPWeb.Common.Attrib
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FilteredPropAttr: Attribute
    {
        public string PropName { get; set; }

        public string Operator { get; set; }

        public FilteredPropAttr()
            : base()
        {
            PropName = "";
            Operator = QueryComparisons.Equal;
        }
       

        public FilteredPropAttr(string p_name, string p_operator = QueryComparisons.Equal)
        {
            PropName = p_name;
            Operator = p_operator;
        }
    }
}
