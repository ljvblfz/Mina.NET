using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// Connects to <see cref="IOHandler"/>s which is bound on the specified
    /// <see cref="LoopbackEndPoint"/>.
    /// </summary>
    public class LoopbackConnector : AbstractIOConnector
    {
        static readonly HashSet<LoopbackEndPoint> TakenLocalEPs = new HashSet<LoopbackEndPoint>();
        static int _nextLocalPort = -1;
        private IdleStatusChecker _idleStatusChecker;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public LoopbackConnector()
            : base(new DefaultLoopbackSessionConfig())
        {
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => LoopbackSession.Metadata;

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEp, EndPoint localEp, Action<IOSession, IConnectFuture> sessionInitializer)
        {
            LoopbackPipe entry;
            if (!LoopbackAcceptor.BoundHandlers.TryGetValue(remoteEp, out entry))
                return DefaultConnectFuture.NewFailedFuture(new IOException("Endpoint unavailable: " + remoteEp));

            var future = new DefaultConnectFuture();

            // Assign the local end point dynamically,
            LoopbackEndPoint actualLocalEp;
            try
            {
                actualLocalEp = NextLocalEp();
            }
            catch (IOException e)
            {
                return DefaultConnectFuture.NewFailedFuture(e);
            }

            var localSession = new LoopbackSession(this, actualLocalEp, Handler, entry);

            InitSession(localSession, future, sessionInitializer);

            // and reclaim the local end point when the connection is closed.
            localSession.CloseFuture.Complete += ReclaimLocalEp;

            // initialize connector session
            try
            {
                var filterChain = localSession.FilterChain;
                FilterChainBuilder.BuildFilterChain(filterChain);

                // The following sentences don't throw any exceptions.
                var serviceSupport = this as IOServiceSupport;
                if (serviceSupport != null)
                    serviceSupport.FireSessionCreated(localSession);
            }
            catch (Exception ex)
            {
                future.Exception = ex;
                return future;
            }

            // initialize acceptor session
            var remoteSession = localSession.RemoteSession;
            ((LoopbackAcceptor)remoteSession.Service).DoFinishSessionInitialization(remoteSession, null);
            try
            {
                var filterChain = remoteSession.FilterChain;
                entry.Acceptor.FilterChainBuilder.BuildFilterChain(filterChain);

                // The following sentences don't throw any exceptions.
                var serviceSupport = entry.Acceptor as IOServiceSupport;
                if (serviceSupport != null)
                    serviceSupport.FireSessionCreated(remoteSession);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
                remoteSession.Close(true);
            }

            // Start chains, and then allow and messages read/written to be processed. This is to ensure that
            // sessionOpened gets received before a messageReceived
            ((LoopbackFilterChain)localSession.FilterChain).Start();
            ((LoopbackFilterChain)remoteSession.FilterChain).Start();

            return future;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _idleStatusChecker.Dispose();
            }
            base.Dispose(disposing);
        }

        private static LoopbackEndPoint NextLocalEp()
        {
            lock (TakenLocalEPs)
            {
                if (_nextLocalPort >= 0)
                    _nextLocalPort = -1;
                for (var i = 0; i < int.MaxValue; i++)
                {
                    var answer = new LoopbackEndPoint(_nextLocalPort--);
                    if (!TakenLocalEPs.Contains(answer))
                    {
                        TakenLocalEPs.Add(answer);
                        return answer;
                    }
                }
            }

            throw new IOException("Can't assign a Loopback port.");
        }

        private static void ReclaimLocalEp(object sender, IoFutureEventArgs e)
        {
            lock (TakenLocalEPs)
            {
                TakenLocalEPs.Remove((LoopbackEndPoint)e.Future.Session.LocalEndPoint);
            }
        }
    }
}
