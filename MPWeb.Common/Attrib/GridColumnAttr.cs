using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace MPWeb.Common.Attrib
{
    //this attribute indicates item type fields
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class GridColumnAttr : DisplayNameAttribute
    {
        public string Header { get; set; }
        public bool Filterable { get; set; }
        public int Order { get; set; }
        public int Width { get; set; }

        public GridColumnAttr()
            : base()
        {
            Header = "";
            Filterable = true;
            Order = 0;
            Width = 120;
        }

        public GridColumnAttr(string p_header, bool p_filterable)
            : base(p_header)
        {
            Header = p_header;
            Filterable = p_filterable;
        }

        public GridColumnAttr(string p_header, bool p_filterable, int p_order)
        : base(p_header)
        {
            Header = p_header;
            Filterable = p_filterable;
            Order = p_order;
        }

        public override string DisplayName => Header;
    }
}
