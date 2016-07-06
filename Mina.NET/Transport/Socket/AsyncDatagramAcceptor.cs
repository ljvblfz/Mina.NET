using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOAcceptor"/> for datagram transport (UDP/IP).
    /// </summary>
    public partial class AsyncDatagramAcceptor : AbstractIOAcceptor, IDatagramAcceptor, IIOProcessor<AsyncDatagramSession>
    {
        private static readonly IOSessionRecycler DefaultRecycler = new ExpiringSessionRecycler();

        private readonly IdleStatusChecker _idleStatusChecker;
        private bool _disposed;
        private IOSessionRecycler _sessionRecycler = DefaultRecycler;
        private readonly Dictionary<EndPoint, SocketContext> _listenSockets = new Dictionary<EndPoint, SocketContext>();

        /// <summary>
        /// Instantiates.
        /// </summary>
        public AsyncDatagramAcceptor()
            : base(new DefaultDatagramSessionConfig())
        {
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
            ReuseBuffer = true;
        }

        /// <inheritdoc/>
        public new IDatagramSessionConfig SessionConfig => (IDatagramSessionConfig)base.SessionConfig;

        /// <inheritdoc/>
        public new IPEndPoint LocalEndPoint => (IPEndPoint)base.LocalEndPoint;

        /// <inheritdoc/>
        public new IPEndPoint DefaultLocalEndPoint
        {
            get { return (IPEndPoint)base.DefaultLocalEndPoint; }
            set { base.DefaultLocalEndPoint = value; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => AsyncDatagramSession.Metadata;

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
        public IOSessionRecycler SessionRecycler
        {
            get { return _sessionRecycler; }
            set
            {
                lock (BindLock)
                {
                    if (Active)
                        throw new InvalidOperationException("SessionRecycler can't be set while the acceptor is bound.");

                    _sessionRecycler = value == null ? DefaultRecycler : value;
                }
            }
        }

        public IOSession NewSession(EndPoint remoteEp, EndPoint localEp)
        {
            if (Disposed)
                throw new ObjectDisposedException("AsyncDatagramAcceptor");
            if (remoteEp == null)
                throw new ArgumentNullException(nameof(remoteEp));

            SocketContext ctx;
            if (!_listenSockets.TryGetValue(localEp, out ctx))
                throw new ArgumentException("Unknown local endpoint: " + localEp, nameof(localEp));

            lock (BindLock)
            {
                if (!Active)
                    throw new InvalidOperationException("Can't create a session from a unbound service.");
                return NewSessionWithoutLock(remoteEp, ctx);
            }
        }

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
                    var listenSocket = new System.Net.Sockets.Socket(ep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                    new DatagramSessionConfigImpl(listenSocket).SetAll(SessionConfig);
                    listenSocket.Bind(ep);
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

            foreach (var pair in newListeners)
            {
                var ctx = new SocketContext(pair.Value, SessionConfig);
                _listenSockets[pair.Key] = ctx;
                BeginReceive(ctx);
            }

            _idleStatusChecker.Start();

            return newListeners.Keys;
        }

        /// <inheritdoc/>
        protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            foreach (var ep in localEndPoints)
            {
                SocketContext ctx;
                if (!_listenSockets.TryGetValue(ep, out ctx))
                    continue;
                _listenSockets.Remove(ep);
                ctx.Close();
            }

            if (_listenSockets.Count == 0)
            {
                _idleStatusChecker.Stop();
            }
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
                        foreach (var ctx in _listenSockets.Values)
                        {
                            ctx.Close();
                            ((IDisposable)ctx.Socket).Dispose();
                        }
                    }
                    _idleStatusChecker.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        private void EndReceive(SocketContext ctx, IOBuffer buf, EndPoint remoteEp)
        {
            var session = NewSessionWithoutLock(remoteEp, ctx);
            session.FilterChain.FireMessageReceived(buf);
            BeginReceive(ctx);
        }

        private IOSession NewSessionWithoutLock(EndPoint remoteEp, SocketContext ctx)
        {
            IOSession session;
            lock (_sessionRecycler)
            {
                session = _sessionRecycler.Recycle(remoteEp);

                if (session != null)
                    return session;

                // If a new session needs to be created.
                session = new AsyncDatagramSession(this, this, ctx, remoteEp, ReuseBuffer);
                _sessionRecycler.Put(session);
            }

            InitSession<IOFuture>(session, null, null);

            try
            {
                FilterChainBuilder.BuildFilterChain(session.FilterChain);

                var serviceSupport = session.Service as IOServiceSupport;
                if (serviceSupport != null)
                    serviceSupport.FireSessionCreated(session);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }

            return session;
        }

        internal partial class SocketContext
        {
            public readonly System.Net.Sockets.Socket _socket;
            private readonly ConcurrentQueue<AsyncDatagramSession> _flushingSessions = new ConcurrentQueue<AsyncDatagramSession>();
            private int _writing;

            public System.Net.Sockets.Socket Socket => _socket;

            public void Flush(AsyncDatagramSession session)
            {
                if (ScheduleFlush(session))
                    Flush();
            }

            private bool ScheduleFlush(AsyncDatagramSession session)
            {
                if (session.ScheduledForFlush())
                {
                    _flushingSessions.Enqueue(session);
                    return true;
                }
                return false;
            }

            private void Flush()
            {
                if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                    return;
                BeginSend(null);
            }

            private void BeginSend(AsyncDatagramSession session)
            {
                IWriteRequest req;

                while (true)
                {
                    if (session == null && !_flushingSessions.TryDequeue(out session))
                    {
                        Interlocked.Exchange(ref _writing, 0);
                        return;
                    }

                    req = session.CurrentWriteRequest;
                    if (req == null)
                    {
                        req = session.WriteRequestQueue.Poll(session);

                        if (req == null)
                        {
                            session.UnscheduledForFlush();
                            session = null;
                            continue;
                        }

                        session.CurrentWriteRequest = req;
                    }

                    break;
                }

                var buf = req.Message as IOBuffer;
                if (buf == null)
                    EndSend(session, new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?"));

                if (buf.HasRemaining)
                {
                    var destination = req.Destination;
                    if (destination == null)
                        destination = session.RemoteEndPoint;
                    BeginSend(session, buf, destination);
                }
                else
                {
                    EndSend(session, 0);
                }
            }

            private void EndSend(AsyncDatagramSession session, int bytesTransferred)
            {
                session.IncreaseWrittenBytes(bytesTransferred, DateTime.Now);

                var req = session.CurrentWriteRequest;
                if (req != null)
                {
                    var buf = req.Message as IOBuffer;
                    if (buf == null)
                    {
                        // we only send buffers and files so technically it shouldn't happen
                    }
                    else
                    {
                        // Buffer has been sent, clear the current request.
                        var pos = buf.Position;
                        buf.Reset();

                        session.CurrentWriteRequest = null;
                        try
                        {
                            session.FilterChain.FireMessageSent(req);
                        }
                        catch (Exception ex)
                        {
                            session.FilterChain.FireExceptionCaught(ex);
                        }

                        // And set it back to its position
                        buf.Position = pos;
                    }
                }

                BeginSend(session);
            }

            private void EndSend(AsyncDatagramSession session, Exception ex)
            {
                var req = session.CurrentWriteRequest;
                if (req != null)
                    req.Future.Exception = ex;
                session.FilterChain.FireExceptionCaught(ex);
                BeginSend(session);
            }
        }

        #region IoProcessor

        /// <inheritdoc/>
        public void Add(AsyncDatagramSession session)
        {
            // do nothing for UDP
        }

        /// <inheritdoc/>
        public void Write(AsyncDatagramSession session, IWriteRequest writeRequest)
        {
            session.WriteRequestQueue.Offer(session, writeRequest);
            Flush(session);
        }

        /// <inheritdoc/>
        public void Flush(AsyncDatagramSession session)
        {
            session.Context.Flush(session);
        }

        /// <inheritdoc/>
        public void Remove(AsyncDatagramSession session)
        {
            SessionRecycler.Remove(session);
            var support = session.Service as IOServiceSupport;
            if (support != null)
                support.FireSessionDestroyed(session);
        }

        /// <inheritdoc/>
        public void UpdateTrafficControl(AsyncDatagramSession session)
        {
            throw new NotSupportedException();
        }

        void IOProcessor.Write(IOSession session, IWriteRequest writeRequest)
        {
            Write((AsyncDatagramSession)session, writeRequest);
        }

        void IOProcessor.Flush(IOSession session)
        {
            Flush((AsyncDatagramSession)session);
        }

        void IOProcessor.Add(IOSession session)
        {
            Add((AsyncDatagramSession)session);
        }

        void IOProcessor.Remove(IOSession session)
        {
            Remove((AsyncDatagramSession)session);
        }

        void IOProcessor.UpdateTrafficControl(IOSession session)
        {
            UpdateTrafficControl((AsyncDatagramSession)session);
        }

        #endregion
    }
}
