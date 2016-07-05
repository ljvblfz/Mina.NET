using System.Collections.Generic;
using System.IO;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// Binds the specified <see cref="IOHandler"/> to the specified
    /// <see cref="LoopbackEndPoint"/>.
    /// </summary>
    public class LoopbackAcceptor : AbstractIOAcceptor
    {
        internal static readonly Dictionary<EndPoint, LoopbackPipe> BoundHandlers
            = new Dictionary<EndPoint, LoopbackPipe>();
        private IdleStatusChecker _idleStatusChecker;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public LoopbackAcceptor()
            : base(new DefaultLoopbackSessionConfig())
        {
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => LoopbackSession.Metadata;

        /// <inheritdoc/>
        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            var newLocalEPs = new HashSet<EndPoint>();

            lock (BoundHandlers)
            {
                foreach (var ep in localEndPoints)
                {
                    var localEp = ep as LoopbackEndPoint;
                    if (localEp == null || localEp.Port == 0)
                    {
                        localEp = null;
                        for (var i = 10000; i < int.MaxValue; i++)
                        {
                            var newLocalEp = new LoopbackEndPoint(i);
                            if (!BoundHandlers.ContainsKey(newLocalEp) && !newLocalEPs.Contains(newLocalEp))
                            {
                                localEp = newLocalEp;
                                break;
                            }
                        }

                        if (localEp == null)
                            throw new IOException("No port available.");
                    }
                    else if (localEp.Port < 0)
                    {
                        throw new IOException("Bind port number must be 0 or above.");
                    }
                    else if (BoundHandlers.ContainsKey(localEp))
                    {
                        throw new IOException("Address already bound: " + localEp);
                    }

                    newLocalEPs.Add(localEp);
                }

                foreach (LoopbackEndPoint localEp in newLocalEPs)
                {
                    if (BoundHandlers.ContainsKey(localEp))
                    {
                        foreach (LoopbackEndPoint ep in newLocalEPs)
                        {
                            BoundHandlers.Remove(ep);
                        }
                        throw new IOException("Duplicate local address: " + localEp);
                    }
                    BoundHandlers[localEp] = new LoopbackPipe(this, localEp, Handler);
                }
            }

            _idleStatusChecker.Start();

            return newLocalEPs;
        }

        /// <inheritdoc/>
        protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            lock (BoundHandlers)
            {
                foreach (var ep in localEndPoints)
                {
                    BoundHandlers.Remove(ep);
                }
            }

            if (BoundHandlers.Count == 0)
            {
                _idleStatusChecker.Stop();
            }
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

        internal void DoFinishSessionInitialization(IOSession session, IOFuture future)
        {
            InitSession(session, future, null);
        }
    }
}
