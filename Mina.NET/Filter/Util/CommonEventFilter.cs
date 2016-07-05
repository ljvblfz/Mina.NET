﻿using System;
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
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionCreated, session, null));
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IOSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionOpened, session, null));
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionIdle, session, status));
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionClosed, session, null));
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.ExceptionCaught, session, cause));
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.MessageReceived, session, message));
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.MessageSent, session, writeRequest));
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.Write, session, writeRequest));
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IOSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.Close, session, null));
        }

        /// <summary>
        /// Filters an <see cref="IoFilterEvent"/>.
        /// </summary>
        /// <param name="ioe">the event</param>
        protected abstract void Filter(IoFilterEvent ioe);
    }
}
