using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base implementation of <see cref="IOAcceptor"/>.
    /// </summary>
    public abstract class AbstractIOAcceptor : AbstractIOService, IOAcceptor
    {
        private readonly List<EndPoint> _defaultLocalEndPoints = new List<EndPoint>();
        private readonly List<EndPoint> _boundEndPoints = new List<EndPoint>();

        /// <summary>
        /// The lock for binding.
        /// </summary>
        [CLSCompliant(false)]
        protected object BindLock;

        /// <summary>
        /// </summary>
        protected AbstractIOAcceptor(IOSessionConfig sessionConfig)
            : base(sessionConfig)
        {
            BindLock = ((ICollection) _boundEndPoints).SyncRoot;
        }

        /// <inheritdoc/>
        public bool CloseOnDeactivation { get; set; } = true;

        /// <inheritdoc/>
        public IEnumerable<EndPoint> DefaultLocalEndPoints
        {
            get { return _defaultLocalEndPoints; }
            set
            {
                lock (BindLock)
                {
                    if (_boundEndPoints.Count > 0)
                    {
                        throw new InvalidOperationException("LocalEndPoints can't be set while the acceptor is bound.");
                    }

                    _defaultLocalEndPoints.Clear();
                    _defaultLocalEndPoints.AddRange(value);
                }
            }
        }

        /// <inheritdoc/>
        public EndPoint DefaultLocalEndPoint
        {
            get { return _defaultLocalEndPoints.Count == 0 ? null : _defaultLocalEndPoints[0]; }
            set
            {
                lock (BindLock)
                {
                    if (_boundEndPoints.Count > 0)
                    {
                        throw new InvalidOperationException("LocalEndPoint can't be set while the acceptor is bound.");
                    }

                    _defaultLocalEndPoints.Clear();
                    _defaultLocalEndPoints.Add(value);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<EndPoint> LocalEndPoints => _boundEndPoints;

        /// <inheritdoc/>
        public EndPoint LocalEndPoint => _boundEndPoints.Count == 0 ? null : _boundEndPoints[0];

        /// <inheritdoc/>
        public void Bind()
        {
            Bind(DefaultLocalEndPoints);
        }

        /// <inheritdoc/>
        public void Bind(EndPoint localEp)
        {
            if (localEp == null)
            {
                throw new ArgumentNullException(nameof(localEp));
            }
            Bind(new EndPoint[] {localEp});
        }

        /// <inheritdoc/>
        public void Bind(params EndPoint[] localEndPoints)
        {
            if (localEndPoints == null)
            {
                throw new ArgumentNullException(nameof(localEndPoints));
            }
            if (localEndPoints.Length == 0)
            {
                Bind(DefaultLocalEndPoints);
            }
            else
            {
                Bind((IEnumerable<EndPoint>) localEndPoints);
            }
        }

        /// <inheritdoc/>
        public void Bind(IEnumerable<EndPoint> localEndPoints)
        {
            if (Disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            if (localEndPoints == null)
            {
                throw new ArgumentNullException(nameof(localEndPoints));
            }

            var localEndPointsCopy = new List<EndPoint>();
            foreach (var ep in localEndPoints)
            {
                if (ep != null && !TransportMetadata.EndPointType.IsAssignableFrom(ep.GetType()))
                {
                    throw new ArgumentException("localAddress type: " + ep.GetType().Name + " (expected: "
                                                + TransportMetadata.EndPointType.Name + ")");
                }
                localEndPointsCopy.Add(ep);
            }

            if (localEndPointsCopy.Count == 0)
            {
                throw new ArgumentException("localEndPoints is empty.", nameof(localEndPoints));
            }

            var active = false;
            lock (BindLock)
            {
                if (_boundEndPoints.Count == 0)
                {
                    active = true;
                }

                var eps = BindInternal(localEndPointsCopy);
                _boundEndPoints.AddRange(eps);
            }

            if (active)
            {
                ((IOServiceSupport) this).FireServiceActivated();
            }
        }

        /// <inheritdoc/>
        public void Unbind()
        {
            Unbind(LocalEndPoints);
        }

        /// <inheritdoc/>
        public void Unbind(EndPoint localEp)
        {
            if (localEp == null)
            {
                throw new ArgumentNullException(nameof(localEp));
            }
            Unbind(new EndPoint[] {localEp});
        }

        /// <inheritdoc/>
        public void Unbind(params EndPoint[] localEndPoints)
        {
            Unbind((IEnumerable<EndPoint>) localEndPoints);
        }

        /// <inheritdoc/>
        public void Unbind(IEnumerable<EndPoint> localEndPoints)
        {
            if (localEndPoints == null)
            {
                throw new ArgumentNullException(nameof(localEndPoints));
            }

            var deactivate = false;
            lock (BindLock)
            {
                if (_boundEndPoints.Count == 0)
                {
                    return;
                }

                try
                {
                    UnbindInternal(localEndPoints);
                }
                catch
                {
                    throw;
                }

                if (_boundEndPoints == localEndPoints)
                {
                    _boundEndPoints.Clear();
                }
                else
                {
                    foreach (var ep in localEndPoints)
                    {
                        _boundEndPoints.Remove(ep);
                    }
                }

                if (_boundEndPoints.Count == 0)
                {
                    deactivate = true;
                }
            }

            if (deactivate)
            {
                ((IOServiceSupport) this).FireServiceDeactivated();
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var m = TransportMetadata;
            return '('
                   + m.ProviderName
                   + ' '
                   + m.Name
                   + " acceptor: "
                   + (Active
                       ? "localAddress(es): " + LocalEndPoints + ", managedSessionCount: "
                         + ManagedSessions.Count
                       : "not bound") + ')';
        }

        /// <summary>
        /// Implement this method to perform the actual bind operation.
        /// </summary>
        /// <param name="localEndPoints">the endpoints to bind</param>
        /// <returns>the endpoints which is bound actually</returns>
        protected abstract IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints);

        /// <summary>
        /// Implement this method to perform the actual unbind operation.
        /// </summary>
        /// <param name="localEndPoints">the endpoints to unbind</param>
        protected abstract void UnbindInternal(IEnumerable<EndPoint> localEndPoints);
    }
}
