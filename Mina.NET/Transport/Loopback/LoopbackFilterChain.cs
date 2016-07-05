using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Loopback
{
    class LoopbackFilterChain : VirtualDefaultIOFilterChain
    {
        private readonly ConcurrentQueue<IOEvent> _eventQueue = new ConcurrentQueue<IOEvent>();
        private volatile bool _flushEnabled;
        private volatile bool _sessionOpened;

        /// <summary>
        /// </summary>
        public LoopbackFilterChain(AbstractIOSession session)
            : base(session)
        {
            Processor = new LoopbackIoProcessor(this);
        }

        public void Start()
        {
            _flushEnabled = true;
            FlushEvents();
            FlushPendingDataQueues((LoopbackSession)Session);
        }

        internal IIoProcessor<LoopbackSession> Processor { get; }

        private void PushEvent(IOEvent e)
        {
            PushEvent(e, _flushEnabled);
        }

        private void PushEvent(IOEvent e, bool flushNow)
        {
            _eventQueue.Enqueue(e);
            if (flushNow)
                FlushEvents();
        }

        private void FlushEvents()
        {
            IOEvent e;
            while (_eventQueue.TryDequeue(out e))
            {
                FireEvent(e);
            }
        }

        private void FireEvent(IOEvent e)
        {
            var session = (LoopbackSession)Session;
            var data = e.Parameter;
            switch (e.EventType)
            {
                case IoEventType.MessageReceived:
                    if (_sessionOpened && (!session.ReadSuspended) && Monitor.TryEnter(session.Lock))
                    {
                        try
                        {
                            if (session.ReadSuspended)
                            {
                                session.ReceivedMessageQueue.Enqueue(data);
                            }
                            else
                            {
                                base.FireMessageReceived(data);
                            }
                        }
                        finally
                        {
                            Monitor.Exit(session.Lock);
                        }
                    }
                    else
                    {
                        session.ReceivedMessageQueue.Enqueue(data);
                    }
                    break;
                case IoEventType.Write:
                    base.FireFilterWrite((IWriteRequest)data);
                    break;
                case IoEventType.MessageSent:
                    base.FireMessageSent((IWriteRequest)data);
                    break;
                case IoEventType.ExceptionCaught:
                    base.FireExceptionCaught((Exception)data);
                    break;
                case IoEventType.SessionCreated:
                    Monitor.Enter(session.Lock);
                    try
                    {
                        base.FireSessionCreated();
                    }
                    finally
                    {
                        Monitor.Exit(session.Lock);
                    }
                    break;
                case IoEventType.SessionOpened:
                    base.FireSessionOpened();
                    _sessionOpened = true;
                    break;
                case IoEventType.SessionIdle:
                    base.FireSessionIdle((IdleStatus)data);
                    break;
                case IoEventType.SessionClosed:
                    FlushPendingDataQueues(session);
                    base.FireSessionClosed();
                    break;
                case IoEventType.Close:
                    base.FireFilterClose();
                    break;
                default:
                    break;
            }
        }

        private static void FlushPendingDataQueues(LoopbackSession s)
        {
            s.Processor.UpdateTrafficControl(s);
            s.RemoteSession.Processor.UpdateTrafficControl(s);
        }

        public override void FireSessionCreated()
        {
            PushEvent(new IOEvent(IoEventType.SessionCreated, Session, null));
        }

        public override void FireSessionOpened()
        {
            PushEvent(new IOEvent(IoEventType.SessionOpened, Session, null));
        }

        public override void FireSessionClosed()
        {
            PushEvent(new IOEvent(IoEventType.SessionClosed, Session, null));
        }

        public override void FireSessionIdle(IdleStatus status)
        {
            PushEvent(new IOEvent(IoEventType.SessionIdle, Session, status));
        }

        public override void FireMessageReceived(object message)
        {
            PushEvent(new IOEvent(IoEventType.MessageReceived, Session, message));
        }

        public override void FireMessageSent(IWriteRequest request)
        {
            PushEvent(new IOEvent(IoEventType.MessageSent, Session, request));
        }

        public override void FireExceptionCaught(Exception cause)
        {
            PushEvent(new IOEvent(IoEventType.ExceptionCaught, Session, cause));
        }

        public override void FireFilterWrite(IWriteRequest writeRequest)
        {
            PushEvent(new IOEvent(IoEventType.Write, Session, writeRequest));
        }

        public override void FireFilterClose()
        {
            PushEvent(new IOEvent(IoEventType.Close, Session, null));
        }

        class LoopbackIoProcessor : IIoProcessor<LoopbackSession>
        {
            private readonly LoopbackFilterChain _chain;

            public LoopbackIoProcessor(LoopbackFilterChain chain)
            {
                _chain = chain;
            }

            public void Add(LoopbackSession session)
            {
                // do nothing
            }

            public void Write(LoopbackSession session, IWriteRequest writeRequest)
            {
                session.WriteRequestQueue.Offer(session, writeRequest);

                if (!session.WriteSuspended)
                {
                    Flush(session);
                }
            }

            public void Flush(LoopbackSession session)
            {
                var queue = session.WriteRequestQueue;
                if (!session.Closing)
                {
                    lock (session.Lock)
                    {
                        try
                        {
                            if (queue.IsEmpty(session))
                                return;

                            IWriteRequest req;
                            var currentTime = DateTime.Now;
                            while ((req = queue.Poll(session)) != null)
                            {
                                var m = req.Message;
                                _chain.PushEvent(new IOEvent(IoEventType.MessageSent, session, req), false);
                                session.RemoteSession.FilterChain.FireMessageReceived(GetMessageCopy(m));
                                var buf = m as IOBuffer;
                                if (buf != null)
                                    session.IncreaseWrittenBytes(buf.Remaining, currentTime);
                            }
                        }
                        finally
                        {
                            if (_chain._flushEnabled)
                                _chain.FlushEvents();
                        }
                    }

                    FlushPendingDataQueues(session);
                }
                else
                {
                    var failedRequests = new List<IWriteRequest>();
                    IWriteRequest req;
                    while ((req = queue.Poll(session)) != null)
                    {
                        failedRequests.Add(req);
                    }

                    if (failedRequests.Count > 0)
                    {
                        var cause = new WriteToClosedSessionException(failedRequests);
                        foreach (var r in failedRequests)
                        {
                            r.Future.Exception = cause;
                        }
                        session.FilterChain.FireExceptionCaught(cause);
                    }
                }
            }

            public void Remove(LoopbackSession session)
            {
                lock (session.Lock)
                {
                    if (!session.CloseFuture.Closed)
                    {
                        var support = session.Service as IOServiceSupport;
                        if (support != null)
                            support.FireSessionDestroyed(session);
                        session.RemoteSession.Close(true);
                    }
                }
            }

            public void UpdateTrafficControl(LoopbackSession session)
            {
                if (!session.ReadSuspended)
                {
                    var queue = session.ReceivedMessageQueue;
                    object item;
                    while (queue.TryDequeue(out item))
                    {
                        _chain.FireMessageReceived(item);
                    }
                }

                if (!session.WriteSuspended)
                {
                    Flush(session);
                }
            }

            private object GetMessageCopy(object message)
            {
                var messageCopy = message;
                var rb = message as IOBuffer;
                if (rb != null)
                {
                    rb.Mark();
                    var wb = IOBuffer.Allocate(rb.Remaining);
                    wb.Put(rb);
                    wb.Flip();
                    rb.Reset();
                    messageCopy = wb;
                }
                return messageCopy;
            }

            void IOProcessor.Write(IOSession session, IWriteRequest writeRequest)
            {
                Write((LoopbackSession)session, writeRequest);
            }

            void IOProcessor.Flush(IOSession session)
            {
                Flush((LoopbackSession)session);
            }

            void IOProcessor.Add(IOSession session)
            {
                Add((LoopbackSession)session);
            }

            void IOProcessor.Remove(IOSession session)
            {
                Remove((LoopbackSession)session);
            }

            void IOProcessor.UpdateTrafficControl(IOSession session)
            {
                UpdateTrafficControl((LoopbackSession)session);
            }
        }
    }
}
