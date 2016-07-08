using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write operations were failed.
    /// </summary>
    [Serializable]
    public class WriteException : IOException
    {
        private readonly IList<IWriteRequest> _requests;

        public WriteException(IWriteRequest request)
        {
            _requests = AsRequestList(request);
        }

        public WriteException(IEnumerable<IWriteRequest> requests)
        {
            _requests = AsRequestList(requests);
        }

        protected WriteException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public IWriteRequest Request => _requests[0];

        public IEnumerable<IWriteRequest> Requests => _requests;

        private static IList<IWriteRequest> AsRequestList(IWriteRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var requests = new List<IWriteRequest>(1);
            requests.Add(request);
            return requests.AsReadOnly();
        }

        private static IList<IWriteRequest> AsRequestList(IEnumerable<IWriteRequest> requests)
        {
            if (requests == null)
            {
                throw new ArgumentNullException(nameof(requests));
            }
            var newRequests = new List<IWriteRequest>(requests);
            return newRequests.AsReadOnly();
        }
    }
}
