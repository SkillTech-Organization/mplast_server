using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using MPWeb.Common.Attrib;
using Newtonsoft.Json;
using MPWeb.Common;
using System.Text.RegularExpressions;

namespace AzureTableStore
{
    public class AzureAccess
    {
        private const string IsoDateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
        private const string AzureConnectStringName = "AzureTableStore";
        private string m_AzureConnectString = "";

        private const int BATCHSIZE = 100;
        private const int SPLIT_STR_LEN = 32000;    //UTF-8 és UTF-16 -hoz is jó

        private CloudStorageAccount m_account = null;
        private CloudTableClient m_client = null;
        private long m_lastID = -1;

        //Lazy objects are thread safe, double checked and they have better performance than locks.
        //see it: http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly Lazy<AzureAccess> m_instance = new Lazy<AzureAccess>(() => new AzureAccess(), true);

        static public AzureAccess Instance                   //inicializálódik, ezért biztos létrejon az instance osztály)
        {
            get
            {
                return m_instance.Value;            //It's thread safe!
            }
        }

        private AzureAccess()
        {
            if (ConfigurationManager.ConnectionStrings[AzureConnectStringName] == null)
                throw new Exception(AzureConnectStringName + " connect string is not in appp.config!");

            m_AzureConnectString = ConfigurationManager.ConnectionStrings[AzureConnectStringName].ConnectionString;
        }

        private void InitTableStore()
        {
            try
            {
                if (m_account == null)
                    m_account = CloudStorageAccount.Parse(m_AzureConnectString);
                if (m_client == null)
                    m_client = m_account.CreateCloudTableClient();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private bool parseHttpStatus(int p_HttpStatusCode)
        {
            return ("200,201,202,203,204,205").Contains(p_HttpStatusCode.ToString());
        }

        private DynamicTableEntity cloneForWrite(object p_obj)
        {
            JsonSerializerSettings jsonsettings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };

            DynamicTableEntity TableEntity = null;
            Type tp = p_obj.GetType();
            PropertyInfo PartitionKeyProp = tp.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
            if (PartitionKeyProp == null)
                throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");

            string PartitionKey = GetValidAzureKeyValue(PartitionKeyProp.PropertyType, tp.GetProperty(PartitionKeyProp.Name).GetValue(p_obj, null));
            string RowKey = "";

            PropertyInfo RowKeyProp = tp.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();
            if (RowKeyProp != null)
                RowKey = GetValidAzureKeyValue(RowKeyProp.PropertyType, tp.GetProperty(RowKeyProp.Name).GetValue(p_obj, null));

            TableEntity = new DynamicTableEntity(PartitionKey, RowKey);

            try
            {
                PropertyInfo[] writeProps = tp.GetProperties().Where(pi => (Attribute.IsDefined(pi, typeof(DataMemberAttribute)) &&
                                                                           !Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr)) &&
                                                                           !Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr)))).ToArray<PropertyInfo>();
                foreach (var propInf in writeProps)
                {
                    try
                    {

                        if (propInf.CanWrite)
                        {

                            var val = tp.GetProperty(propInf.Name).GetValue(p_obj, null);
                            if (propInf.PropertyType == typeof(bool?) || propInf.PropertyType == typeof(bool))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((bool?)val));
                            else if (propInf.PropertyType == typeof(byte[]))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((byte[])val));
                            else if (propInf.PropertyType == typeof(DateTime?))
                            {
                               /* A beírandó DateTime -ra azt mondjuk, hogy UTC, így már nem konvertál az Azure */
                               if (val != null)
                                {
                                    var dt = (DateTime?)val;

                                    TableEntity.Properties.Add(propInf.Name, new EntityProperty(new DateTime(dt.Value.Ticks, DateTimeKind.Utc)));
                                }
                            }
                            else if (propInf.PropertyType == typeof(DateTime))
                            {
                                /* A beírandó DateTime -ra azt mondjuk, hogy UTC, így már nem konvertál az Azure */
                                if (val != null)
                                {
                                    var dt = (DateTime)val;
                                    TableEntity.Properties.Add(propInf.Name, new EntityProperty(new DateTime(dt.Ticks, DateTimeKind.Utc)));
                                }

                            }
                            else if (propInf.PropertyType == typeof(DateTimeOffset?) || propInf.PropertyType == typeof(DateTimeOffset))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((DateTimeOffset?)val));
                            else if (propInf.PropertyType == typeof(double?) || propInf.PropertyType == typeof(double))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((double?)val));
                            else if (propInf.PropertyType == typeof(Guid?) || propInf.PropertyType == typeof(Guid))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((Guid?)val));
                            else if (propInf.PropertyType == typeof(int?) || propInf.PropertyType == typeof(int))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((int?)val));
                            else if (propInf.PropertyType == typeof(long?) || propInf.PropertyType == typeof(long))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((long?)val));
                            else if (propInf.PropertyType == typeof(string))
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty((string)val));
                            else if (propInf.PropertyType.IsEnum)
                                TableEntity.Properties.Add(propInf.Name, new EntityProperty(val.ToString()));
                            else if (propInf.PropertyType.IsGenericType && (propInf.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                            {
                                var jsonString = JsonConvert.SerializeObject(val, jsonsettings);
                                var splitted = Utils.ChunkString(jsonString, SPLIT_STR_LEN);
                                int item = 0;
                                foreach (var str in splitted)
                                {
                                    TableEntity.Properties.Add(propInf.Name + "_" + item.ToString().PadLeft(2, '0'), new EntityProperty(str));
                                    item++;
                                }
                            }
                            else
                                throw new Exception("There is no conversion for any Azure types:" + propInf.PropertyType.Name);


                        }

                    }
                    catch (Exception ex)
                    {
                        throw;
                    }     //szebben megoldani!
                }
            }
            catch (Exception ex)
            {
                throw;

            }     //szebben megoldani
            TableEntity.Timestamp = DateTimeOffset.Now;
            return TableEntity;
        }


        /// <summary>
        /// tárolóobjektum kulcs -> string map
        /// Megj:Tiltott karakterek a kulcsban:
        /// https://msdn.microsoft.com/en-us/library/dd179338.aspx
        /// </summary>
        /// <param name="p_keyProp"></param>
        /// <param name="p_keyValue"></param>
        /// <returns></returns>
        public static string GetValidAzureKeyValue(Type p_type, object p_keyValue)
        {
            string key = "";
            if (p_type == null)
                throw new Exception("The value of key is null !");

            if (p_type == typeof(DateTime))
            {
                /* UTC-ben tárolunk */
                var d = new DateTime(((DateTime)p_keyValue).Ticks, DateTimeKind.Utc);
                key = d.ToString(IsoDateTimeFormat);
            }
            else
                key = p_keyValue.ToString();

            var sb = new StringBuilder();
            foreach (var c in key)
            {
                if (c != '/'
                            && c != '\\'
                            && c != '#'
                            && c != '/'
                            && c != '?'
                            && c != ','
                            && c != '.'
                            && !char.IsControl(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append("&@@" + ((int)c).ToString() + ";");
                }
            }
            return sb.ToString();
        }

        private object getFromDynamic<T>(DynamicTableEntity p_obj)
        {
            JsonSerializerSettings jsonsettings = new JsonSerializerSettings { DateFormatHandling = DateFormatHandling.IsoDateFormat };

            object result = Activator.CreateInstance(typeof(T));
            Type t = typeof(T);
            PropertyInfo PartitionKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
            if (PartitionKeyProp == null)
                throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");
            t.GetProperty(PartitionKeyProp.Name).SetValue(result, GetAzureKeyValue(PartitionKeyProp.PropertyType, p_obj.PartitionKey), null);


            PropertyInfo RowKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();
            if (RowKeyProp != null)
            {
                t.GetProperty(RowKeyProp.Name).SetValue(result, GetAzureKeyValue(RowKeyProp.PropertyType, p_obj.RowKey), null);
            }

            DateTimeKind kind = (DateTimeKind)(t.GetProperty("DateTimeKind").GetValue((object)result));

            PropertyInfo[] writeProps = t.GetProperties().Where(pi => (Attribute.IsDefined(pi, typeof(DataMemberAttribute)) &&
                                                                   !Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr)) &&
                                                                   !Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr)))).ToArray<PropertyInfo>();
            foreach (var propInf in writeProps)
            {
                try
                {
                    if (propInf.CanWrite &&
                        (p_obj.Properties.ContainsKey(propInf.Name) || p_obj.Properties.Where(w => w.Key.StartsWith(propInf.Name + "_")).Count() != 0))
                    {
                        //                                jsonSerialiser.ConvertToType(p_obj.Properties[propInf.Name].StringValue, propInf.PropertyType), null);


                        if (propInf.PropertyType.IsGenericType && (propInf.PropertyType.GetGenericTypeDefinition() == typeof(List<>)))
                        {
                            string strValue = "";
                            var lstProps = p_obj.Properties.Where(w => w.Key.StartsWith(propInf.Name + "_")).OrderBy(o => o.Key);
                            foreach (var prop in lstProps)
                            {
                                strValue += p_obj.Properties[prop.Key].StringValue;
                            }

                            t.GetProperty(propInf.Name).SetValue(result,
                                JsonConvert.DeserializeObject(strValue, propInf.PropertyType, jsonsettings), null);
                        }
                        else if (propInf.PropertyType.IsEnum && p_obj.Properties[propInf.Name].PropertyType == EdmType.String)
                            t.GetProperty(propInf.Name).SetValue(result,
                                Enum.Parse(propInf.PropertyType, p_obj.Properties[propInf.Name].StringValue), null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.String)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].StringValue, null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.Int64)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].Int64Value, null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.Int32)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].Int32Value, null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.DateTime)
                        {
                            var dt = (DateTime)p_obj.Properties[propInf.Name].DateTime;
                            t.GetProperty(propInf.Name).SetValue(result, new DateTime(dt.Ticks, kind), null);
                        }
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.Guid)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].GuidValue, null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.Boolean)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].BooleanValue, null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.Double)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].DoubleValue, null);
                        else if (p_obj.Properties[propInf.Name].PropertyType == EdmType.Binary)
                            t.GetProperty(propInf.Name).SetValue(result, p_obj.Properties[propInf.Name].BinaryValue, null);



                    }
                }
                catch (Exception ex) { throw; }     //szebben megoldani!
            }
            return result;
        }


        public static object GetAzureKeyValue(Type p_type, string p_keyValue)

        {

            string converted = WebUtility.HtmlDecode(p_keyValue.Replace("&@@", "&#"));

            if (p_type == typeof(Guid?) || p_type == typeof(Guid))
                return new Guid(converted);
            else if (p_type == typeof(int?) || p_type == typeof(int))
                return int.Parse(converted);
            else if (p_type == typeof(Int64?) || p_type == typeof(Int64))
                return Int64.Parse(converted);
            else if (p_type == typeof(DateTime?) || p_type == typeof(DateTime))
                return DateTime.Parse(converted);
            else
                return converted;
        }


        public string NextID()
        {
            long nextID = DateTime.UtcNow.Ticks;
            while (nextID == m_lastID)
            {
                Thread.Sleep(1);
                nextID = DateTime.UtcNow.Ticks;
            }
            string ID = "0000000000000000000" + nextID.ToString();
            m_lastID = nextID;
            return ID.Substring(ID.Length - 20, 20);
        }

        public bool DeleteTable(string p_tableName)
        {
            try
            {
                InitTableStore();
                CloudTable table = null;
                table = m_client.GetTableReference(p_tableName);
                bool bOK = table.DeleteIfExists();
                return bOK;
            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool Insert(object p_obj, string p_user)
        {
            try
            {

                InitTableStore();
                Type tp = p_obj.GetType();
                AzureTableObjBase.enObjectState oriState = AzureTableObjBase.enObjectState.New;
                if (tp.IsSubclassOf(typeof(AzureTableObjBase)))
                {
                    AzureTableObjBase mb = (AzureTableObjBase)p_obj;
                    oriState = mb.State;
                    mb.State = AzureTableObjBase.enObjectState.Stored;
                    mb.Created = (mb.DateTimeKind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
                    mb.Creator = p_user;            //TODO: usert beimportálni !
                }


                DynamicTableEntity dynObj = cloneForWrite(p_obj);
                CloudTable table = null;
                table = m_client.GetTableReference(p_obj.GetType().Name);
                table.CreateIfNotExists();

                TableOperation insertOperation = TableOperation.InsertOrReplace(dynObj);

                TableResult res = table.Execute(insertOperation);
                bool bOK = parseHttpStatus(res.HttpStatusCode);
                if (!bOK && tp.IsSubclassOf(typeof(AzureTableObjBase)))
                {
                    AzureTableObjBase mb = (AzureTableObjBase)p_obj;
                    mb.State = oriState;
                }

                return bOK;
            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        public void BatchInsertOrReplace<T>(List<T> entities, string p_user)
        {

            Type t = typeof(T);

            InitTableStore();

            CloudTable table = null;
            table = m_client.GetTableReference(t.Name);
            table.CreateIfNotExists();

            PropertyInfo PartitionKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
            if (PartitionKeyProp == null)
                throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");
            PropertyInfo RowKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();

            //egy batch művelet szegmensében azonos partition key szerepelhet
            //
            var groups = entities.GroupBy(g => GetValidAzureKeyValue(PartitionKeyProp.PropertyType, t.GetProperty(PartitionKeyProp.Name).GetValue(g, null)));

            foreach (var group in groups)
            {



                int rowOffset = 0;

                var tasks = new List<Task>();
                var groupEntities = group.ToList();
                while (rowOffset < groupEntities.Count)
                {
                    // next batch
                    var rows = groupEntities.Skip(rowOffset).Take(BATCHSIZE).ToList();

                    rowOffset += rows.Count;

                    var batch = new TableBatchOperation();
                    Dictionary<T, AzureTableObjBase.enObjectState> lstStates = new Dictionary<T, AzureTableObjBase.enObjectState>();

                    foreach (var row in rows)
                    {
                        AzureTableObjBase.enObjectState oriState = AzureTableObjBase.enObjectState.New;
                        if (t.IsSubclassOf(typeof(AzureTableObjBase)))
                        {
                            object obj = (T)row;
                            AzureTableObjBase mb = (AzureTableObjBase)obj;
                            oriState = mb.State;
                            mb.State = AzureTableObjBase.enObjectState.Stored;
                            lstStates.Add(row, oriState);


                            if (oriState == AzureTableObjBase.enObjectState.New)
                            {
                                mb.Created = (mb.DateTimeKind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
                                mb.Creator = p_user;
                            }
                            if (oriState == AzureTableObjBase.enObjectState.Modified)
                            {
                                mb.Updated = (mb.DateTimeKind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
                                mb.Updater = p_user;
                            }


                        }
                        DynamicTableEntity dynObj = cloneForWrite(row);
                        batch.InsertOrReplace(dynObj);

                    }

                    // submit
                    var results = table.ExecuteBatch(batch);
                    foreach (var res in results)
                    {
                        bool bOK = parseHttpStatus(res.HttpStatusCode);
                        if (!bOK && t.IsSubclassOf(typeof(AzureTableObjBase)))
                        {
                            DynamicTableEntity dt = (DynamicTableEntity)res.Result;
                            var item = (T)rows.Where(w => GetValidAzureKeyValue(PartitionKeyProp.PropertyType, t.GetProperty(PartitionKeyProp.Name).GetValue(w, null)) == dt.PartitionKey &&
                               (RowKeyProp != null ? GetValidAzureKeyValue(RowKeyProp.PropertyType, t.GetProperty(RowKeyProp.Name).GetValue(w, null)) == dt.RowKey : true)).FirstOrDefault();


                            if (lstStates.ContainsKey(item))
                            {
                                object obj = item;
                                AzureTableObjBase mb = (AzureTableObjBase)obj;
                                mb.State = lstStates[item];
                            }
                        }
                    }
                }
            }
        }

        public bool Modify(object p_obj, string p_user)
        {
            try
            {
                InitTableStore();
                Type tp = p_obj.GetType();
                AzureTableObjBase.enObjectState oriState = AzureTableObjBase.enObjectState.Modified;
                if (tp.IsSubclassOf(typeof(AzureTableObjBase)))
                {
                    AzureTableObjBase mb = (AzureTableObjBase)p_obj;
                    oriState = mb.State;
                    mb.State = AzureTableObjBase.enObjectState.Stored;
                    mb.Updated = (mb.DateTimeKind == DateTimeKind.Utc ? DateTime.UtcNow : DateTime.Now);
                    mb.Updater = p_user;
                    if (mb.Created == DateTime.MinValue)
                    {
                        mb.Created = mb.Updated;
                    }

                }


                DynamicTableEntity dynObj = cloneForWrite(p_obj);
                dynObj.ETag = "*";
                CloudTable table = null;
                table = m_client.GetTableReference(p_obj.GetType().Name);
                table.CreateIfNotExists();
                TableOperation modifyOperation = TableOperation.Replace(dynObj);
                TableResult res = table.Execute(modifyOperation);
                bool bOK = parseHttpStatus(res.HttpStatusCode);
                if (!bOK && tp.IsSubclassOf(typeof(AzureTableObjBase)))
                {
                    AzureTableObjBase mb = (AzureTableObjBase)p_obj;
                    mb.State = oriState;
                }

                return bOK;

            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
                //                return false;
            }
            catch (Exception ex)
            {
                throw;
            }
        }


        //http://stackoverflow.com/questions/22684881/batch-delete-in-windows-azure-table-storage
        public bool Delete(object p_obj)
        {
            try
            {
                InitTableStore();
                Type tp = p_obj.GetType();
                AzureTableObjBase.enObjectState oriState = AzureTableObjBase.enObjectState.Stored;
                if (tp.IsSubclassOf(typeof(AzureTableObjBase)))
                {
                    AzureTableObjBase mb = (AzureTableObjBase)p_obj;
                    oriState = mb.State;

                    mb.State = AzureTableObjBase.enObjectState.Inactive;
                }
                DynamicTableEntity dynObj = cloneForWrite(p_obj);
                dynObj.ETag = "*";

                CloudTable table = null;
                table = m_client.GetTableReference(p_obj.GetType().Name);
                table.CreateIfNotExists();
                TableOperation deleteOperation = TableOperation.Delete(dynObj);
                TableResult res = table.Execute(deleteOperation);
                bool bOK = parseHttpStatus(res.HttpStatusCode);
                if (!bOK && tp.IsSubclassOf(typeof(AzureTableObjBase)))
                {
                    AzureTableObjBase mb = (AzureTableObjBase)p_obj;
                    mb.State = oriState;
                }
                return bOK;
            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
                //                return false;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool DeleteRange<T>(List<AzureItemKeys> p_itemKeys)
        {
            try
            {
                InitTableStore();

                Type t = typeof(T);

                PropertyInfo PartitionKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
                if (PartitionKeyProp == null)
                    throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");

                PropertyInfo RowKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();

                p_itemKeys.ForEach(x => {
                    x.PartitionKey = GetAzureKeyValue(PartitionKeyProp.PropertyType, x.PartitionKey).ToString();
                    x.RowKey = (RowKeyProp == null || x.RowKey == null ? x.RowKey : GetAzureKeyValue(RowKeyProp.PropertyType, x.RowKey).ToString());
                });


                //egy batch művelet szegmensében azonos partition key szerepelhet
                //
                var groups = p_itemKeys.GroupBy(g => g.PartitionKey);

                foreach (var group in groups)
                {


                    CloudTable table = null;
                    table = m_client.GetTableReference(t.Name);
                    table.CreateIfNotExists();

                    Action<IEnumerable<DynamicTableEntity>> processor = entities =>
                    {
                        var batches = new Dictionary<string, TableBatchOperation>();

                        foreach (var entity in entities)
                        {
                            TableBatchOperation batch = null;

                            if (batches.TryGetValue(entity.RowKey, out batch) == false)
                            {
                                batches[entity.RowKey] = batch = new TableBatchOperation();
                            }

                            batch.Add(TableOperation.Delete(entity));

                            if (batch.Count == BATCHSIZE)
                            {
                                table.ExecuteBatch(batch);
                                batches[entity.RowKey] = new TableBatchOperation();
                            }
                        }

                        foreach (var batch in batches.Values)
                        {
                            if (batch.Count > 0)
                            {
                                table.ExecuteBatch(batch);
                            }
                        }
                    };

                    BatchProcessEntities(table, processor, group.ToList());
                }
            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
                //                return false;
            }
            catch (Exception ex)
            {
                throw;
            }
            return true;
        }

        private void BatchProcessEntities(CloudTable table, Action<IEnumerable<DynamicTableEntity>> processor, List<AzureItemKeys> p_itemKeys)
        {
            TableQuerySegment<DynamicTableEntity> segment = null;

            int round = 0;
            while (segment == null || segment.ContinuationToken != null)
            {
                string segmentFilter = null;
                foreach (var itm in p_itemKeys.GetRange(round * BATCHSIZE, p_itemKeys.Count - (round * BATCHSIZE) >= BATCHSIZE ? BATCHSIZE : p_itemKeys.Count - (round * BATCHSIZE)).ToList())
                {
                    string tmpFilter;
                    if (itm.RowKey != null)
                    {
                        tmpFilter = TableQuery.CombineFilters(
                                              TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, itm.PartitionKey),
                                              TableOperators.And,
                                              TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, itm.RowKey));
                    }
                    else
                    {
                        tmpFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, itm.PartitionKey);
                    }

                    segmentFilter = segmentFilter != null ? TableQuery.CombineFilters(tmpFilter, TableOperators.Or, segmentFilter) : tmpFilter;
                }


                var query = new TableQuery<DynamicTableEntity>().Where(segmentFilter).Take(BATCHSIZE);
                segment = table.ExecuteQuerySegmented<DynamicTableEntity>(query, segment == null ? null : segment.ContinuationToken);

                processor(segment.Results);
            }
        }


        public T Retrieve<T>(T p_obj)
        {
            object partitionKey = "";
            object rowKey = "";
            Type tp = p_obj.GetType();
            PropertyInfo PartitionKeyProp = tp.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
            if (PartitionKeyProp == null)
                throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");
            partitionKey = tp.GetProperty(PartitionKeyProp.Name).GetValue(p_obj, null);

            PropertyInfo RowKeyProp = tp.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();
            if (RowKeyProp != null)
                rowKey = tp.GetProperty(RowKeyProp.Name).GetValue(p_obj, null);
            return Retrieve<T>(partitionKey, rowKey);
        }

        public T Retrieve<T>(object p_partitionKey, object p_rowKey)
        {
            try
            {
                InitTableStore();
                CloudTable table = null;
                table = m_client.GetTableReference(typeof(T).Name);
                table.CreateIfNotExists();

                Type t = typeof(T);

                PropertyInfo PartitionKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
                if (PartitionKeyProp == null)
                    throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");
                PropertyInfo RowKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();

                TableOperation retrieveOperation = TableOperation.Retrieve<DynamicTableEntity>(
                                GetValidAzureKeyValue(PartitionKeyProp.PropertyType, p_partitionKey),
                                GetValidAzureKeyValue(RowKeyProp == null ? typeof(string) : RowKeyProp.PropertyType, p_rowKey));
                TableResult res = table.Execute(retrieveOperation);
                if (parseHttpStatus(res.HttpStatusCode) && res.Result != null)
                {
                    var o = getFromDynamic<T>((DynamicTableEntity)res.Result);
                    return (T)Convert.ChangeType(o, typeof(T)); ;
                }
                else
                    return default(T);
            }
            catch (StorageException sex)
            {
                return default(T);
            }
            catch (Exception ex)
            {
                throw;
            }

        }

        // Nagy tömegű, szegmentált lekérdezés:http://stackoverflow.com/questions/33162412/can-you-expose-azure-table-storage-iqueryable-table-createquery-as-poco

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// 


        public List<T> RetrieveList<T>()
        {
            int Total = 0;
            return RetrieveList<T>("", "", out Total);
        }

        public List<T> RetrieveList<T>(string p_where)
        {
            int Total = 0;
            return RetrieveList<T>(p_where, "", out Total);
        }

        public List<T> RetrieveList<T>(string p_where, out int Total)
        {
            return RetrieveList<T>(p_where, "", out Total);
        }

        public List<T> RetrieveList<T>(string p_where, string p_orderBy, out int Total, int pageSize = 0, int page = 1)
        {
            List<T> lstResult = new List<T>();
            try
            {
                InitTableStore();
                CloudTable table = null;
                table = m_client.GetTableReference(typeof(T).Name);
                table.CreateIfNotExists();

                TableQuery<DynamicTableEntity> query;
                if (p_where != "")
                {
                    p_where = FixTableStorageWhere<T>(p_where);
                    query = new TableQuery<DynamicTableEntity>().Where(p_where);
                }
                else
                {
                    query = new TableQuery<DynamicTableEntity>();
                }
                var res = table.ExecuteQuery(query);
                if (res.Any())
                {
                    foreach (DynamicTableEntity item in res)
                    {
                        var o = getFromDynamic<T>((DynamicTableEntity)item);
                        lstResult.Add((T)Convert.ChangeType(o, typeof(T)));
                    }
                }

                if (p_orderBy != "")
                {
                    var ordering = p_orderBy.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    if (ordering.Length > 1 && ordering[1].ToLower() == "desc")
                        lstResult = lstResult.OrderByDescending(x => x.GetType().GetProperty(ordering[0]).GetValue(x, null)).ToList();
                    else
                        lstResult = lstResult.OrderBy(x => x.GetType().GetProperty(ordering[0]).GetValue(x, null)).ToList();
                }
                Total = lstResult.Count;
                if (pageSize > 0)
                {
                    lstResult = lstResult.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                }
            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
            }
            catch (Exception ex)
            {
                throw;
            }
            return lstResult;
        }


        public ObservableCollection<T> RetrieveObservableList<T>(out int Total)
        {
            return RetrieveObservableList<T>("", "", out Total);
        }

        public ObservableCollection<T> RetrieveObservableList<T>(string p_where, out int Total)
        {
            return RetrieveObservableList<T>(p_where, "", out Total);
        }
        public ObservableCollection<T> RetrieveObservableList<T>(string p_where, string p_orderBy, out int Total, int pageSize = 0)
        {
            var res = RetrieveList<T>(p_where, p_orderBy, out Total, pageSize);
            if (res != null)
                return new ObservableCollection<T>(res);
            return null;
        }


        public List<T> RetrieveRange<T>(List<AzureItemKeys> p_itemKeys)
        {
            List<T> result = new List<T>();
            try
            {
                InitTableStore();

                Type t = typeof(T);

                PropertyInfo PartitionKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTablePartitionKeyAttr))).FirstOrDefault();
                if (PartitionKeyProp == null)
                    throw new Exception("AzureTablePartitionKeyAttr annotation is missing!");

                PropertyInfo RowKeyProp = t.GetProperties().Where(pi => Attribute.IsDefined(pi, typeof(AzureTableRowKeyAttr))).FirstOrDefault();

                //A kulcsban tiltott karakterek átalakítása
                p_itemKeys.ForEach(x => {
                    x.PartitionKey = GetAzureKeyValue(PartitionKeyProp.PropertyType, x.PartitionKey).ToString();
                    x.RowKey = (RowKeyProp == null ? x.RowKey : GetAzureKeyValue(RowKeyProp.PropertyType, x.RowKey).ToString());
                });

                //egy batch művelet szegmensében azonos partition key szerepelhet
                //
                var groups = p_itemKeys.GroupBy(g => g.PartitionKey);

                foreach (var group in groups)
                {


                    CloudTable table = null;
                    table = m_client.GetTableReference(t.Name);
                    table.CreateIfNotExists();

                    Action<IEnumerable<DynamicTableEntity>> processor = entities =>
                    {
                        var batches = new Dictionary<string, TableBatchOperation>();

                        foreach (var entity in entities)
                        {
                            TableBatchOperation batch = null;

                            if (batches.TryGetValue(entity.RowKey, out batch) == false)
                            {
                                batches[entity.RowKey] = batch = new TableBatchOperation();
                            }

                            batch.Add(TableOperation.Retrieve(entity.PartitionKey, entity.RowKey));

                            if (batch.Count == BATCHSIZE)
                            {
                                table.ExecuteBatch(batch);
                                batches[entity.RowKey] = new TableBatchOperation();
                            }
                        }

                        foreach (var batch in batches.Values)
                        {
                            if (batch.Count > 0)
                            {
                                var lstRes = table.ExecuteBatch(batch);
                                foreach (var res in lstRes)
                                {
                                    if (parseHttpStatus(res.HttpStatusCode) && res.Result != null)
                                    {
                                        var o = getFromDynamic<T>((DynamicTableEntity)res.Result);
                                        result.Add((T)Convert.ChangeType(o, typeof(T)));
                                    }
                                    else
                                        throw new Exception("AzureTableStore.RetrieveRange error. Code:" + res.HttpStatusCode);

                                }
                            }
                        }
                    };
                    BatchProcessEntities(table, processor, group.ToList());
                }
            }
            catch (StorageException sex)
            {
                throw new Exception(GetStorageExceptionMessage(sex));
                //                return false;
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;
        }

        
        public static string DateFilter(DateTime p_dt)
        {
            return "datetime'" + p_dt.ToString("yyyy-MM-ddTHH:mm:ss") + "'";
        }

        private static string FixTableStorageWhere<T>(string p_where)
        {
            p_where = KendoFilterToTableStoreFilter<T>(p_where);
            if (p_where.Contains("datetime"))
            {
                p_where = p_where.Replace("'''", "'");
                p_where = p_where.Replace("'datetime''", "datetime'");
            }

            if (p_where.Contains("guid"))
            {
                p_where = p_where.Replace("'''", "'");
                p_where = p_where.Replace("'guid''", "guid'");
            }

            return p_where;
        }

        private static string GetStorageExceptionMessage(StorageException p_sex)
        {
            var requestInformation = p_sex.RequestInformation;
            var information = requestInformation.ExtendedErrorInformation;
            var errorCode = information.ErrorCode;
            return String.Format("({0}) {1}", errorCode, information.ErrorMessage);
        }

        public static string KendoFilterToTableStoreFilter<T>(string filter)
        {
            string pattern = @"((?'field'\w+)~(?'op'\w+)~(?'value'[\w'-]+))(~(?'and'and)~)?";

            filter = Regex.Replace(filter, pattern, delegate (Match match)
            {
                string field = match.Groups["field"].Value;
                string op = match.Groups["op"].Value;
                string value = match.Groups["value"].Value;
                string and = match.Groups["and"].Value;
                var pInfo = typeof(T).GetProperties().Where(f => f.Name == field).SingleOrDefault();
                var azureField = pInfo?.GetCustomAttribute<AzureTableFieldAttr>();
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
