﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// Base class of socket acceptor.
    /// </summary>
    public abstract class AbstractSocketAcceptor : AbstractIOAcceptor, ISocketAcceptor
    {
        private readonly AsyncSocketProcessor _processor;
        private int _backlog;
        private int _maxConnections;
        private Semaphore _connectionPool;
#if NET20
        private readonly WaitCallback _startAccept;
#else
        private readonly Action<object> _startAccept;
#endif
        private bool _disposed;
        private readonly Dictionary<EndPoint, System.Net.Sockets.Socket> _listenSockets = new Dictionary<EndPoint, System.Net.Sockets.Socket>();

        /// <summary>
        /// Instantiates with default max connections of 1024.
        /// </summary>
        protected AbstractSocketAcceptor()
            : this(1024)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="maxConnections">the max connections allowed</param>
        protected AbstractSocketAcceptor(int maxConnections)
            : base(new DefaultSocketSessionConfig())
        {
            _maxConnections = maxConnections;
            _processor = new AsyncSocketProcessor(() => ManagedSessions.Values);
            SessionDestroyed += OnSessionDestroyed;
            _startAccept = StartAccept0;
            ReuseBuffer = true;
        }

        /// <inheritdoc/>
        public new ISocketSessionConfig SessionConfig => (ISocketSessionConfig)base.SessionConfig;

        /// <inheritdoc/>
        public new IPEndPoint LocalEndPoint => (IPEndPoint)base.LocalEndPoint;

        /// <inheritdoc/>
        public new IPEndPoint DefaultLocalEndPoint
        {
            get { return (IPEndPoint)base.DefaultLocalEndPoint; }
            set { base.DefaultLocalEndPoint = value; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => AsyncSocketSession.Metadata;

        /// <inheritdoc/>
        public bool ReuseAddress { get; set; }

        /// <inheritdoc/>
        public int Backlog
        {
            get { return _backlog; }
            set
            {
                lock (BindLock)
                {
                    if (Active)
                        throw new InvalidOperationException("Backlog can't be set while the acceptor is bound.");
                    _backlog = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of max connections.
        /// </summary>
        public int MaxConnections
        {
            get { return _maxConnections; }
            set
            {
                lock (BindLock)
                {
                    if (Active)
                        throw new InvalidOperationException("MaxConnections can't be set while the acceptor is bound.");
                    _maxConnections = value;
                }
            }
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

        /// <inheritdoc/>
        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            var newListeners = new Dictionary<EndPoint, System.Net.Sockets.Socket>();
            try
            {
                // Process all the addresses
                foreach (var localEp in localEndPoints)
                {
                    var ep = localEp;
                    if (ep == null)
                        ep = new IPEndPoint(IPAddress.Any, 0);
                    var listenSocket = new System.Net.Sockets.Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Bind(ep);
                    listenSocket.Listen(Backlog);
                    newListeners[listenSocket.LocalEndPoint] = listenSocket;
                }
            }
            catch (Exception)
            {
                // Roll back if failed to bind all addresses
                foreach (var listenSocket in newListeners.Values)
                {
                    try
                    {
                        listenSocket.Close();
                    }
                    catch (Exception ex)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(ex);
                    }
                }

                throw;
            }

            if (MaxConnections > 0)
                _connectionPool = new Semaphore(MaxConnections, MaxConnections);

            foreach (var pair in newListeners)
            {
                _listenSockets[pair.Key] = pair.Value;
                StartAccept(new ListenerContext(pair.Value));
            }

            _processor.IdleStatusChecker.Start();

            return newListeners.Keys;
        }

        /// <inheritdoc/>
        protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            foreach (var ep in localEndPoints)
            {
                System.Net.Sockets.Socket listenSocket;
                if (!_listenSockets.TryGetValue(ep, out listenSocket))
                    continue;
                listenSocket.Close();
                _listenSockets.Remove(ep);
            }

            if (_listenSockets.Count == 0)
            {
                _processor.IdleStatusChecker.Stop();

                if (_connectionPool != null)
                {
                    _connectionPool.Close();
                    _connectionPool = null;
                }
            }
        }

        private void StartAccept(ListenerContext listener)
        {
            if (_connectionPool == null)
            {
                BeginAccept(listener);
            }
            else
            {
#if NET20
                System.Threading.ThreadPool.QueueUserWorkItem(_startAccept, listener);
#else
                System.Threading.Tasks.Task.Factory.StartNew(_startAccept, listener);
#endif
            }
        }

        private void StartAccept0(object state)
        {
            var pool = _connectionPool;
            if (pool == null)
                // this might happen if has been unbound
                return;
            try
            {
                pool.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            BeginAccept((ListenerContext)state);
        }

        private void OnSessionDestroyed(object sender, IoSessionEventArgs e)
        {
            var pool = _connectionPool;
            if (pool != null)
                pool.Release();
        }

        /// <inheritdoc/>
        protected abstract IOSession NewSession(IIOProcessor<SocketSession> processor, System.Net.Sockets.Socket socket);

        /// <summary>
        /// Begins an accept operation.
        /// </summary>
        /// <param name="listener"></param>
        protected abstract void BeginAccept(ListenerContext listener);

        /// <summary>
        /// Ends an accept operation.
        /// </summary>
        /// <param name="socket">the accepted client socket</param>
        /// <param name="listener">the <see cref="ListenerContext"/></param>
        protected void EndAccept(System.Net.Sockets.Socket socket, ListenerContext listener)
        {
            if (socket != null)
            {
                var session = NewSession(_processor, socket);
                try
                {
                    InitSession<IOFuture>(session, null, null);
                    session.Processor.Add(session);
                }
                catch (Exception ex)
                {
                    ExceptionMonitor.Instance.ExceptionCaught(ex);
                }
            }

            // Accept the next connection request
            StartAccept(listener);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_listenSockets.Count > 0)
                    {
                        foreach (var listenSocket in _listenSockets.Values)
                        {
                            ((IDisposable)listenSocket).Dispose();
                        }
                    }
                    if (_connectionPool != null)
                    {
                        _connectionPool.Dispose();
                        _connectionPool = null;
                    }
                    _processor.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Provides context info for a socket acceptor.
        /// </summary>
        protected class ListenerContext
        {
            /// <summary>
            /// Instantiates.
            /// </summary>
            /// <param name="socket">the associated socket</param>
            public ListenerContext(System.Net.Sockets.Socket socket)
            {
                Socket = socket;
            }

            /// <summary>
            /// Gets the associated socket.
            /// </summary>
            public System.Net.Sockets.Socket Socket { get; }

            /// <summary>
            /// Gets or sets a tag.
            /// </summary>
            public object Tag { get; set; }
        }
    }
}
