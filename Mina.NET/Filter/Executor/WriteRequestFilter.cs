using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Attaches an <see cref="IOEventQueueHandler"/> to an <see cref="IOSession"/>'s
    /// <see cref="IWriteRequest"/> queue to provide accurate write queue status tracking.
    /// </summary>
    public class WriteRequestFilter : IOFilterAdapter
    {
        /// <summary>
        /// Instantiates with an <see cref="IoEventQueueThrottle"/>.
        /// </summary>
        public WriteRequestFilter()
            : this(new IoEventQueueThrottle())
        { }

        /// <summary>
        /// Instantiates with the given <see cref="IOEventQueueHandler"/>.
        /// </summary>
        /// <param name="queueHandler">the handler</param>
        public WriteRequestFilter(IOEventQueueHandler queueHandler)
        {
            if (queueHandler == null)
                throw new ArgumentNullException(nameof(queueHandler));
            QueueHandler = queueHandler;
        }

        /// <summary>
        /// Gets the <see cref="IOEventQueueHandler"/>.
        /// </summary>
        public IOEventQueueHandler QueueHandler { get; }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var ioe = new IoEvent(IoEventType.Write, session, writeRequest);
            if (QueueHandler.Accept(this, ioe))
            {
                nextFilter.FilterWrite(session, writeRequest);
                var writeFuture = writeRequest.Future;
                if (writeFuture == null)
                    return;

                // We can track the write request only when it has a future.
                QueueHandler.Offered(this, ioe);
                writeFuture.Complete += (s, e) => QueueHandler.Polled(this, ioe);
            }
        }
    }
}
