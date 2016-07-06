using System;
using System.Collections.Generic;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base interface for all <see cref="IOAcceptor"/>s and <see cref="IOConnector"/>s
    /// that provide I/O service and manage <see cref="IOSession"/>s.
    /// </summary>
    public interface IOService : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ITransportMetadata"/> that this service runs on.
        /// </summary>
        ITransportMetadata TransportMetadata { get; }

        /// <summary>
        /// Returns <code>true</code> if and if only all resources of this service
        /// have been disposed.
        /// </summary>
        bool Disposed { get; }

        /// <summary>
        /// Gets or sets the handler which will handle all connections managed by this service.
        /// </summary>
        IOHandler Handler { get; set; }

        /// <summary>
        /// Gets the map of all sessions which are currently managed by this service.
        /// </summary>
        IDictionary<long, IOSession> ManagedSessions { get; }

        /// <summary>
        /// Returns a value of whether or not this service is active.
        /// </summary>
        bool Active { get; }

        /// <summary>
        /// Returns the time when this service was activated.
        /// </summary>
        DateTime ActivationTime { get; }

        /// <summary>
        /// Returns the default configuration of the new <see cref="IOSession"/>s created by this service.
        /// </summary>
        IOSessionConfig SessionConfig { get; }

        /// <summary>
        /// Gets or sets the <see cref="IOFilterChainBuilder"/> which will build the
        /// <see cref="IOFilterChain"/> of all <see cref="IOSession"/>s which is created by this service.
        /// </summary>
        IOFilterChainBuilder FilterChainBuilder { get; set; }

        /// <summary>
        /// A shortcut for <tt>( ( DefaultIoFilterChainBuilder ) </tt><see cref="FilterChainBuilder"/><tt> )</tt>.
        /// </summary>
        DefaultIOFilterChainBuilder FilterChain { get; }

        /// <summary>
        /// Gets or sets the <see cref="IOSessionDataStructureFactory"/> that provides
        /// related data structures for a new session created by this service.
        /// </summary>
        IOSessionDataStructureFactory SessionDataStructureFactory { get; set; }

        /// <summary>
        /// Writes the specified message to all the <see cref="IOSession"/>s
        /// managed by this service.
        /// </summary>
        IEnumerable<IWriteFuture> Broadcast(object message);

        /// <summary>
        /// Fires when this service is activated.
        /// </summary>
        event EventHandler Activated;

        /// <summary>
        /// Fires when this service is idle.
        /// </summary>
        event EventHandler<IdleEventArgs> Idle;

        /// <summary>
        /// Fires when this service is deactivated.
        /// </summary>
        event EventHandler Deactivated;

        /// <summary>
        /// Fires when a new session is created.
        /// </summary>
        event EventHandler<IOSessionEventArgs> SessionCreated;

        /// <summary>
        /// Fires when a new session is being destroyed.
        /// </summary>
        event EventHandler<IOSessionEventArgs> SessionDestroyed;

        /// <summary>
        /// Fires when a session is opened. Only available when
        /// no <see cref="IOHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IOHandler.SessionOpened(IOSession)"/> instead.
        /// </remarks>
        event EventHandler<IOSessionEventArgs> SessionOpened;

        /// <summary>
        /// Fires when a session is closed. Only available when
        /// no <see cref="IOHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IOHandler.SessionClosed(IOSession)"/> instead.
        /// </remarks>
        event EventHandler<IOSessionEventArgs> SessionClosed;

        /// <summary>
        /// Fires when a session is idle. Only available when
        /// no <see cref="IOHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IOHandler.SessionIdle(IOSession, IdleStatus)"/> instead.
        /// </remarks>
        event EventHandler<IOSessionIdleEventArgs> SessionIdle;

        /// <summary>
        /// Fires when any exception is thrown. Only available when
        /// no <see cref="IOHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IOHandler.ExceptionCaught(IOSession, Exception)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionExceptionEventArgs> ExceptionCaught;

        /// <summary>
        /// Occurs when the closure of an half-duplex channel.
        /// </summary>
        event EventHandler<IOSessionEventArgs> InputClosed;

        /// <summary>
        /// Fires when a message is received. Only available when
        /// no <see cref="IOHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IOHandler.MessageReceived(IOSession, object)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionMessageEventArgs> MessageReceived;

        /// <summary>
        /// Fires when a message is sent. Only available when
        /// no <see cref="IOHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IOHandler.MessageSent(IOSession, object)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionMessageEventArgs> MessageSent;

        /// <summary>
        /// Gets the IoServiceStatistics object for this service.
        /// </summary>
        IOServiceStatistics Statistics { get; }
    }
}
