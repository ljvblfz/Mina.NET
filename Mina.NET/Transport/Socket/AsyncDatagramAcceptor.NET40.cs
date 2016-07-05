using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    partial class AsyncDatagramAcceptor : AbstractIOAcceptor, IDatagramAcceptor
    {
        private void BeginReceive(SocketContext ctx)
        {
            if (ctx.ReceiveBuffer == null)
            {
                var buffer = new byte[SessionConfig.ReadBufferSize];
                ctx.ReceiveBuffer = new SocketAsyncEventArgs();
                ctx.ReceiveBuffer.SetBuffer(buffer, 0, buffer.Length);
                ctx.ReceiveBuffer.Completed += OnCompleted;
                ctx.ReceiveBuffer.UserToken = ctx;
            }

            ctx.ReceiveBuffer.RemoteEndPoint = new IPEndPoint(ctx.Socket.AddressFamily == AddressFamily.InterNetwork ?
                IPAddress.Any : IPAddress.IPv6Any, 0);

            bool willRaiseEvent;
            try
            {
                willRaiseEvent = ctx.Socket.ReceiveFromAsync(ctx.ReceiveBuffer);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessReceive(ctx.ReceiveBuffer);
            }
        }

        void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var buf = IOBuffer.Allocate(e.BytesTransferred);
                buf.Put(e.Buffer, e.Offset, e.BytesTransferred);
                buf.Flip();
                EndReceive((SocketContext)e.UserToken, buf, e.RemoteEndPoint);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((int)e.SocketError));
            }
        }

        partial class SocketContext
        {
            public SocketAsyncEventArgs ReceiveBuffer;
            private SocketAsyncEventArgsBuffer _writeBuffer;
            private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

            public SocketContext(System.Net.Sockets.Socket socket, IOSessionConfig config)
            {
                _socket = socket;

                _completeHandler = OnCompleted;

                var writeBuffer = new byte[config.ReadBufferSize];
                _writeBuffer = SocketAsyncEventArgsBufferAllocator.Instance.Wrap(writeBuffer);
                _writeBuffer.SocketAsyncEventArgs.Completed += OnCompleted;
                _writeBuffer.SocketAsyncEventArgs.UserToken = this;
            }

            public void Close()
            {
                _socket.Close();
                ReceiveBuffer.Dispose();
                _writeBuffer.Dispose();
            }

            private void BeginSend(AsyncDatagramSession session, IOBuffer buf, EndPoint remoteEp)
            {
                _writeBuffer.Clear();

                SocketAsyncEventArgs saea;
                var saeaBuf = buf as SocketAsyncEventArgsBuffer;
                if (saeaBuf == null)
                {
                    if (_writeBuffer.Remaining < buf.Remaining)
                    {
                        // TODO allocate a temp buffer
                    }
                    else
                    {
                        _writeBuffer.Put(buf);
                    }
                    _writeBuffer.Flip();
                    saea = _writeBuffer.SocketAsyncEventArgs;
                    saea.SetBuffer(saea.Offset + _writeBuffer.Position, _writeBuffer.Limit);
                }
                else
                {
                    saea = saeaBuf.SocketAsyncEventArgs;
                    saea.Completed += _completeHandler;
                }

                saea.UserToken = session;
                saea.RemoteEndPoint = remoteEp;

                bool willRaiseEvent;
                try
                {
                    willRaiseEvent = Socket.SendToAsync(saea);
                }
                catch (ObjectDisposedException)
                { 
                    // do nothing
                    return;
                }
                catch (Exception ex)
                {
                    EndSend(session, ex);
                    return;
                }
                if (!willRaiseEvent)
                {
                    ProcessSend(saea);
                }
            }

            void OnCompleted(object sender, SocketAsyncEventArgs e)
            {
                if (e != _writeBuffer.SocketAsyncEventArgs)
                {
                    e.Completed -= _completeHandler;
                }
                ProcessSend(e);
            }

            private void ProcessSend(SocketAsyncEventArgs e)
            {
                if (e.SocketError == SocketError.Success)
                {
                    EndSend((AsyncDatagramSession)e.UserToken, e.BytesTransferred);
                }
                else
                {
                    EndSend((AsyncDatagramSession)e.UserToken, new SocketException((int)e.SocketError));
                }
            }
        }
    }
}
