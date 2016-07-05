using System.Collections.Concurrent;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// A <see cref="IOSession"/> for loopback transport.
    /// </summary>
    class LoopbackSession : AbstractIoSession
    {
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "loopback", false, false, typeof(LoopbackEndPoint));

        private readonly LoopbackEndPoint _localEp;
        private readonly LoopbackEndPoint _remoteEp;
        private readonly LoopbackFilterChain _filterChain;

        /// <summary>
        /// Constructor for client-side session.
        /// </summary>
        public LoopbackSession(IOService service, LoopbackEndPoint localEp,
            IOHandler handler, LoopbackPipe remoteEntry)
            : base(service)
        {
            Config = new DefaultLoopbackSessionConfig();
            Lock = new byte[0];
            _localEp = localEp;
            _remoteEp = remoteEntry.Endpoint;
            _filterChain = new LoopbackFilterChain(this);
            ReceivedMessageQueue = new ConcurrentQueue<object>();
            RemoteSession = new LoopbackSession(this, remoteEntry);
        }

        /// <summary>
        /// Constructor for server-side session.
        /// </summary>
        public LoopbackSession(LoopbackSession remoteSession, LoopbackPipe entry)
            : base(entry.Acceptor)
        {
            Config = new DefaultLoopbackSessionConfig();
            Lock = remoteSession.Lock;
            _localEp = remoteSession._remoteEp;
            _remoteEp = remoteSession._localEp;
            _filterChain = new LoopbackFilterChain(this);
            RemoteSession = remoteSession;
            ReceivedMessageQueue = new ConcurrentQueue<object>();
        }

        public override IOProcessor Processor => _filterChain.Processor;

        public override IOFilterChain FilterChain => _filterChain;

        public override EndPoint LocalEndPoint => _localEp;

        public override EndPoint RemoteEndPoint => _remoteEp;

        public override ITransportMetadata TransportMetadata => Metadata;

        public LoopbackSession RemoteSession { get; }

        internal ConcurrentQueue<object> ReceivedMessageQueue { get; }

        internal object Lock { get; }
    }
}
