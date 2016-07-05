using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// Base implementation of <see cref="IOSession"/> for socket transport (TCP/IP).
    /// </summary>
    public abstract class SocketSession : AbstractIoSession
    {
        private static readonly object Dummy = IOBuffer.Wrap(new byte[0]);
        private int _writing;
        private object _pendingReceivedMessage = Dummy;

        /// <summary>
        /// </summary>
        protected SocketSession(IOService service, IOProcessor processor, IOSessionConfig config,
            System.Net.Sockets.Socket socket, EndPoint localEp, EndPoint remoteEp, bool reuseBuffer)
            : base(service)
        {
            Socket = socket;
            LocalEndPoint = localEp;
            RemoteEndPoint = remoteEp;
            Config = config;
            if (service.SessionConfig != null)
                Config.SetAll(service.SessionConfig);
            Processor = processor;
            FilterChain = new DefaultIOFilterChain(this);
        }

        /// <inheritdoc/>
        public override IOProcessor Processor { get; }

        /// <inheritdoc/>
        public override IOFilterChain FilterChain { get; }

        /// <inheritdoc/>
        public override EndPoint LocalEndPoint { get; }

        /// <inheritdoc/>
        public override EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Gets the <see cref="System.Net.Sockets.Socket"/>
        /// associated with this session.
        /// </summary>
        public System.Net.Sockets.Socket Socket { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the internal
        /// read buffer as the buffer sent to <see cref="SocketSession.FilterChain"/>
        /// by <see cref="IOFilterChain.FireMessageReceived(object)"/>.
        /// </summary>
        /// <remarks>
        /// If any thread model, i.e. an <see cref="Filter.Executor.ExecutorFilter"/>,
        /// is added before filters that process the incoming <see cref="Core.Buffer.IOBuffer"/>
        /// in <see cref="IOFilter.MessageReceived(Core.FilterchIOSessionSession.IoSession, object)"/>,
        /// this must be set to <code>false</code> since the internal read buffer
        /// will be reset every time a session begins to receive.
        /// </remarks>
        /// <seealso cref="AbstractSocketAcceptor.ReuseBuffer"/>
        public bool ReuseBuffer { get; set; }

        /// <summary>
        /// Starts this session.
        /// </summary>
        public void Start()
        {
            if (ReadSuspended)
                return;

            if (_pendingReceivedMessage != null)
            {
                if (!ReferenceEquals(_pendingReceivedMessage, Dummy))
                    FilterChain.FireMessageReceived(_pendingReceivedMessage);
                _pendingReceivedMessage = null;
                BeginReceive();
            }
        }

        /// <summary>
        /// Flushes this session.
        /// </summary>
        public void Flush()
        {
            if (WriteSuspended)
                return;
            if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                return;
            BeginSend();
        }

        private void BeginSend()
        {
            var req = CurrentWriteRequest;
            if (req == null)
            {
                req = WriteRequestQueue.Poll(this);

                if (req == null)
                {
                    Interlocked.Exchange(ref _writing, 0);
                    return;
                }
                
                CurrentWriteRequest = req;
            }

            var buf = req.Message as IOBuffer;

            if (buf == null)
            {
                var file = req.Message as IFileRegion;
                if (file == null)
                    EndSend(new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?"),
                            true);
                else
                    BeginSendFile(req, file);
            }
            else if (buf.HasRemaining)
            {
                BeginSend(req, buf);
            }
            else
            {
                EndSend(0);
            }
        }

        /// <summary>
        /// Begins send operation.
        /// </summary>
        /// <param name="request">the current write request</param>
        /// <param name="buf">the buffer to send</param>
        protected abstract void BeginSend(IWriteRequest request, IOBuffer buf);

        /// <summary>
        /// Begins to send a file.
        /// </summary>
        /// <param name="request">the current write request</param>
        /// <param name="file">the file to send</param>
        protected abstract void BeginSendFile(IWriteRequest request, IFileRegion file);

        /// <summary>
        /// Ends send operation.
        /// </summary>
        /// <param name="bytesTransferred">the bytes transferred in last send operation</param>
        protected void EndSend(int bytesTransferred)
        {
            IncreaseWrittenBytes(bytesTransferred, DateTime.Now);

            var req = CurrentWriteRequest;
            if (req != null)
            {
                var buf = req.Message as IOBuffer;
                if (buf == null)
                {
                    var file = req.Message as IFileRegion;
                    if (file != null)
                    {
                        FireMessageSent(req);
                    }
                }
                else if (!buf.HasRemaining)
                {
                    // Buffer has been sent, clear the current request.
                    var pos = buf.Position;
                    buf.Reset();

                    FireMessageSent(req);

                    // And set it back to its position
                    buf.Position = pos;

                    buf.Free();
                }
            }

            if (Socket.Connected)
                BeginSend();
        }

        /// <summary>
        /// Ends send operation.
        /// </summary>
        /// <param name="ex">the exception caught</param>
        protected void EndSend(Exception ex)
        {
            EndSend(ex, false);
        }

        /// <summary>
        /// Ends send operation.
        /// </summary>
        /// <param name="ex">the exception caught</param>
        /// <param name="discardWriteRequest">discard the current write quest or not</param>
        protected void EndSend(Exception ex, bool discardWriteRequest)
        {
            var req = CurrentWriteRequest;
            if (req != null)
            {
                req.Future.Exception = ex;
                if (discardWriteRequest)
                {
                    CurrentWriteRequest = null;
                    var buf = req.Message as IOBuffer;
                    if (buf != null)
                        buf.Free();
                }
            }
            FilterChain.FireExceptionCaught(ex);
            if (Socket.Connected)
                BeginSend();
        }

        /// <summary>
        /// Begins receive operation.
        /// </summary>
        protected abstract void BeginReceive();

        /// <summary>
        /// Ends receive operation.
        /// </summary>
        /// <param name="buf">the buffer received in last receive operation</param>
        protected void EndReceive(IOBuffer buf)
        {
            if (ReadSuspended)
            {
                _pendingReceivedMessage = buf;
            }
            else
            {
                FilterChain.FireMessageReceived(buf);

                if (Socket.Connected)
                    BeginReceive();
            }
        }

        /// <summary>
        /// Ends receive operation.
        /// </summary>
        /// <param name="ex">the exception caught</param>
        protected void EndReceive(Exception ex)
        {
            FilterChain.FireExceptionCaught(ex);
            if (Socket.Connected && !ReadSuspended)
                BeginReceive();
        }

        private void FireMessageSent(IWriteRequest req)
        {
            CurrentWriteRequest = null;
            try
            {
                FilterChain.FireMessageSent(req);
            }
            catch (Exception ex)
            {
                FilterChain.FireExceptionCaught(ex);
            }
        }
    }
}
