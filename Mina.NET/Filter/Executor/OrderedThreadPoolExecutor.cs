using System.Collections.Concurrent;
using System.Text;
using Common.Logging;
using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// A <see cref="IOEventExecutor"/> that maintains the order of <see cref="IOEvent"/>s.
    /// If you don't need to maintain the order of events per session, please use
    /// <see cref="UnorderedThreadPoolExecutor"/>.
    /// </summary>
    public class OrderedThreadPoolExecutor : ThreadPoolExecutor, IOEventExecutor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(OrderedThreadPoolExecutor));

        /// <summary>
        /// A key stored into the session's attribute for the event tasks being queued
        /// </summary>
        private readonly AttributeKey _tasksQueue = new AttributeKey(typeof(OrderedThreadPoolExecutor), "tasksQueue");

        /// <summary>
        /// Instantiates with a <see cref="NoopIoEventQueueHandler"/>.
        /// </summary>
        public OrderedThreadPoolExecutor()
            : this(null)
        { }

        /// <summary>
        /// Instantiates with the given <see cref="IOEventQueueHandler"/>.
        /// </summary>
        /// <param name="queueHandler">the handler</param>
        public OrderedThreadPoolExecutor(IOEventQueueHandler queueHandler)
        {
            QueueHandler = queueHandler == null ? NoopIOEventQueueHandler.Instance : queueHandler;
        }

        /// <summary>
        /// Gets the <see cref="IOEventQueueHandler"/>.
        /// </summary>
        public IOEventQueueHandler QueueHandler { get; }

        /// <inheritdoc/>
        public void Execute(IOEvent ioe)
        {
            var session = ioe.Session;
            var sessionTasksQueue = GetSessionTasksQueue(session);
            bool exec;

            // propose the new event to the event queue handler. If we
            // use a throttle queue handler, the message may be rejected
            // if the maximum size has been reached.
            var offerEvent = QueueHandler.Accept(this, ioe);

            if (offerEvent)
            {
                lock (sessionTasksQueue.SyncRoot)
                {
                    sessionTasksQueue.TasksQueue.Enqueue(ioe);

                    if (sessionTasksQueue.ProcessingCompleted)
                    {
                        sessionTasksQueue.ProcessingCompleted = false;
                        exec = true;
                    }
                    else
                    {
                        exec = false;
                    }

                    if (Log.IsDebugEnabled)
                        Print(sessionTasksQueue.TasksQueue, ioe);
                }

                if (exec)
                {
                    Execute(() =>
                    {
                        RunTasks(sessionTasksQueue);
                    });
                }

                QueueHandler.Offered(this, ioe);
            }
        }

        private SessionTasksQueue GetSessionTasksQueue(IOSession session)
        {
            var queue = session.GetAttribute<SessionTasksQueue>(_tasksQueue);

            if (queue == null)
            {
                queue = new SessionTasksQueue();
                var oldQueue = (SessionTasksQueue)session.SetAttributeIfAbsent(_tasksQueue, queue);
                if (oldQueue != null)
                    queue = oldQueue;
            }

            return queue;
        }

        private void RunTasks(SessionTasksQueue sessionTasksQueue)
        {
            IOEvent ioe;
            while (true)
            {
                lock (sessionTasksQueue.SyncRoot)
                {
                    if (!sessionTasksQueue.TasksQueue.TryDequeue(out ioe))
                    {
                        sessionTasksQueue.ProcessingCompleted = true;
                        break;
                    }
                }

                QueueHandler.Polled(this, ioe);
                ioe.Fire();
            }
        }

        private void Print(ConcurrentQueue<IOEvent> queue, IOEvent ioe)
        {
            var sb = new StringBuilder();
            sb.Append("Adding event ")
                .Append(ioe.EventType)
                .Append(" to session ")
                .Append(ioe.Session.Id);
            var first = true;
            sb.Append("\nQueue : [");
            foreach (var elem in queue)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(elem.EventType).Append(", ");
            }
            sb.Append("]\n");
            Log.Debug(sb.ToString());
        }

        class SessionTasksQueue
        {
            public readonly object SyncRoot = new byte[0];
            /// <summary>
            /// A queue of ordered event waiting to be processed
            /// </summary>
            public readonly ConcurrentQueue<IOEvent> TasksQueue = new ConcurrentQueue<IOEvent>();
            /// <summary>
            /// The current task state
            /// </summary>
            public bool ProcessingCompleted = true;
        }
    }
}
