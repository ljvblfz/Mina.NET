using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Mina.Core.Session;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when write buffer is not flushed for
    /// <see cref="IOSessionConfig.WriteTimeout"/> seconds.
    /// </summary>
    [Serializable]
    public class WriteTimeoutException : WriteException
    {
        /// <summary>
        /// </summary>
        public WriteTimeoutException(IWriteRequest request)
            : base(request)
        {
        }

        /// <summary>
        /// </summary>
        public WriteTimeoutException(IEnumerable<IWriteRequest> requests)
            : base(requests)
        {
        }

        /// <summary>
        /// </summary>
        protected WriteTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
