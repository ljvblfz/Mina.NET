using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// A filter that forwards I/O events to <see cref="IOEventExecutor"/> to enforce a certain
    /// thread model while allowing the events per session to be processed
    /// simultaneously. You can apply various thread model by inserting this filter
    /// to a <see cref="IOFilterChain"/>.
    /// </summary>
    public class ExecutorFilter : IOFilterAdapter
    {
        private const IoEventType DefaultEventSet = IoEventType.ExceptionCaught |
            IoEventType.MessageReceived | IoEventType.MessageSent | IoEventType.SessionClosed |
            IoEventType.SessionIdle | IoEventType.SessionOpened;

        private readonly IoEventType _eventTypes;

        /// <summary>
        /// Creates an executor filter with default <see cref="IOEventExecutor"/> on default event types.
        /// </summary>
        public ExecutorFilter()
            : this(null, DefaultEventSet)
        { }

        /// <summary>
        /// Creates an executor filter with default <see cref="IOEventExecutor"/>.
        /// </summary>
        /// <param name="eventTypes">the event types interested</param>
        public ExecutorFilter(IoEventType eventTypes)
            : this(null, eventTypes)
        { }

        /// <summary>
        /// Creates an executor filter on default event types.
        /// </summary>
        /// <param name="executor">the <see cref="IOEventExecutor"/> to run events</param>
        public ExecutorFilter(IOEventExecutor executor)
            : this(executor, DefaultEventSet)
        { }

        /// <summary>
        /// Creates an executor filter.
        /// </summary>
        /// <param name="executor">the <see cref="IOEventExecutor"/> to run events</param>
        /// <param name="eventTypes">the event types interested</param>
        public ExecutorFilter(IOEventExecutor executor, IoEventType eventTypes)
        {
            _eventTypes = eventTypes;
            if (executor == null)
                Executor = new OrderedThreadPoolExecutor();
            else
                Executor = executor;
        }

        /// <summary>
        /// Gets the <see cref="IOEventExecutor"/> to run events.
        /// </summary>
        public IOEventExecutor Executor { get; }

        /// <inheritdoc/>
        public override void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once. Create another instance and add it.");
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IOSession session)
        {
            if ((_eventTypes & IoEventType.SessionOpened) == IoEventType.SessionOpened)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.SessionOpened, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.SessionOpened(nextFilter, session);
            }
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            if ((_eventTypes & IoEventType.SessionClosed) == IoEventType.SessionClosed)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.SessionClosed, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.SessionClosed(nextFilter, session);
            }
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            if ((_eventTypes & IoEventType.SessionIdle) == IoEventType.SessionIdle)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.SessionIdle, session, status);
                FireEvent(ioe);
            }
            else
            {
                base.SessionIdle(nextFilter, session, status);
            }
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
        {
            if ((_eventTypes & IoEventType.ExceptionCaught) == IoEventType.ExceptionCaught)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.ExceptionCaught, session, cause);
                FireEvent(ioe);
            }
            else
            {
                base.ExceptionCaught(nextFilter, session, cause);
            }
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            if ((_eventTypes & IoEventType.MessageReceived) == IoEventType.MessageReceived)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.MessageReceived, session, message);
                FireEvent(ioe);
            }
            else
            {
                base.MessageReceived(nextFilter, session, message);
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            if ((_eventTypes & IoEventType.MessageSent) == IoEventType.MessageSent)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.MessageSent, session, writeRequest);
                FireEvent(ioe);
            }
            else
            {
                base.MessageSent(nextFilter, session, writeRequest);
            }
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            if ((_eventTypes & IoEventType.Write) == IoEventType.Write)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.Write, session, writeRequest);
                FireEvent(ioe);
            }
            else
            {
                base.FilterWrite(nextFilter, session, writeRequest);
            }
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IOSession session)
        {
            if ((_eventTypes & IoEventType.Close) == IoEventType.Close)
            {
                var ioe = new IOFilterEvent(nextFilter, IoEventType.Close, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.FilterClose(nextFilter, session);
            }
        }

        /// <summary>
        /// Fires an event.
        /// </summary>
        /// <param name="ioe"></param>
        protected void FireEvent(IOFilterEvent ioe)
        {
            Executor.Execute(ioe);
        }
    }
}
