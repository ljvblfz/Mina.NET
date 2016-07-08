using System;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A container of <see cref="IOFilter"/>s that forwards <see cref="IOHandler"/> events
    /// to the consisting filters and terminal <see cref="IOHandler"/> sequentially.
    /// Every <see cref="IOSession"/> has its own <see cref="IOFilterChain"/> (1-to-1 relationship).
    /// </summary>
    public interface IOFilterChain : IChain<IOFilter, INextFilter>
    {
        /// <summary>
        /// Gets the parent <see cref="IOSession"/> of this chain.
        /// </summary>
        IOSession Session { get; }

        /// <summary>
        /// Fires a <see cref="IOHandler.SessionCreated(IOSession)"/> event.
        /// </summary>
        void FireSessionCreated();

        /// <summary>
        /// Fires a <see cref="IOHandler.SessionOpened(IOSession)"/> event.
        /// </summary>
        void FireSessionOpened();

        /// <summary>
        /// Fires a <see cref="IOHandler.SessionClosed(IOSession)"/> event.
        /// </summary>
        void FireSessionClosed();

        /// <summary>
        /// Fires a <see cref="IOHandler.SessionIdle(IOSession, IdleStatus)"/> event.
        /// </summary>
        void FireSessionIdle(IdleStatus status);

        /// <summary>
        /// Fires a <see cref="IOHandler.MessageReceived(IOSession, object)"/> event.
        /// </summary>
        void FireMessageReceived(object message);

        /// <summary>
        /// Fires a <see cref="IOHandler.MessageSent(IOSession, object)"/> event.
        /// </summary>
        void FireMessageSent(IWriteRequest request);

        /// <summary>
        /// Fires a <see cref="IOHandler.ExceptionCaught(IOSession, Exception)"/> event.
        /// </summary>
        void FireExceptionCaught(Exception ex);

        /// <summary>
        /// Fires a <see cref="IOHandler.InputClosed(IOSession)"/> event. Most users don't
        /// need to call this method at all. Please use this method only when you
        /// implement a new transport or fire a virtual event.
        /// </summary>
        void FireInputClosed();

        /// <summary>
        /// Fires a <see cref="IOSession.Write(object)"/> event.
        /// </summary>
        void FireFilterWrite(IWriteRequest writeRequest);

        /// <summary>
        /// Fires a <see cref="IOSession.Close(bool)"/> event.
        /// </summary>
        void FireFilterClose();
    }
}
