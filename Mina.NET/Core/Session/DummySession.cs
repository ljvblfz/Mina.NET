using System;
using System.Collections.Generic;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// A dummy <see cref="IOSession"/> for unit-testing or non-network-use of
    /// the classes that depends on <see cref="IOSession"/>.
    /// </summary>
    public class DummySession : AbstractIOSession
    {
        private static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "dummy", false, false, typeof(IPEndPoint));

        private volatile IOHandler _handler = new IOHandlerAdapter();
        private readonly IIoProcessor<DummySession> _processor;
        private volatile EndPoint _remoteAddress = AnonymousEndPoint.Instance;
        private volatile ITransportMetadata _transportMetadata = Metadata;

        /// <summary>
        /// </summary>
        public DummySession()
            : base(new DummyService(new DummyConfig()))
        {
            _processor = new DummyProcessor();
            FilterChain = new DefaultIOFilterChain(this);

            IOSessionDataStructureFactory factory = new DefaultIoSessionDataStructureFactory();
            AttributeMap = factory.GetAttributeMap(this);
            SetWriteRequestQueue(factory.GetWriteRequestQueue(this));
        }

        /// <inheritdoc/>
        public override IOProcessor Processor => _processor;

        /// <inheritdoc/>
        public override IOHandler Handler => _handler;

        /// <inheritdoc/>
        public override IOFilterChain FilterChain { get; }

        /// <inheritdoc/>
        public override EndPoint LocalEndPoint => AnonymousEndPoint.Instance;

        /// <inheritdoc/>
        public override EndPoint RemoteEndPoint => _remoteAddress;

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => _transportMetadata;

        /// <summary>
        /// </summary>
        public void SetTransportMetadata(ITransportMetadata metadata)
        {
            _transportMetadata = metadata;
        }

        /// <summary>
        /// </summary>
        public void SetRemoteEndPoint(EndPoint ep)
        {
            _remoteAddress = ep;
        }

        /// <summary>
        /// </summary>
        public void SetHandler(IOHandler handler)
        {
            _handler = handler;
        }

        class DummyService : AbstractIOAcceptor
        {
            public DummyService(IOSessionConfig sessionConfig)
                : base(sessionConfig)
            { }

            public override ITransportMetadata TransportMetadata => Metadata;

            protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
            {
                throw new NotSupportedException();
            }

            protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
            {
                throw new NotImplementedException();
            }
        }

        class DummyProcessor : IIoProcessor<DummySession>
        {
            public void Add(DummySession session)
            {
                // Do nothing
            }

            public void Remove(DummySession session)
            {
                if (!session.CloseFuture.Closed)
                    session.FilterChain.FireSessionClosed();
            }

            public void Write(DummySession session, IWriteRequest writeRequest)
            {
                var queue = session.WriteRequestQueue;
                queue.Offer(session, writeRequest);
                if (!session.WriteSuspended)
                    Flush(session);
            }

            public void Flush(DummySession session)
            {
                var req = session.WriteRequestQueue.Poll(session);

                // Chek that the request is not null. If the session has been closed,
                // we may not have any pending requests.
                if (req != null)
                {
                    var m = req.Message;
                    session.FilterChain.FireMessageSent(req);
                }
            }

            public void UpdateTrafficControl(DummySession session)
            {
                // Do nothing
            }

            void IOProcessor.Write(IOSession session, IWriteRequest writeRequest)
            {
                Write((DummySession)session, writeRequest);
            }

            void IOProcessor.Flush(IOSession session)
            {
                Flush((DummySession)session);
            }

            void IOProcessor.Add(IOSession session)
            {
                Add((DummySession)session);
            }

            void IOProcessor.Remove(IOSession session)
            {
                Remove((DummySession)session);
            }

            void IOProcessor.UpdateTrafficControl(IOSession session)
            {
                UpdateTrafficControl((DummySession)session);
            }
        }

        class DummyConfig : AbstractIoSessionConfig
        {
            protected override void DoSetAll(IOSessionConfig config)
            {
                // Do nothing
            }
        }

        class AnonymousEndPoint : EndPoint
        {
            public static AnonymousEndPoint Instance = new AnonymousEndPoint();

            private AnonymousEndPoint() { }

            public override string ToString()
            {
                return "?";
            }
        }
    }
}
