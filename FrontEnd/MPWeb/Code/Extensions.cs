using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace MPWeb.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Converts Stream to Byte array
        /// </summary>
        public static byte[] ToBytes(this Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            return memoryStream.ToArray();
        }

    }
}