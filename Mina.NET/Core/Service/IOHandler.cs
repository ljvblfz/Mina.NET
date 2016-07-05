using System;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Handles all I/O events fired by MINA.
    /// </summary>
    public interface IOHandler
    {
        /// <summary>
        /// Invoked from an I/O processor thread when a new connection has been created.
        /// </summary>
        void SessionCreated(IOSession session);
        /// <summary>
        /// Invoked when a connection has been opened.
        /// This method is invoked after <see cref="SessionCreated(IOSession)"/>.
        /// </summary>
        /// <remarks>
        /// The biggest difference from <see cref="SessionCreated(IOSession)"/> is that
        /// it's invoked from other thread than an I/O processor thread once
        /// thread model is configured properly.
        /// </remarks>
        void SessionOpened(IOSession session);
        /// <summary>
        /// Invoked when a connection is closed.
        /// </summary>
        void SessionClosed(IOSession session);
        /// <summary>
        /// Invoked with the related <see cref="IdleStatus"/> when a connection becomes idle.
        /// </summary>
        void SessionIdle(IOSession session, IdleStatus status);
        /// <summary>
        /// Invoked when any exception is thrown by user <see cref="IOHandler"/>
        /// implementation or by Mina.
        /// </summary>
        void ExceptionCaught(IOSession session, Exception cause);
        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        void MessageReceived(IOSession session, object message);
        /// <summary>
        /// Invoked when a message written by <see cref="IOSession.Write(object)"/>
        /// is sent out.
        /// </summary>
        void MessageSent(IOSession session, object message);
        ///
        /// Handle the closure of an half-duplex channel.
        ///
        void InputClosed(IOSession session);
    }
}
