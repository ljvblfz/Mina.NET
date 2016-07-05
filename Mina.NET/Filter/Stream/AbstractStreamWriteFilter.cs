using System;
using System.Collections.Concurrent;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Stream
{
    public abstract class AbstractStreamWriteFilter<T> : IOFilterAdapter
        where T : class
    {
        /// <summary>
        /// The default buffer size this filter uses for writing.
        /// </summary>
        public const int DefaultStreamBufferSize = 4096;

        private int _writeBufferSize = DefaultStreamBufferSize;
        protected readonly AttributeKey CurrentStream;
        protected readonly AttributeKey WriteRequestQueue;
        protected readonly AttributeKey CurrentWriteRequest;

        protected AbstractStreamWriteFilter()
        { 
            CurrentStream = new AttributeKey(GetType(), "stream");
            WriteRequestQueue = new AttributeKey(GetType(), "queue");
            CurrentWriteRequest = new AttributeKey(GetType(), "writeRequest");
        }

        /// <summary>
        /// Gets or sets the size of the write buffer in bytes. Data will be read from the
        /// stream in chunks of this size and then written to the next filter.
        /// </summary>
        public int WriteBufferSize
        {
            get { return _writeBufferSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentException("WriteBufferSize must be at least 1");
                _writeBufferSize = value;
            }
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            if (parent.Contains(GetType()))
                throw new InvalidOperationException("Only one " + GetType().Name + " is permitted.");
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            // If we're already processing a stream we need to queue the WriteRequest.
            if (session.GetAttribute(CurrentStream) != null)
            {
                var queue = GetWriteRequestQueue(session);
                queue.Enqueue(writeRequest);
                return;
            }

            var stream = writeRequest.Message as T;

            if (stream == null)
            {
                base.FilterWrite(nextFilter, session, writeRequest);
            }
            else
            {
                var buffer = GetNextBuffer(stream);
                if (buffer == null)
                {
                    // EOF
                    writeRequest.Future.Written = true;
                    nextFilter.MessageSent(session, writeRequest);
                }
                else
                {
                    session.SetAttribute(CurrentStream, stream);
                    session.SetAttribute(CurrentWriteRequest, writeRequest);

                    nextFilter.FilterWrite(session, new DefaultWriteRequest(buffer));
                }
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var stream = session.GetAttribute(CurrentStream) as T;

            if (stream == null)
            {
                base.MessageSent(nextFilter, session, writeRequest);
            }
            else
            {
                var buffer = GetNextBuffer(stream);

                if (buffer == null)
                {
                    // EOF
                    session.RemoveAttribute(CurrentStream);
                    var currentWriteRequest = (IWriteRequest)session.RemoveAttribute(CurrentWriteRequest);

                    // Write queued WriteRequests.
                    var queue = RemoveWriteRequestQueue(session);
                    if (queue != null)
                    {
                        IWriteRequest wr;
                        while (queue.TryDequeue(out wr))
                        {
                            FilterWrite(nextFilter, session, wr);
                        }
                    }

                    currentWriteRequest.Future.Written = true;
                    nextFilter.MessageSent(session, currentWriteRequest);
                }
                else
                {
                    nextFilter.FilterWrite(session, new DefaultWriteRequest(buffer));
                }
            }
        }

        protected abstract IOBuffer GetNextBuffer(T message);

        private ConcurrentQueue<IWriteRequest> GetWriteRequestQueue(IOSession session)
        {
            var queue = session.GetAttribute<ConcurrentQueue<IWriteRequest>>(WriteRequestQueue);
            if (queue == null)
            {
                queue = new ConcurrentQueue<IWriteRequest>();
                session.SetAttribute(WriteRequestQueue, queue);
            }
            return queue;
        }

        private ConcurrentQueue<IWriteRequest> RemoveWriteRequestQueue(IOSession session)
        {
            return (ConcurrentQueue<IWriteRequest>)session.RemoveAttribute(WriteRequestQueue);
        }
    }
}
