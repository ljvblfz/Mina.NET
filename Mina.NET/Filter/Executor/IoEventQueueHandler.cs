using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Listens and filters all event queue operations occurring in
    /// <see cref="OrderedThreadPoolExecutor"/> and <see cref="UnorderedThreadPoolExecutor"/>.
    /// </summary>
    public interface IOEventQueueHandler
    {
        /// <summary>
        /// Returns <tt>true</tt> if and only if the specified <tt>event</tt> is
        /// allowed to be offered to the event queue.  The <tt>event</tt> is dropped
        /// if <tt>false</tt> is returned.
        /// </summary>
        bool Accept(object source, IOEvent ioe);
        /// <summary>
        /// Invoked after the specified <paramref name="ioe"/> has been offered to the event queue.
        /// </summary>
        void Offered(object source, IOEvent ioe);
        /// <summary>
        /// Invoked after the specified <paramref name="ioe"/> has been polled to the event queue.
        /// </summary>
        void Polled(object source, IOEvent ioe);
    }

    class NoopIoEventQueueHandler : IOEventQueueHandler
    {
        public static readonly NoopIoEventQueueHandler Instance = new NoopIoEventQueueHandler();

        private NoopIoEventQueueHandler()
        { }

        public bool Accept(object source, IOEvent ioe)
        {
            return true;
        }

        public void Offered(object source, IOEvent ioe)
        {
            // NOOP
        }

        public void Polled(object source, IOEvent ioe)
        {
            // NOOP
        }
    }
}
