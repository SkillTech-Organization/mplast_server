using MPWeb.Common.Attrib;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;
using static MPWeb.Common.Enums;

namespace MPWeb.Common.Generators
{
    public class FormlyFormBuilder
    {
        private static Random _rnd = new Random();
        private int _unique = 1;
        //Lazy objects are thread safe, double checked and they have better performance than locks.
        //see it: http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Lazy<FormlyFormBuilder> m_instance = new Lazy<FormlyFormBuilder>(() => new FormlyFormBuilder(), true);

        static public FormlyFormBuilder Instance                   //inicializálódik, ezért biztos létrejön az instance osztály)
        {
            get
            {
                return m_instance.Value;            //It's thread safe!
            }
        }

        private string GetFormlyControlType(VariableType type)
        {
            string result = string.Empty;
            switch (type)
            {
                case VariableType.String:
                    result = "input";
                    break;
                case VariableType.Int:
                case VariableType.Double:
                    result = "kendoNTB";
                    break;
                case VariableType.Date:
                    result = "kendoDP";
                    break;
                case VariableType.DateTime:
                    result = "kendoDTP";
                    break;
                default:
                    break;
            }
            return result;
        }

        public string Build(object obj, int level = 0)
        {
            StringBuilder sb = new StringBuilder();

            //Want to iterate through returnData to do something to it.
            if (obj is IEnumerable)
            {
                // get generic type argument
                var dataType = obj.GetType();

                if (dataType.IsGenericType)
                {
                    // this is a System.Collections.Generic.IEnumerable<T> -- get the generic type argument to loop through it
                    Type genericArgument = dataType.GetGenericArguments()[0];

                    var genericEnumerator =
                        typeof(System.Collections.Generic.IEnumerable<>)
                            .MakeGenericType(genericArgument)
                            .GetMethod("GetEnumerator")
                            .Invoke(obj, null);

                    IEnumerator enm = genericEnumerator as IEnumerator;
                    while (enm.MoveNext())
                    {
                        var item = enm.Current;
                        _unique++;
                        sb.Append(Build(item, level+1));
                    }

                }
                else
                {
                    int index = 0;
                    // this is an System.Collections.IEnumerable (not generic)
                    foreach (var item in (obj as IEnumerable))
                    {
                        _unique++;
                        sb.Append(Build(item, level+1));
                    }
                }
            }
            else
            {
                Type objType = obj.GetType();
                PropertyInfo varType = objType.GetProperty("VariableType");
                string controlType = GetFormlyControlType((VariableType)varType.GetValue(obj));
                PropertyInfo[] fields = objType.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                int index = 0;
                foreach (PropertyInfo pi in fields)
                {
                    var attr = pi.GetCustomAttributes<FormControlAttribute>().FirstOrDefault();
                    if (attr == null || !attr.Visible)
                        continue;
                    index++;
                    sb.Append("{");
                    sb.AppendFormat("\"id\": \"{0}_{1}_{2}{3}\",", pi.Name, index, _unique, _rnd.Next(0,9999));
                    sb.AppendFormat("\"key\": \"{0}\",", pi.Name);

                    sb.AppendFormat("\"type\": \"{0}\",", attr.UseVariableType ? controlType : "input");
                    sb.Append("\"className\": \"col-xs-#NUM#\",");

                    if (attr.Visible)
                    {
                        sb.Append("\"templateOptions\": {");
                        if (attr.ShowLabel)
                        {
                            sb.AppendFormat("\"label\": \"{0}\"", attr.Label);
                        }
                        sb.AppendFormat("\"disabled\": {0}", attr.ReadOnly.ToString().ToLower());
                        sb.Append("}");
                    }
                    sb.Append("},");
                }
                int colValue = 12 / index;
                sb = sb.Replace("#NUM#", string.Format("{0}", colValue < 3 ? 12 : colValue));
            }
            string subString = sb.ToString().Replace(",}", "}").Trim(',');
            if (level != 0)
            {
                sb = new StringBuilder(string.Format("{{\"className\": \"row\", \"fieldGroup\": [{0}]}},", subString));
            }
            else
            {
                sb = new StringBuilder(string.Format("[{0}]", subString));
            }
            return sb.ToString();
        }

    }
}