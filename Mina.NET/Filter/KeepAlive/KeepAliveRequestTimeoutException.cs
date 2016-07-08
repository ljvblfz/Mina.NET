using System;
using System.Runtime.Serialization;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// An exception which is thrown when a keep-alive response
    /// message was not received within a certain timeout.
    /// </summary>
    [Serializable]
    public class KeepAliveRequestTimeoutException : Exception
    {
        /// <summary>
        /// </summary>
        public KeepAliveRequestTimeoutException()
        {
        }

        /// <summary>
        /// </summary>
        public KeepAliveRequestTimeoutException(string message) : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public KeepAliveRequestTimeoutException(string message, Exception inner) : base(message, inner)
        {
        }

        /// <summary>
        /// </summary>
        protected KeepAliveRequestTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
