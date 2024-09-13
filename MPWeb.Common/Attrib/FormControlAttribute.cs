using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Common.Attrib
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FormControlAttribute : Attribute
    {

        public string Label { get; set; }

        [DefaultValue(true)]
        public bool ShowLabel { get; set; }

        [DefaultValue(true)]
        public bool Visible { get; set; }

        [DefaultValue(false)]
        public bool ReadOnly { get; set; }

        public bool UseVariableType { get; set; }

        public FormControlAttribute()
        {
            ShowLabel = true;
            Visible = true;
            ReadOnly = false;
            UseVariableType = false;
        }

    }
}
