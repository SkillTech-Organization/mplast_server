using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MPWeb
{
    public static class HtmlHelpers
    {
        public static HtmlString ApplicationVersion(this HtmlHelper helper)
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version;
            var product = asm.GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), true).FirstOrDefault() as System.Reflection.AssemblyProductAttribute;

            if (version != null && product != null)
            {
                return new HtmlString(string.Format("<span>v{0}.{1}.{2}&nbsp;({3})</span>", version.Major, version.Minor, version.Build, version.Revision));
            }
            else
            {
                return new HtmlString("");
            }

        }

        public static HtmlString BuildRevision(this HtmlHelper helper)
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            var version = asm.GetName().Version;
            var product = asm.GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), true).FirstOrDefault() as System.Reflection.AssemblyProductAttribute;

            if (version != null && product != null)
            {
                return new HtmlString(version.Revision.ToString());
            }
            else
            {
                return new HtmlString("0");
            }

        }

        public static HtmlString BaseUrl(this HtmlHelper helper)
        {
            var request = HttpContext.Current.Request;
            if (request.Url == (Uri)null)
                return new HtmlString("");
            else
                return new HtmlString(request.Url.Scheme + "://" + request.Url.Authority + VirtualPathUtility.ToAbsolute("~/"));
        }
    }
}