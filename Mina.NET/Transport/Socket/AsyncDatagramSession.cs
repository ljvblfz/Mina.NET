using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IOSession"/> for datagram transport (UDP/IP).
    /// </summary>
    public partial class AsyncDatagramSession : SocketSession
    {

        /// <summary>
        /// Transport metadata for async datagram session.
        /// </summary>
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("async", "datagram", true, false, typeof(IPEndPoint));

        private int _scheduledForFlush;

        /// <summary>
        /// Creates a new acceptor-side session instance.
        /// </summary>
        internal AsyncDatagramSession(IOService service, IIOProcessor<AsyncDatagramSession> processor,
            AsyncDatagramAcceptor.SocketContext ctx, EndPoint remoteEp, bool reuseBuffer)
            : base(service, processor, new DefaultDatagramSessionConfig(), ctx.Socket, ctx.Socket.LocalEndPoint, remoteEp, reuseBuffer)
        {
            Context = ctx;
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => Metadata;

        internal AsyncDatagramAcceptor.SocketContext Context { get; }

        public bool IsScheduledForFlush => _scheduledForFlush != 0;

        public bool ScheduledForFlush()
        {
            return Interlocked.CompareExchange(ref _scheduledForFlush, 1, 0) == 0;
        }

        public void UnscheduledForFlush()
        {
            Interlocked.Exchange(ref _scheduledForFlush, 0);
        }

        /// <inheritdoc/>
        protected override void BeginSend(IWriteRequest request, IOBuffer buf)
        {
            var destination = request.Destination;
            if (destination == null)
                destination = RemoteEndPoint;
            BeginSend(buf, destination);
        }

        /// <inheritdoc/>
        protected override void BeginSendFile(IWriteRequest request, IFileRegion file)
        {
            EndSend(new InvalidOperationException("Cannot send a file via UDP"));
        }
    }
}
