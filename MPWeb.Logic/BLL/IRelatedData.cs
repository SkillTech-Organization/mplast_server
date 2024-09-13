using AzureTableStore;
using System.Collections.Generic;

namespace MPWeb.Logic.BLL
{
    public interface IRelatedData
    {
        List<T> GetRelatedData<T>(bool p_createIfNotExist = true);
        void AddItem(AzureTableObjBase p_obj );
    }
}
