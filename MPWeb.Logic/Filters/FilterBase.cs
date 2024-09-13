using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MPWeb.Common.Attrib;
using MPWeb.Common;
using AzureTableStore;

namespace MPWeb.Logic.Filters
{
    public class FilterBase<T> where T : AzureTableObjBase
    {
        private const string IsoDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public virtual string ToTableStoreFilter(string filter = "")
        {
            var props = this.GetType().GetProperties()
                .Where(p =>
                {
                    var pr = p.GetCustomAttribute<FilteredPropAttr>();
                    return pr != null;
                });
            StringBuilder sb = new StringBuilder();
            DateTime tmpDate;
            bool isDateTime = false;
            foreach (var item in props)
            {
                var filterAttr = item.GetCustomAttribute<FilteredPropAttr>();
                if (item.GetValue(this) == null || string.IsNullOrEmpty(item.GetValue(this).ToString()))
                    continue;
                if (sb.Length != 0)
                    sb.Append(TableOperators.And);
                if (DateTime.TryParse(item.GetValue(this).ToString(), out tmpDate))
                {
                    isDateTime = true;
                    if (filterAttr.Operator == QueryComparisons.LessThan ||
                        filterAttr.Operator == QueryComparisons.LessThanOrEqual)
                    {
                        tmpDate = tmpDate.AddDays(1).AddMilliseconds(-1);
                    }
                }
                sb.AppendFormat(" ({0} {1} {3}'{2}') ",
                    filterAttr.PropName,
                    filterAttr.Operator,
                    item.PropertyType.IsDateTime() ? ((DateTime)item.GetValue(this)).ToString(IsoDateTimeFormat) :
                    isDateTime ? tmpDate.ToString(IsoDateTimeFormat) : item.GetValue(this),
                    item.PropertyType.IsDateTime() ? "datetime" :
                    (item.PropertyType.IsGuid() ? "guid" : ""));
            }
            filter = KendoFilterToTableStoreFilter(filter);
            if (!string.IsNullOrWhiteSpace(filter))
                sb.AppendFormat("{0}({1}) ", sb.Length > 0 ? " and " : "", filter);
            return sb.ToString();
        }

        public static string KendoFilterToTableStoreFilter(string filter)
        {
            string pattern = @"((?'field'\w+)~(?'op'\w+)~(?'value'[\w'-]+))(~(?'and'and)~)?";

            filter = Regex.Replace(filter, pattern, delegate (Match match)
            {
                string field = match.Groups["field"].Value;
                string op = match.Groups["op"].Value;
                string value = match.Groups["value"].Value;
                string and = match.Groups["and"].Value;
                var pInfo = typeof(T).GetProperties().Where(f => f.Name == field).SingleOrDefault();
                var azureField = pInfo.GetCustomAttribute<AzureTableFieldAttr>();
                if (azureField != null)
                    field = azureField.FieldName;
                if (op == "startswith")
                    op = "eq";
                op = ToTablestoreOperator(op);
                if (value.StartsWith("datetime"))
                {
                    string datepattern = @"datetime'(?'date'\d{4}-\d{2}-\d{2})T(?'time'\d{2}-\d{2}-\d{2})'";
                    value = Regex.Replace(value, datepattern, delegate (Match dmatch)
                    {
                        string date = dmatch.Groups["date"].Value;
                        string time = dmatch.Groups["time"].Value.Replace("-", ":");
                        if (op == "eq" && time.StartsWith("00:00:00"))
                            return string.Format("{1} ge datetime'{0}T00:00:00.000Z' and {1} lt datetime'{0}T23:59:59.999Z'", date, field);
                        else
                            return string.Format("{0} {1} datetime'{2}T{3}.000Z'", field, op, date, time);
                    });
                    return string.Format("{0} {1} ", value, and);
                }
                else
                {
                    return string.Format("{0} {1} {2} {3} ", field, op, value, and);
                }
            }, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return filter;
        }

        private static string ToTablestoreOperator(string kendoOp)
        {
            switch (kendoOp)
            {
                case "eq":
                    return QueryComparisons.Equal;
                case "neq":
                    return QueryComparisons.NotEqual;
                case "lt":
                    return QueryComparisons.LessThan;
                case "lte":
                    return QueryComparisons.LessThanOrEqual;
                case "gt":
                    return QueryComparisons.GreaterThan;
                case "gte":
                    return QueryComparisons.GreaterThanOrEqual;
                default:
                    break;
            }
            return null;
        }
    }
}
