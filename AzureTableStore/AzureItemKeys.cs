using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureTableStore
{
    public class AzureItemKeys
    {
        public AzureItemKeys(string p_partitionKey, string p_rowKey)
        {
            PartitionKey = p_partitionKey;
            RowKey = p_rowKey;
        }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
    }

}
