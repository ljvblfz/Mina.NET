using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write operations were
    /// attempted on a closed session.
    /// </summary>
    [Serializable]
    public class WriteToClosedSessionException : WriteException
    {
        /// <summary>
        /// </summary>
        public WriteToClosedSessionException(IWriteRequest request)
            : base(request)
        {
        }

        /// <summary>
        /// </summary>
        public WriteToClosedSessionException(IEnumerable<IWriteRequest> requests)
            : base(requests)
        {
        }

        /// <summary>
        /// </summary>
        protected WriteToClosedSessionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
