using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An adapter class for <see cref="IOFilter"/>.  You can extend
    /// this class and selectively override required event filter methods only.  All
    /// methods forwards events to the next filter by default.
    /// </summary>
    public abstract class IOFilterAdapter : IOFilter
    {
        /// <inheritdoc/>
        public virtual void Init()
        { }

        /// <inheritdoc/>
        public virtual void Destroy()
        { }

        /// <inheritdoc/>
        public virtual void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void OnPostAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void OnPreRemove(IOFilterChain parent, string name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void OnPostRemove(IOFilterChain parent, string name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void SessionCreated(INextFilter nextFilter, IOSession session)
        {
            nextFilter.SessionCreated(session);
        }

        /// <inheritdoc/>
        public virtual void SessionOpened(INextFilter nextFilter, IOSession session)
        {
            nextFilter.SessionOpened(session);
        }

        /// <inheritdoc/>
        public virtual void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            nextFilter.SessionClosed(session);
        }

        /// <inheritdoc/>
        public virtual void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            nextFilter.SessionIdle(session, status);
        }

        /// <inheritdoc/>
        public virtual void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
        {
            nextFilter.ExceptionCaught(session, cause);
        }
        
        /// <inheritdoc/>
        public virtual void InputClosed(INextFilter nextFilter, IOSession session)
        {
            nextFilter.InputClosed(session);
        }

        /// <inheritdoc/>
        public virtual void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            nextFilter.MessageReceived(session, message);
        }

        /// <inheritdoc/>
        public virtual void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            nextFilter.MessageSent(session, writeRequest);
        }

        /// <inheritdoc/>
        public virtual void FilterClose(INextFilter nextFilter, IOSession session)
        {
            nextFilter.FilterClose(session);
        }

        /// <inheritdoc/>
        public virtual void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            nextFilter.FilterWrite(session, writeRequest);
        }
    }
}
