using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// A <see cref="IOEventExecutor"/> that does not maintain the order of <see cref="IoEvent"/>s.
    /// This means more than one event handler methods can be invoked at the same time with mixed order.
    /// If you need to maintain the order of events per session, please use
    /// <see cref="OrderedThreadPoolExecutor"/>.
    /// </summary>
    public class UnorderedThreadPoolExecutor : ThreadPoolExecutor, IOEventExecutor
    {
        /// <summary>
        /// Instantiates with a <see cref="NoopIoEventQueueHandler"/>.
        /// </summary>
        public UnorderedThreadPoolExecutor()
            : this(null)
        { }

        /// <summary>
        /// Instantiates with the given <see cref="IOEventQueueHandler"/>.
        /// </summary>
        /// <param name="queueHandler">the handler</param>
        public UnorderedThreadPoolExecutor(IOEventQueueHandler queueHandler)
        {
            QueueHandler = queueHandler == null ? NoopIoEventQueueHandler.Instance : queueHandler;
        }

        /// <summary>
        /// Gets the <see cref="IOEventQueueHandler"/>.
        /// </summary>
        public IOEventQueueHandler QueueHandler { get; }

        /// <inheritdoc/>
        public void Execute(IoEvent ioe)
        {
            var offeredEvent = QueueHandler.Accept(this, ioe);
            if (offeredEvent)
            {
                Execute(() =>
                {
                    QueueHandler.Polled(this, ioe);
                    ioe.Fire();
                });

                QueueHandler.Offered(this, ioe);
            }
        }
    }
}
