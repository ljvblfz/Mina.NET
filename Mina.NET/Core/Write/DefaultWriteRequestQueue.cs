using System.Collections.Concurrent;
using Mina.Core.Session;

namespace Mina.Core.Write
{
    class DefaultWriteRequestQueue : IWriteRequestQueue
    {
        private ConcurrentQueue<IWriteRequest> _q = new ConcurrentQueue<IWriteRequest>();

        public int Size => _q.Count;

        public IWriteRequest Poll(IOSession session)
        {
            IWriteRequest request;
            _q.TryDequeue(out request);
            return request;
        }

        public void Offer(IOSession session, IWriteRequest writeRequest)
        {
            _q.Enqueue(writeRequest);
        }

        public bool IsEmpty(IOSession session)
        {
            return _q.IsEmpty;
        }

        public void Clear(IOSession session)
        {
            _q = new ConcurrentQueue<IWriteRequest>();
        }

        public void Dispose(IOSession session)
        {
            // Do nothing
        }
    }
}
