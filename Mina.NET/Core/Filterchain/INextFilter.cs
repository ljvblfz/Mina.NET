using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// Represents the next <see cref="IOFilter"/> in <see cref="IOFilterChain"/>.
    /// </summary>
    public interface INextFilter
    {
        /// <summary>
        /// Forwards <code>SessionCreated</code> event to next filter.
        /// </summary>
        void SessionCreated(IOSession session);
        /// <summary>
        /// Forwards <code>SessionOpened</code> event to next filter.
        /// </summary>
        void SessionOpened(IOSession session);
        /// <summary>
        /// Forwards <code>SessionClosed</code> event to next filter.
        /// </summary>
        void SessionClosed(IOSession session);
        /// <summary>
        /// Forwards <code>SessionIdle</code> event to next filter.
        /// </summary>
        void SessionIdle(IOSession session, IdleStatus status);
        /// <summary>
        /// Forwards <code>ExceptionCaught</code> event to next filter.
        /// </summary>
        void ExceptionCaught(IOSession session, Exception cause);
        /// <summary>
        /// Forwards <code>InputClosed</code> event to next filter.
        /// </summary>
        void InputClosed(IOSession session);
        /// <summary>
        /// Forwards <code>MessageReceived</code> event to next filter.
        /// </summary>
        void MessageReceived(IOSession session, object message);
        /// <summary>
        /// Forwards <code>MessageSent</code> event to next filter.
        /// </summary>
        void MessageSent(IOSession session, IWriteRequest writeRequest);
        /// <summary>
        /// Forwards <code>FilterClose</code> event to next filter.
        /// </summary>
        void FilterClose(IOSession session);
        /// <summary>
        /// Forwards <code>FilterWrite</code> event to next filter.
        /// </summary>
        void FilterWrite(IOSession session, IWriteRequest writeRequest);
    }
}
