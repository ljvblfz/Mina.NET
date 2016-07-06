using System;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// A base implementation of <see cref="IOConnector"/>.
    /// </summary>
    public abstract class AbstractIOConnector : AbstractIOService, IOConnector
    {
        private EndPoint _defaultRemoteEp;

        /// <summary>
        /// </summary>
        protected AbstractIOConnector(IOSessionConfig sessionConfig)
            : base(sessionConfig)
        {
        }

        /// <inheritdoc/>
        public EndPoint DefaultRemoteEndPoint
        {
            get { return _defaultRemoteEp; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (!TransportMetadata.EndPointType.IsAssignableFrom(value.GetType()))
                {
                    throw new ArgumentException("defaultRemoteAddress type: " + value.GetType()
                                                + " (expected: " + TransportMetadata.EndPointType + ")");
                }
                _defaultRemoteEp = value;
            }
        }

        /// <inheritdoc/>
        public EndPoint DefaultLocalEndPoint { get; set; }

        /// <inheritdoc/>
        public int ConnectTimeout
        {
            get { return (int) (ConnectTimeoutInMillis / 1000L); }
            set { ConnectTimeoutInMillis = value * 1000L; }
        }

        /// <inheritdoc/>
        public long ConnectTimeoutInMillis { get; set; } = 60000L;

        /// <inheritdoc/>
        public IConnectFuture Connect()
        {
            if (_defaultRemoteEp == null)
            {
                throw new InvalidOperationException("DefaultRemoteEndPoint is not set.");
            }
            return Connect(_defaultRemoteEp, DefaultLocalEndPoint, null);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(Action<IOSession, IConnectFuture> sessionInitializer)
        {
            if (_defaultRemoteEp == null)
            {
                throw new InvalidOperationException("DefaultRemoteEndPoint is not set.");
            }
            return Connect(_defaultRemoteEp, DefaultLocalEndPoint, sessionInitializer);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEp)
        {
            return Connect(remoteEp, DefaultLocalEndPoint, null);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEp, Action<IOSession, IConnectFuture> sessionInitializer)
        {
            return Connect(remoteEp, DefaultLocalEndPoint, sessionInitializer);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEp, EndPoint localEp)
        {
            return Connect(remoteEp, localEp, null);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEp, EndPoint localEp,
            Action<IOSession, IConnectFuture> sessionInitializer)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (remoteEp == null)
            {
                throw new ArgumentNullException(nameof(remoteEp));
            }

            if (!TransportMetadata.EndPointType.IsAssignableFrom(remoteEp.GetType()))
            {
                throw new ArgumentException("remoteAddress type: " + remoteEp.GetType() + " (expected: "
                                            + TransportMetadata.EndPointType + ")");
            }

            return Connect0(remoteEp, localEp, sessionInitializer);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var m = TransportMetadata;
            return '(' + m.ProviderName + ' ' + m.Name + " connector: " + "managedSessionCount: "
                   + ManagedSessions.Count + ')';
        }

        /// <summary>
        /// Implement this method to perform the actual connect operation.
        /// </summary>
        protected abstract IConnectFuture Connect0(EndPoint remoteEp, EndPoint localEp,
            Action<IOSession, IConnectFuture> sessionInitializer);

        /// <inheritdoc/>
        protected override void FinishSessionInitialization0(IOSession session, IOFuture future)
        {
            // In case that IConnectFuture.Cancel() is invoked before
            // SetSession() is invoked, add a listener that closes the
            // connection immediately on cancellation.
            future.Complete += (s, e) =>
            {
                if (((IConnectFuture) e.Future).Canceled)
                {
                    session.Close(true);
                }
            };
        }
    }
}
