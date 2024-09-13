using MPWeb.Common.Attrib;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace MPWeb.Common
{
    public static class Utils
    {
        public static string GetTempFilePathWithExtension(string extension)
        {
            var path = Path.GetTempPath();
            var fileName = String.Concat(Guid.NewGuid().ToString(), extension);
            return Path.Combine(path, fileName);
        }

        public static void CopyStream(Stream input, Stream output)
        {
            byte[] buffer = new byte[8 * 1024];
            int len;
            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }

        public static bool IsDateTime(this Type type)
        {
            return type == typeof(DateTime) || type == typeof(DateTime?);
        }

        public static bool IsGuid(this Type type)
        {
            return type == typeof(Guid) || type == typeof(Guid?);
        }

        public static string GenerateHashCode(string decodeString)
        {
            System.Security.Cryptography.SHA1 hash = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            var h2 = hash.ComputeHash(Encoding.Unicode.GetBytes(decodeString));
            var hh = HexStringFromBytes(h2);
            return hh;
        }

        private static string HexStringFromBytes(byte[] bytes)
        {
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                var hex = b.ToString("x2");
                sb.Append(hex);
            }
            return sb.ToString();
        }

        public static T ForceType<T>(this object o)
        {
            T res;
            res = Activator.CreateInstance<T>();

            Type x = o.GetType();
            Type y = res.GetType();

            foreach (var destinationProp in y.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                var sourceProp = x.GetProperty(destinationProp.Name);
                if (destinationProp.CanWrite && sourceProp.CanRead && sourceProp != null)
                {
                    destinationProp.SetValue(res, sourceProp.GetValue(o));
                }
            }

            return res;
        }

        public static Type GetGenericCollectionItemType(Type type)
        {
            return type.GetInterfaces()
                .Where(face => face.IsGenericType &&
                               face.GetGenericTypeDefinition() == typeof(ICollection<>))
                .Select(face => face.GetGenericArguments()[0])
                .FirstOrDefault();
        }

        public static void GetGridSchemaModel<T>(string p_hiddenColumns, out string schemaField, out string columns)
        {
            schemaField = "";
            columns = "";
            var numFormat = " \"format\": \"{0:0.##########}\", ";

            StringBuilder sbF = new StringBuilder("{ \"data\": \"Data\", \"total\":\"Total\", \"model\" : { \"id\":\"ID\", \"fields\" : { ");
            StringBuilder sbC = new StringBuilder("[");
            var l_hiddenColumns = p_hiddenColumns.ToUpper().Split(",".ToCharArray()).ToList();
            PropertyInfo[] props = typeof(T).GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(GridColumnAttr))).ToArray(); //[GridColumn] attribútumra szűrni
            foreach (var propInf in props.OrderBy(o => o.GetCustomAttribute<GridColumnAttr>().Order))
            {
                int width = 120;
                string title = propInf.Name;
                if (!l_hiddenColumns.Contains(title.ToUpper()))
                {
                    bool filterable = false;
                    var ga = propInf.GetCustomAttribute<GridColumnAttr>();
                    if (ga != null)
                    {
                        title = ga.Header;
                        filterable = ga.Filterable;
                        width = ga.Width;
                    }

                    if (propInf.PropertyType == typeof(DateTime) ||
                        propInf.PropertyType == typeof(DateTime?) ||
                        propInf.Name.ToLower().Contains("date"))
                    {
                        sbF.Append($"\"{propInf.Name}\": {{\"type\": \"date\", \"editable\": false}},");
                        sbC.Append($"{{\"field\":\"{propInf.Name}\", \"title\": \"{title}\", \"filterable\": {{\"cell\": {{\"enabled\":{filterable.ToString().ToLower()}}}}}, \"format\":\"{{0:g}}\", \"parseFormats\": [\"yyyy-MM-dd'T'hh:mm:ss\", \"yyyy-MM-ddThh:mm:ss\", \"yyyy-MM-dd\"], \"width\":\"{width}px\"}},");
                    }
                    else if (propInf.PropertyType == typeof(int) ||
                             propInf.PropertyType == typeof(int?) ||
                             propInf.PropertyType == typeof(double) ||
                             propInf.PropertyType == typeof(double?) ||
                             propInf.PropertyType == typeof(decimal) ||
                             propInf.PropertyType == typeof(decimal?) ||
                             propInf.PropertyType == typeof(float) ||
                             propInf.PropertyType == typeof(float?))
                    {
                        sbF.Append($"\"{propInf.Name}\": {{\"type\": \"number\", \"editable\": false}},");
                        sbC.Append($"{{\"field\":\"{propInf.Name}\", \"title\": \"{title}\", \"filterable\": {{\"cell\": {{\"enabled\":{filterable.ToString().ToLower()}}}}}, {numFormat} \"width\":\"{width}px\"}},");

                    }
                    else if (propInf.PropertyType == typeof(bool) ||
                             propInf.PropertyType == typeof(bool?))
                    {
                        sbF.Append($"\"{propInf.Name}\": {{\"type\": \"boolean\", \"editable\": false}},");
                        sbC.Append($"{{\"field\":\"{propInf.Name}\", \"title\": \"{title}\", \"filterable\": {{\"cell\": {{\"enabled\":{filterable.ToString().ToLower()}}}}}, \"width\":\"{width}px\", \"template\": \"<input type='checkbox' #= {propInf.Name} ? checked='checked' : '' # disabled='disabled'></input>\"}},");
                    }
                    else
                    {
                        sbF.Append($"\"{propInf.Name}\": {{\"type\": \"string\", \"editable\": false}},");
                        sbC.Append($"{{\"field\":\"{propInf.Name}\", \"title\": \"{title}\", \"filterable\": {{\"cell\": {{\"enabled\":{filterable.ToString().ToLower()}}}}}, \"width\":\"{width}px\"}},");
                    }
                }
            }
            sbF.Append("}}}");
            sbC.Append("]");
            sbF = sbF.Replace(",}", "}");
            sbC = sbC.Replace(",]", "]");
            schemaField = sbF.ToString();
            columns = sbC.ToString();
        }

        public static Dictionary<string, string> GetEnumToDictionary<T>(T[] p_banned = null)
        {
            var dic = Enum.GetValues(typeof(T))
               .Cast<T>().Where(w => p_banned == null || !p_banned.Contains(w))

               .ToDictionary(k => k.ToString(), v => GetEnumDescription(v as Enum));
            return dic;
        }
        public static string GetEnumDescription(Enum p_value)
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


        public static NameValueCollection ParseHiddenFields(string htmlString)
        {
            Regex reg = new Regex("\\|[0-9]+\\|hiddenField\\|(?'name'[_A-Z]+)\\|(?'value'[^\\|]*)", RegexOptions.Multiline);
            var matches = reg.Matches(htmlString).Cast<Match>().ToList();
            var result = new NameValueCollection();
            matches.ForEach(m => {
                result.Add(m.Groups["name"].Value,
                m.Groups["value"].Value);
            });
            return result;
        }

        public static DateTime Now()
        {
            return new DateTime(DateTime.Now.Ticks, DateTimeKind.Utc);
        }

            public static IEnumerable<string> ChunkString(string str, int maxChunkSize)
            {
                for (int i = 0; i < str.Length; i += maxChunkSize)
                    yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
            }

        /// <summary>
        /// http://zeeshanumardotnet.blogspot.hu/2011/04/how-to-restart-your-web-application.html
        /// </summary>
        /// <returns></returns>
        public static bool RestartAppPool()

        {

            //First try killing your worker process

            try

            {

                //Get the current process
                Process process = Process.GetCurrentProcess();

                // Kill the current process
                process.Kill();

                // if your application have no rights issue then it will restart your app pool
                return true;
            }
            catch (Exception ex)
            {
                //if exception occoured then log exception
     //           Logger.Log("Restart Request Failed. Exception details :-" + ex);
            }



            //Try unloading appdomain

            try
            {
                //note that UnloadAppDomain requires full trust
                HttpRuntime.UnloadAppDomain();
                return true;
            }
            catch (Exception ex)
            {
                //if exception occoured then log exception
           //     Logger.Log("Restart Request Failed. Exception details :-" + ex);
            }



            //Finally automating the dirtiest way to restart your application pool

            //get the path of web.config

            string webConfigPath = HttpContext.Current.Request.PhysicalApplicationPath + "\\web.config";
            try
            {

                //Change the last modified time and it will restart pool
                File.SetLastWriteTimeUtc(webConfigPath, DateTime.UtcNow);
                return true;
            }

            catch (Exception ex)

            {
                //if exception occoured then log exception
    //            Logger.Log("Restart Request Failed. Exception details :-" + ex);
            }


            //Still no hope, you have to do something else.

            return false;

        }
    }
}

