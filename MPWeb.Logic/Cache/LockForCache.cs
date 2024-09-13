using MPWeb.Common.Lock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPWeb.Logic.Cache
{
    public class LockForCache : LockHolder<object>
    {
        public LockForCache(object handle, int milliSecondTimeout)
            : base(handle, milliSecondTimeout)
        {

        }

        public LockForCache(object handle)
            : base(handle)
        {
        }
    }
}
