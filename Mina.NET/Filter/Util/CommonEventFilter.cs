using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Util
{
    /// <summary>
    /// Extend this class when you want to create a filter that
    /// wraps the same logic around all 9 IoEvents
    /// </summary>
    public abstract class CommonEventFilter : IOFilterAdapter
    {
        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IOSession session)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.SessionCreated, session, null));
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IOSession session)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.SessionOpened, session, null));
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.SessionIdle, session, status));
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.SessionClosed, session, null));
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.ExceptionCaught, session, cause));
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.MessageReceived, session, message));
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.MessageSent, session, writeRequest));
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.Write, session, writeRequest));
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IOSession session)
        {
            Filter(new IOFilterEvent(nextFilter, IOEventType.Close, session, null));
        }

        /// <summary>
        /// Filters an <see cref="IOFilterEvent"/>.
        /// </summary>
        /// <param name="ioe">the event</param>
        protected abstract void Filter(IOFilterEvent ioe);
    }
}
