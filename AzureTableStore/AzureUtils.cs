using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AzureTableStore
{
    public static class ContainsExtension
    {
        public static Expression<Func<TEntity, bool>> Contains<TEntity,
            TProperty>(this IEnumerable<object> values,
            Expression<Func<TEntity, TProperty>> expression)
        {
            // Get the property name
            var propertyName = ((PropertyInfo)((MemberExpression)expression.Body).Member).Name;

            // Create the parameter expression
            var parameterExpression = Expression.Parameter(typeof(TEntity), "e");

            // Init the body
            Expression mainBody = Expression.Constant(false);

            foreach (var value in values)
            {
                // Create the equality expression
                var equalityExpression = Expression.Equal(
                    Expression.PropertyOrField(parameterExpression, propertyName),
                    Expression.Constant(value));

                // Add to the main body
                mainBody = Expression.OrElse(mainBody, equalityExpression);
            }

            return Expression.Lambda<Func<TEntity, bool>>(mainBody, parameterExpression);
        }
    }

    public static class CloudExtensions
    {

        public static IEnumerable<TElement> StartsWith<TElement>
        (this CloudTable table, string partitionKey, string searchStr,
        string columnName = "RowKey") where TElement : ITableEntity, new()
        {
            if (string.IsNullOrEmpty(searchStr)) return null;

            char lastChar = searchStr[searchStr.Length - 1];
            char nextLastChar = (char)((int)lastChar + 1);
            string nextSearchStr = searchStr.Substring(0, searchStr.Length - 1) + nextLastChar;
            string prefixCondition = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition(columnName, QueryComparisons.GreaterThanOrEqual, searchStr),
                TableOperators.And,
                TableQuery.GenerateFilterCondition(columnName, QueryComparisons.LessThan, nextSearchStr)
                );

            string filterString = TableQuery.CombineFilters(
                TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey),
                TableOperators.And,
                prefixCondition
                );
            var query = new TableQuery<TElement>().Where(filterString);
            return table.ExecuteQuery<TElement>(query);
        }
    }
    public static class TableQueryExtensions
    {
        public static TableQuery<TElement> AndWhere<TElement>(this TableQuery<TElement> @this, string filter)
        {
            @this.FilterString = TableQuery.CombineFilters(@this.FilterString, TableOperators.And, filter);
            return @this;
        }

        public static TableQuery<TElement> OrWhere<TElement>(this TableQuery<TElement> @this, string filter)
        {
            @this.FilterString = TableQuery.CombineFilters(@this.FilterString, TableOperators.Or, filter);
            return @this;
        }

        public static TableQuery<TElement> NotWhere<TElement>(this TableQuery<TElement> @this, string filter)
        {
            @this.FilterString = TableQuery.CombineFilters(@this.FilterString, TableOperators.Not, filter);
            return @this;
        }
    }

    public class IbizaXml
    {
        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
        public partial class importEntryRequest
        {

            private System.DateTime importDateField;

            private importEntryRequestImportEntry[] wsImportEntryField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
            public System.DateTime importDate
            {
                get
                {
                    return this.importDateField;
                }
                set
                {
                    this.importDateField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlArrayItemAttribute("importEntry", IsNullable = false)]
            public importEntryRequestImportEntry[] wsImportEntry
            {
                get
                {
                    return this.wsImportEntryField;
                }
                set
                {
                    this.wsImportEntryField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class importEntryRequestImportEntry
        {

            private string journalRefField;

            private System.DateTime dateField;

            private string accountNumberField;

            private string descriptionField;

            private decimal debitField;

            private bool debitFieldSpecified;

            private decimal creditField;

            private bool creditFieldSpecified;

            private string pieceField;

            private string voucherRefField;

            private string termField;

            private importEntryRequestImportEntryImportAnalyticalEntries importAnalyticalEntriesField;

            /// <remarks/>
            public string journalRef
            {
                get
                {
                    return this.journalRefField;
                }
                set
                {
                    this.journalRefField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
            public System.DateTime date
            {
                get
                {
                    return this.dateField;
                }
                set
                {
                    this.dateField = value;
                }
            }

            /// <remarks/>
            public string accountNumber
            {
                get
                {
                    return this.accountNumberField;
                }
                set
                {
                    this.accountNumberField = value;
                }
            }

            /// <remarks/>
            public string description
            {
                get
                {
                    return this.descriptionField;
                }
                set
                {
                    this.descriptionField = value;
                }
            }

            /// <remarks/>
            public decimal debit
            {
                get
                {
                    return this.debitField;
                }
                set
                {
                    this.debitField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool debitSpecified
            {
                get
                {
                    return this.debitFieldSpecified;
                }
                set
                {
                    this.debitFieldSpecified = value;
                }
            }

            /// <remarks/>
            public decimal credit
            {
                get
                {
                    return this.creditField;
                }
                set
                {
                    this.creditField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlIgnoreAttribute()]
            public bool creditSpecified
            {
                get
                {
                    return this.creditFieldSpecified;
                }
                set
                {
                    this.creditFieldSpecified = value;
                }
            }

            /// <remarks/>
            public string piece
            {
                get
                {
                    return this.pieceField;
                }
                set
                {
                    this.pieceField = value;
                }
            }

            /// <remarks/>
            public string voucherRef
            {
                get
                {
                    return this.voucherRefField;
                }
                set
                {
                    this.voucherRefField = value;
                }
            }

            /// <remarks/>
            public string term
            {
                get
                {
                    return this.termField;
                }
                set
                {
                    this.termField = value;
                }
            }

            /// <remarks/>
            public importEntryRequestImportEntryImportAnalyticalEntries importAnalyticalEntries
            {
                get
                {
                    return this.importAnalyticalEntriesField;
                }
                set
                {
                    this.importAnalyticalEntriesField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class importEntryRequestImportEntryImportAnalyticalEntries
        {

            private importEntryRequestImportEntryImportAnalyticalEntriesImportAnalyticalEntry importAnalyticalEntryField;

            /// <remarks/>
            public importEntryRequestImportEntryImportAnalyticalEntriesImportAnalyticalEntry importAnalyticalEntry
            {
                get
                {
                    return this.importAnalyticalEntryField;
                }
                set
                {
                    this.importAnalyticalEntryField = value;
                }
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class importEntryRequestImportEntryImportAnalyticalEntriesImportAnalyticalEntry
        {

            private string analysisField;

            private string axis1Field;

            private string axis2Field;

            private decimal creditField;

            /// <remarks/>
            public string analysis
            {
                get
                {
                    return this.analysisField;
                }
                set
                {
                    this.analysisField = value;
                }
            }

            /// <remarks/>
            public string axis1
            {
                get
                {
                    return this.axis1Field;
                }
                set
                {
                    this.axis1Field = value;
                }
            }

            /// <remarks/>
            public string axis2
            {
                get
                {
                    return this.axis2Field;
                }
                set
                {
                    this.axis2Field = value;
                }
            }

            /// <remarks/>
            public decimal credit
            {
                get
                {
                    return this.creditField;
                }
                set
                {
                    this.creditField = value;
                }
            }
        }


    }
}
