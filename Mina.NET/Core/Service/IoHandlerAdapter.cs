using System;
using Common.Logging;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// An adapter class for <see cref="IOHandler"/>.  You can extend this
    /// class and selectively override required event handler methods only.  All
    /// methods do nothing by default.
    /// </summary>
    public class IOHandlerAdapter : IOHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IOHandlerAdapter));

        /// <inheritdoc/>
        public virtual void SessionCreated(IOSession session)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void SessionOpened(IOSession session)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void SessionClosed(IOSession session)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void SessionIdle(IOSession session, IdleStatus status)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void ExceptionCaught(IOSession session, Exception cause)
        {
            if (Log.IsWarnEnabled)
            {
                Log.WarnFormat("EXCEPTION, please implement {0}.ExceptionCaught() for proper handling: {1}", GetType().Name, cause);
            }
        }

        /// <inheritdoc/>
        public virtual void MessageReceived(IOSession session, object message)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void MessageSent(IOSession session, object message)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public void InputClosed(IOSession session)
        {
            session.Close(true);
        }
    }
}
