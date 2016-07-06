using System;
using System.Collections.Generic;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    class AsyncSocketProcessor : IIOProcessor<SocketSession>, IDisposable
    {
        public AsyncSocketProcessor(Func<IEnumerable<IOSession>> getSessionsFunc)
        {
            IdleStatusChecker = new IdleStatusChecker(getSessionsFunc);
        }

        public IdleStatusChecker IdleStatusChecker { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IdleStatusChecker.Dispose();
            }
        }

        public void Add(SocketSession session)
        {
            // Build the filter chain of this session.
            var chainBuilder = session.Service.FilterChainBuilder;
            chainBuilder.BuildFilterChain(session.FilterChain);

            // Propagate the SESSION_CREATED event up to the chain
            var serviceSupport = session.Service as IOServiceSupport;
            if (serviceSupport != null)
                serviceSupport.FireSessionCreated(session);

            session.Start();
        }

        public void Remove(SocketSession session)
        {
            ClearWriteRequestQueue(session);

            if (session.Socket.Connected)
            {
                try
                {
                    session.Socket.Shutdown(System.Net.Sockets.SocketShutdown.Send);
                }
                catch { /* the session has already closed */ }
            }
            session.Socket.Close();

            var support = session.Service as IOServiceSupport;
            if (support != null)
                support.FireSessionDestroyed(session);
        }

        public void Write(SocketSession session, IWriteRequest writeRequest)
        {
            var writeRequestQueue = session.WriteRequestQueue;
            writeRequestQueue.Offer(session, writeRequest);
            if (!session.WriteSuspended)
                Flush(session);
        }

        public void Flush(SocketSession session)
        {
            session.Flush();
        }

        public void UpdateTrafficControl(SocketSession session)
        {
            if (!session.ReadSuspended)
                session.Start();

            if (!session.WriteSuspended)
                Flush(session);
        }

        private void ClearWriteRequestQueue(SocketSession session)
        {
            var writeRequestQueue = session.WriteRequestQueue;
            IWriteRequest req;
            var failedRequests = new List<IWriteRequest>();

            if ((req = writeRequestQueue.Poll(session)) != null)
            {
                var buf = req.Message as IOBuffer;
                if (buf != null)
                {
                    // The first unwritten empty buffer must be
                    // forwarded to the filter chain.
                    if (buf.HasRemaining)
                    {
                        buf.Reset();
                        failedRequests.Add(req);
                    }
                    else
                    {
                        session.FilterChain.FireMessageSent(req);
                    }
                }
                else
                {
                    failedRequests.Add(req);
                }

                // Discard others.
                while ((req = writeRequestQueue.Poll(session)) != null)
                {
                    failedRequests.Add(req);
                }
            }

            // Create an exception and notify.
            if (failedRequests.Count > 0)
            {
                var cause = new WriteToClosedSessionException(failedRequests);

                foreach (var r in failedRequests)
                {
                    //session.DecreaseScheduledBytesAndMessages(r);
                    r.Future.Exception = cause;
                }

                session.FilterChain.FireExceptionCaught(cause);
            }
        }

        void IOProcessor.Write(IOSession session, IWriteRequest writeRequest)
        {
            Write((SocketSession)session, writeRequest);
        }

        void IOProcessor.Flush(IOSession session)
        {
            Flush((SocketSession)session);
        }

        void IOProcessor.Add(IOSession session)
        {
            Add((SocketSession)session);
        }

        void IOProcessor.Remove(IOSession session)
        {
            Remove((SocketSession)session);
        }

        void IOProcessor.UpdateTrafficControl(IOSession session)
        {
            UpdateTrafficControl((SocketSession)session);
        }
    }
}
