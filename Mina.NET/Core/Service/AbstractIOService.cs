using System;
using System.Threading;
using Mina.Core.Session;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Buffer;
using Mina.Util;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base implementation of <see cref="IOService"/>s.
    /// </summary>
    public abstract class AbstractIOService : IOService, IOServiceSupport, IDisposable
    {
        private int _active;
        private IOHandler _handler;
        private bool _hasHandler;
        private IOSessionDataStructureFactory _sessionDataStructureFactory = new DefaultIOSessionDataStructureFactory();

        private ConcurrentDictionary<long, IOSession> _managedSessions = new ConcurrentDictionary<long, IOSession>();

        /// <inheritdoc/>
        public event EventHandler Activated;

        /// <inheritdoc/>
        public event EventHandler<IdleEventArgs> Idle;

        /// <inheritdoc/>
        public event EventHandler Deactivated;

        /// <inheritdoc/>
        public event EventHandler<IOSessionEventArgs> SessionCreated;

        /// <inheritdoc/>
        public event EventHandler<IOSessionEventArgs> SessionOpened;

        /// <inheritdoc/>
        public event EventHandler<IOSessionEventArgs> SessionClosed;

        /// <inheritdoc/>
        public event EventHandler<IOSessionEventArgs> SessionDestroyed;

        /// <inheritdoc/>
        public event EventHandler<IOSessionIdleEventArgs> SessionIdle;

        /// <inheritdoc/>
        public event EventHandler<IOSessionExceptionEventArgs> ExceptionCaught;

        /// <inheritdoc/>
        public event EventHandler<IOSessionEventArgs> InputClosed;

        /// <inheritdoc/>
        public event EventHandler<IOSessionMessageEventArgs> MessageReceived;

        /// <inheritdoc/>
        public event EventHandler<IOSessionMessageEventArgs> MessageSent;

        /// <summary>
        /// </summary>
        protected AbstractIOService(IOSessionConfig sessionConfig)
        {
            SessionConfig = sessionConfig;
            _handler = new InnerHandler(this);
            Statistics = new IOServiceStatistics(this);
        }

        /// <inheritdoc/>
        public abstract ITransportMetadata TransportMetadata { get; }

        /// <inheritdoc/>
        public bool Disposed { get; private set; }

        /// <inheritdoc/>
        public IOHandler Handler
        {
            get { return _handler; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _handler = value;
                _hasHandler = true;
            }
        }

        /// <inheritdoc/>
        public IDictionary<long, IOSession> ManagedSessions => _managedSessions;

        /// <inheritdoc/>
        public IOSessionConfig SessionConfig { get; }

        /// <inheritdoc/>
        public IOFilterChainBuilder FilterChainBuilder { get; set; } = new DefaultIOFilterChainBuilder();

        /// <inheritdoc/>
        public DefaultIOFilterChainBuilder FilterChain => FilterChainBuilder as DefaultIOFilterChainBuilder;

        /// <inheritdoc/>
        public IOSessionDataStructureFactory SessionDataStructureFactory
        {
            get { return _sessionDataStructureFactory; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (Active)
                {
                    throw new InvalidOperationException();
                }
                _sessionDataStructureFactory = value;
            }
        }

        /// <inheritdoc/>
        public bool Active => _active > 0;

        /// <inheritdoc/>
        public DateTime ActivationTime { get; private set; }

        /// <inheritdoc/>
        public IOServiceStatistics Statistics { get; }

        /// <inheritdoc/>
        public IEnumerable<IWriteFuture> Broadcast(object message)
        {
            var answer = new List<IWriteFuture>(_managedSessions.Count);
            var buf = message as IOBuffer;
            if (buf == null)
            {
                foreach (var session in _managedSessions.Values)
                {
                    answer.Add(session.Write(message));
                }
            }
            else
            {
                foreach (var session in _managedSessions.Values)
                {
                    answer.Add(session.Write(buf.Duplicate()));
                }
            }
            return answer;
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initializes sessions.
        /// </summary>
        protected void InitSession<TFuture>(IOSession session, TFuture future,
            Action<IOSession, TFuture> initializeSession)
            where TFuture : IOFuture
        {
            var s = session as AbstractIOSession;
            if (s != null)
            {
                s.AttributeMap = s.Service.SessionDataStructureFactory.GetAttributeMap(session);
                s.SetWriteRequestQueue(s.Service.SessionDataStructureFactory.GetWriteRequestQueue(session));
            }

            if (future != null && future is IConnectFuture)
            {
                session.SetAttribute(DefaultIOFilterChain.SessionCreatedFuture, future);
            }

            if (initializeSession != null)
            {
                initializeSession(session, future);
            }

            FinishSessionInitialization0(session, future);
        }

        /// <summary>
        /// Implement this method to perform additional tasks required for session
        /// initialization. Do not call this method directly.
        /// </summary>
        protected virtual void FinishSessionInitialization0(IOSession session, IOFuture future)
        {
            // Do nothing. Extended class might add some specific code 
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            Disposed = true;
        }

        private void DisconnectSessions()
        {
            var acceptor = this as IOAcceptor;
            if (acceptor == null)
            {
                // We don't disconnect sessions for anything but an IoAcceptor
                return;
            }

            if (!acceptor.CloseOnDeactivation)
            {
                return;
            }

            var closeFutures = new List<ICloseFuture>(_managedSessions.Count);
            foreach (var s in _managedSessions.Values)
            {
                closeFutures.Add(s.Close(true));
            }

            new CompositeIOFuture<ICloseFuture>(closeFutures).Await();
        }

        #region IoServiceSupport

        void IOServiceSupport.FireServiceActivated()
        {
            if (Interlocked.CompareExchange(ref _active, 1, 0) > 0)
            {
                // The instance is already active
                return;
            }
            ActivationTime = DateTime.Now;
            Statistics.LastReadTime = ActivationTime;
            Statistics.LastWriteTime = ActivationTime;
            Statistics.LastThroughputCalculationTime = ActivationTime;
            DelegateUtils.SafeInvoke(Activated, this);
        }

        void IOServiceSupport.FireServiceIdle(IdleStatus idleStatus)
        {
            DelegateUtils.SafeInvoke(Idle, this, new IdleEventArgs(idleStatus));
        }

        void IOServiceSupport.FireSessionCreated(IOSession session)
        {
            // If already registered, ignore.
            if (!_managedSessions.TryAdd(session.Id, session))
            {
                return;
            }

            // Fire session events.
            var filterChain = session.FilterChain;
            filterChain.FireSessionCreated();
            filterChain.FireSessionOpened();

            if (_hasHandler)
            {
                DelegateUtils.SafeInvoke(SessionCreated, this, new IOSessionEventArgs(session));
            }
        }

        void IOServiceSupport.FireSessionDestroyed(IOSession session)
        {
            IOSession s;
            if (!_managedSessions.TryRemove(session.Id, out s))
            {
                return;
            }

            // Fire session events.
            session.FilterChain.FireSessionClosed();

            DelegateUtils.SafeInvoke(SessionDestroyed, this, new IOSessionEventArgs(session));

            // Fire a virtual service deactivation event for the last session of the connector.
            if (session.Service is IOConnector)
            {
                var lastSession = _managedSessions.IsEmpty;
                if (lastSession)
                {
                    ((IOServiceSupport) this).FireServiceDeactivated();
                }
            }
        }

        void IOServiceSupport.FireServiceDeactivated()
        {
            if (Interlocked.CompareExchange(ref _active, 0, 1) == 0)
            {
                // The instance is already desactivated
                return;
            }
            DelegateUtils.SafeInvoke(Deactivated, this);
            DisconnectSessions();
        }

        #endregion

        class InnerHandler : IOHandler
        {
            private readonly AbstractIOService _service;

            public InnerHandler(AbstractIOService service)
            {
                _service = service;
            }

            public void SessionCreated(IOSession session)
            {
                var act = _service.SessionCreated;
                if (act != null)
                {
                    act(_service, new IOSessionEventArgs(session));
                }
            }

            void IOHandler.SessionOpened(IOSession session)
            {
                var act = _service.SessionOpened;
                if (act != null)
                {
                    act(_service, new IOSessionEventArgs(session));
                }
            }

            void IOHandler.SessionClosed(IOSession session)
            {
                var act = _service.SessionClosed;
                if (act != null)
                {
                    act(_service, new IOSessionEventArgs(session));
                }
            }

            void IOHandler.SessionIdle(IOSession session, IdleStatus status)
            {
                var act = _service.SessionIdle;
                if (act != null)
                {
                    act(_service, new IOSessionIdleEventArgs(session, status));
                }
            }

            void IOHandler.ExceptionCaught(IOSession session, Exception cause)
            {
                var act = _service.ExceptionCaught;
                if (act != null)
                {
                    act(_service, new IOSessionExceptionEventArgs(session, cause));
                }
            }

            void IOHandler.MessageReceived(IOSession session, object message)
            {
                var act = _service.MessageReceived;
                if (act != null)
                {
                    act(_service, new IOSessionMessageEventArgs(session, message));
                }
            }

            void IOHandler.MessageSent(IOSession session, object message)
            {
                var act = _service.MessageSent;
                if (act != null)
                {
                    act(_service, new IOSessionMessageEventArgs(session, message));
                }
            }

            void IOHandler.InputClosed(IOSession session)
            {
                var act = _service.InputClosed;
                if (act != null)
                {
                    act(_service, new IOSessionEventArgs(session));
                }
                else
                {
                    session.Close(true);
                }
            }
        }
    }
}
