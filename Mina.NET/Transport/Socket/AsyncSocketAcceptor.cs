using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Common.Logging;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOAcceptor"/> for socket transport (TCP/IP).
    /// </summary>
    public class AsyncSocketAcceptor : AbstractSocketAcceptor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AsyncSocketAcceptor));

        const int OpsToPreAlloc = 2;
        private BufferManager _bufferManager;
        private Pool<SocketAsyncEventArgsBuffer> _readWritePool;

        /// <summary>
        /// Instantiates with default max connections of 1024.
        /// </summary>
        public AsyncSocketAcceptor()
            : this(1024)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="maxConnections">the max connections allowed</param>
        public AsyncSocketAcceptor(int maxConnections)
            : base(maxConnections)
        {
            SessionDestroyed += OnSessionDestroyed;
        }

        /// <inheritdoc/>
        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            InitBuffer();
            return base.BindInternal(localEndPoints);
        }

        private void InitBuffer()
        {
            var bufferSize = SessionConfig.ReadBufferSize;
            if (_bufferManager == null || _bufferManager.BufferSize != bufferSize)
            {
                // TODO free previous pool

                _bufferManager = new BufferManager(bufferSize * MaxConnections * OpsToPreAlloc, bufferSize);
                _bufferManager.InitBuffer();

                var list = new List<SocketAsyncEventArgsBuffer>(MaxConnections * OpsToPreAlloc);
                for (var i = 0; i < MaxConnections * OpsToPreAlloc; i++)
                {
                    var readWriteEventArg = new SocketAsyncEventArgs();
                    _bufferManager.SetBuffer(readWriteEventArg);
                    var buf = new SocketAsyncEventArgsBuffer(readWriteEventArg);
                    list.Add(buf);

                    readWriteEventArg.Completed += readWriteEventArg_Completed;
                }
                _readWritePool = new Pool<SocketAsyncEventArgsBuffer>(list);
            }
        }

        void readWriteEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            var session = e.UserToken as AsyncSocketSession;

            if (session == null)
                return;

            if (e.LastOperation == SocketAsyncOperation.Receive)
            {
                session.ProcessReceive(e);
            }
            else if (e.LastOperation == SocketAsyncOperation.Send
                || e.LastOperation == SocketAsyncOperation.SendPackets)
            {
                session.ProcessSend(e);
            }
        }

        private void OnSessionDestroyed(object sender, IOSessionEventArgs e)
        {
            var s = e.Session as AsyncSocketSession;
            if (s != null && _readWritePool != null)
            {
                // clear the buffer and reset its count to original capacity if changed
                s.ReadBuffer.Clear();
                s.ReadBuffer.SetBuffer();
                _readWritePool.Push(s.ReadBuffer);

                s.WriteBuffer.Clear();
                s.WriteBuffer.SetBuffer();
                _readWritePool.Push(s.WriteBuffer);
            }
        }

        /// <inheritdoc/>
        protected override void BeginAccept(ListenerContext listener)
        {
            var acceptEventArg = (SocketAsyncEventArgs)listener.Tag;
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.UserToken = listener;
                acceptEventArg.Completed += AcceptEventArg_Completed;
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            bool willRaiseEvent;
            try
            {
                willRaiseEvent = listener.Socket.AcceptAsync(acceptEventArg);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                EndAccept(e.AcceptSocket, (ListenerContext)e.UserToken);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((int)e.SocketError));
            }
        }

        /// <inheritdoc/>
        protected override IOSession NewSession(IIOProcessor<SocketSession> processor, System.Net.Sockets.Socket socket)
        {
            var readBuffer = _readWritePool.Pop();
            var writeBuffer = _readWritePool.Pop();

            if (readBuffer == null)
            {
                readBuffer =
                    SocketAsyncEventArgsBufferAllocator.Instance.Allocate(SessionConfig.ReadBufferSize);
                readBuffer.SocketAsyncEventArgs.Completed += readWriteEventArg_Completed;
            }

            if (writeBuffer == null)
            {
                writeBuffer =
                    SocketAsyncEventArgsBufferAllocator.Instance.Allocate(SessionConfig.ReadBufferSize);
                writeBuffer.SocketAsyncEventArgs.Completed += readWriteEventArg_Completed;
            }

            return new AsyncSocketSession(this, processor, socket, readBuffer, writeBuffer, ReuseBuffer);
        }
    }
}
