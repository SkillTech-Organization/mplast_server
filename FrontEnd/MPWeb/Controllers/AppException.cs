using System;
using System.Runtime.Serialization;

namespace MPWeb.Controllers
{
    [Serializable]
    internal class AppException : Exception
    {
        private dynamic correlationId;
        private Exception ex;
        private object logger;

        public AppException()
        {
        }

        public AppException(string message) : base(message)
        {
        }

        public AppException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public AppException(dynamic correlationId, Exception ex, object logger)
        {
            this.correlationId = correlationId;
            this.ex = ex;
            this.logger = logger;
        }

        protected AppException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}