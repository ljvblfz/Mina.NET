using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Filter.Ssl;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IOSession"/> for socket transport (TCP/IP).
    /// </summary>
    public class AsyncSocketSession : SocketSession
    {
        /// <summary>
        /// Transport metadata for async socket session.
        /// </summary>
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("async", "socket", false, true, typeof(IPEndPoint));

        private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="service">the service this session belongs to</param>
        /// <param name="processor">the processor to process this session</param>
        /// <param name="socket">the associated socket</param>
        /// <param name="readBuffer">the <see cref="SocketAsyncEventArgsBuffer"/> as reading buffer</param>
        /// <param name="writeBuffer">the <see cref="SocketAsyncEventArgsBuffer"/> as writing buffer</param>
        /// <param name="reuseBuffer">whether or not reuse internal buffer, see <seealso cref="SocketSession.ReuseBuffer"/> for more</param>
        public AsyncSocketSession(IOService service, IIOProcessor<SocketSession> processor,
            System.Net.Sockets.Socket socket,
            SocketAsyncEventArgsBuffer readBuffer, SocketAsyncEventArgsBuffer writeBuffer, bool reuseBuffer)
            : base(
                service, processor, new SessionConfigImpl(socket), socket, socket.LocalEndPoint, socket.RemoteEndPoint,
                reuseBuffer)
        {
            ReadBuffer = readBuffer;
            ReadBuffer.SocketAsyncEventArgs.UserToken = this;
            WriteBuffer = writeBuffer;
            WriteBuffer.SocketAsyncEventArgs.UserToken = this;
            _completeHandler = saea_Completed;
        }

        /// <summary>
        /// Gets the reading buffer belonged to this session.
        /// </summary>
        public SocketAsyncEventArgsBuffer ReadBuffer { get; }

        /// <summary>
        /// Gets the writing buffer belonged to this session.
        /// </summary>
        public SocketAsyncEventArgsBuffer WriteBuffer { get; }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => Metadata;

        /// <inheritdoc/>
        public override bool Secured
        {
            get
            {
                var chain = FilterChain;
                var sslFilter = (SslFilter) chain.Get(typeof(SslFilter));
                return sslFilter != null && sslFilter.IsSslStarted(this);
            }
        }

        /// <inheritdoc/>
        protected override void BeginSend(IWriteRequest request, IOBuffer buf)
        {
            SocketAsyncEventArgs saea;
            var saeaBuf = buf as SocketAsyncEventArgsBuffer;
            if (saeaBuf == null)
            {
                WriteBuffer.Clear();
                if (WriteBuffer.Remaining < buf.Remaining)
                {
                    var oldLimit = buf.Limit;
                    buf.Limit = buf.Position + WriteBuffer.Remaining;
                    WriteBuffer.Put(buf);
                    buf.Limit = oldLimit;
                }
                else
                {
                    WriteBuffer.Put(buf);
                }
                WriteBuffer.Flip();
                WriteBuffer.SetBuffer();
                saea = WriteBuffer.SocketAsyncEventArgs;
            }
            else
            {
                saea = saeaBuf.SocketAsyncEventArgs;
                saea.Completed += _completeHandler;
            }

            bool willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.SendAsync(saea);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndSend(ex);
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessSend(saea);
            }
        }

        /// <inheritdoc/>
        protected override void BeginSendFile(IWriteRequest request, IFileRegion file)
        {
            var saea = WriteBuffer.SocketAsyncEventArgs;
            saea.SendPacketsElements = new SendPacketsElement[]
            {
                new SendPacketsElement(file.FullName)
            };

            bool willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.SendPacketsAsync(saea);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndSend(ex);
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessSend(saea);
            }
        }

        void saea_Completed(object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= _completeHandler;
            ProcessSend(e);
        }

        /// <summary>
        /// Processes send events.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                EndSend(e.BytesTransferred);
            }
            else if (e.SocketError != SocketError.OperationAborted
                     && e.SocketError != SocketError.Interrupted
                     && e.SocketError != SocketError.ConnectionReset)
            {
                EndSend(new SocketException((int) e.SocketError));
            }
            else
            {
                // closed
                Processor.Remove(this);
            }
        }

        /// <inheritdoc/>
        protected override void BeginReceive()
        {
            ReadBuffer.Clear();

            bool willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.ReceiveAsync(ReadBuffer.SocketAsyncEventArgs);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndReceive(ex);
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessReceive(ReadBuffer.SocketAsyncEventArgs);
            }
        }

        /// <summary>
        /// Processes receive events.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    ReadBuffer.Position = e.BytesTransferred;
                    ReadBuffer.Flip();

                    if (ReuseBuffer)
                    {
                        EndReceive(ReadBuffer);
                    }
                    else
                    {
                        var buf = IOBuffer.Allocate(ReadBuffer.Remaining);
                        buf.Put(ReadBuffer);
                        buf.Flip();
                        EndReceive(buf);
                    }

                    return;
                }
                // closed
                //Processor.Remove(this);
                FilterChain.FireInputClosed();
            }
            else if (e.SocketError != SocketError.OperationAborted
                     && e.SocketError != SocketError.Interrupted
                     && e.SocketError != SocketError.ConnectionReset)
            {
                EndReceive(new SocketException((int) e.SocketError));
            }
            else
            {
                // closed
                Processor.Remove(this);
            }
        }
    }
}
