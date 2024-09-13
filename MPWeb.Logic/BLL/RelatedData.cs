using AzureTableStore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.BLL
{
    public class RelatedData : IRelatedData
    {
        private Dictionary<Type, List<AzureTableObjBase>> m_dataStore;

        public RelatedData()
        {
            m_dataStore = new Dictionary<Type, List<AzureTableObjBase>>();
        }
        List<T> IRelatedData.GetRelatedData<T>(bool p_createIfNotExist)
        {
            if (m_dataStore.ContainsKey(typeof(T)))
                return m_dataStore[typeof(T)].Cast<T>().ToList();

            if (p_createIfNotExist)
            {
                var retData = AzureTableStore.AzureAccess.Instance.RetrieveList<T>();
                m_dataStore.Add(typeof(T), retData.Cast<AzureTableObjBase>().ToList());

                return retData;
            }
            return null;
        }

        void IRelatedData.AddItem(AzureTableObjBase p_obj)
        {
            Type type = p_obj.GetType();
            if (m_dataStore.ContainsKey(type))
            {
                m_dataStore[type].Add(p_obj);
            }
            else
            {
                Type listType = typeof(List<>).MakeGenericType(new[] { type });
                IList list = (IList)Activator.CreateInstance(listType);
                list.Add(p_obj);
                m_dataStore.Add(p_obj.GetType(), list.Cast<AzureTableObjBase>().ToList());
            }
  
        }

    }
}
