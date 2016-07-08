using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// Base class of socket connector.
    /// </summary>
    public abstract class AbstractSocketConnector : AbstractIOConnector
    {
        private readonly AsyncSocketProcessor _processor;

        /// <summary>
        /// Instantiates.
        /// </summary>
        protected AbstractSocketConnector(IOSessionConfig sessionConfig)
            : base(sessionConfig)
        {
            _processor = new AsyncSocketProcessor(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public new IPEndPoint DefaultRemoteEndPoint
        {
            get { return (IPEndPoint) base.DefaultRemoteEndPoint; }
            set { base.DefaultRemoteEndPoint = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the read buffer
        /// sent to <see cref="SocketSession.FilterChain"/> by
        /// <see cref="IOFilterChain.FireMessageReceived(object)"/>.
        /// </summary>
        /// <remarks>
        /// If any thread model, i.e. an <see cref="Filter.Executor.ExecutorFilter"/>,
        /// is added before filters that process the incoming <see cref="Core.Buffer.IOBuffer"/>
        /// in <see cref="IOFilter.MessageReceived(Core.FilterchIOSessionFilter, IoSession, object)"/>,
        /// this must be set to <code>false</code> to avoid undetermined state
        /// of the read buffer. The default value is <code>true</code>.
        /// </remarks>
        public bool ReuseBuffer { get; set; }

        /// <summary>
        /// Gets the <see cref="IOProcessor"/>.
        /// </summary>
        protected IIOProcessor<SocketSession> Processor => _processor;

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEp, EndPoint localEp,
            Action<IOSession, IConnectFuture> sessionInitializer)
        {
            var socket = NewSocket(remoteEp.AddressFamily);
            if (localEp != null)
            {
                socket.Bind(localEp);
            }
            var ctx = new ConnectorContext(socket, remoteEp, sessionInitializer);
            BeginConnect(ctx);
            return ctx;
        }

        /// <summary>
        /// Creates a socket according to the address family.
        /// </summary>
        /// <param name="addressFamily">the <see cref="AddressFamily"/></param>
        /// <returns>the socket created</returns>
        protected abstract System.Net.Sockets.Socket NewSocket(AddressFamily addressFamily);

        /// <summary>
        /// Begins connecting.
        /// </summary>
        /// <param name="connector">the context of current connector</param>
        protected abstract void BeginConnect(ConnectorContext connector);

        /// <summary>
        /// Ends connecting.
        /// </summary>
        /// <param name="session">the connected session</param>
        /// <param name="connector">the context of current connector</param>
        protected void EndConnect(IOSession session, ConnectorContext connector)
        {
            try
            {
                InitSession(session, connector, connector.SessionInitializer);
                session.Processor.Add(session);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }

            _processor.IdleStatusChecker.Start();
        }

        /// <summary>
        /// Ends connecting.
        /// </summary>
        /// <param name="cause">the exception occurred</param>
        /// <param name="connector">the context of current connector</param>
        protected void EndConnect(Exception cause, ConnectorContext connector)
        {
            connector.Exception = cause;
            connector.Socket.Close();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _processor.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Provides context info for a socket connector.
        /// </summary>
        protected class ConnectorContext : DefaultConnectFuture
        {
            /// <summary>
            /// Instantiates.
            /// </summary>
            /// <param name="socket">the associated socket</param>
            /// <param name="remoteEp">the remote endpoint</param>
            /// <param name="sessionInitializer">the funciton to initialize session</param>
            public ConnectorContext(System.Net.Sockets.Socket socket, EndPoint remoteEp,
                Action<IOSession, IConnectFuture> sessionInitializer)
            {
                Socket = socket;
                RemoteEp = remoteEp;
                SessionInitializer = sessionInitializer;
            }

            /// <summary>
            /// Gets the associated socket.
            /// </summary>
            public System.Net.Sockets.Socket Socket { get; }

            /// <summary>
            /// Gets the remote endpoint.
            /// </summary>
            public EndPoint RemoteEp { get; }

            /// <summary>
            /// Gets the function to initialize session.
            /// </summary>
            public Action<IOSession, IConnectFuture> SessionInitializer { get; }

            /// <inheritdoc/>
            public override bool Cancel()
            {
                var justCancelled = base.Cancel();
                if (justCancelled)
                {
                    Socket.Close();
                }
                return justCancelled;
            }

            /// <inheritdoc/>
            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Socket.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
