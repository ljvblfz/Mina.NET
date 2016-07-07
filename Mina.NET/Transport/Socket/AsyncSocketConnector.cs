using System.Net.Sockets;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOConnector"/> for socket transport (TCP/IP).
    /// </summary>
    public class AsyncSocketConnector : AbstractSocketConnector, ISocketConnector
    {
        /// <summary>
        /// Instantiates.
        /// </summary>
        public AsyncSocketConnector()
            : base(new DefaultSocketSessionConfig())
        {
        }

        /// <inheritdoc/>
        public new ISocketSessionConfig SessionConfig => (ISocketSessionConfig) base.SessionConfig;

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => AsyncSocketSession.Metadata;

        /// <inheritdoc/>
        protected override System.Net.Sockets.Socket NewSocket(AddressFamily addressFamily)
        {
            return new System.Net.Sockets.Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <inheritdoc/>
        protected override void BeginConnect(ConnectorContext connector)
        {
            var e = new SocketAsyncEventArgs();
            e.Completed += SocketAsyncEventArgs_Completed;
            e.RemoteEndPoint = connector.RemoteEp;
            e.UserToken = connector;
            var willRaiseEvent = connector.Socket.ConnectAsync(e);
            if (!willRaiseEvent)
            {
                ProcessConnect(e);
            }
        }

        void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ((AsyncSocketSession) e.UserToken).ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                case SocketAsyncOperation.SendPackets:
                    ((AsyncSocketSession) e.UserToken).ProcessSend(e);
                    break;
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            var connector = (ConnectorContext) e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                var readBuffer = e;
                readBuffer.AcceptSocket = null;
                readBuffer.RemoteEndPoint = null;
                readBuffer.SetBuffer(new byte[SessionConfig.ReadBufferSize], 0, SessionConfig.ReadBufferSize);

                var writeBuffer = new SocketAsyncEventArgs();
                writeBuffer.SetBuffer(new byte[SessionConfig.ReadBufferSize], 0, SessionConfig.ReadBufferSize);
                writeBuffer.Completed += SocketAsyncEventArgs_Completed;

                EndConnect(new AsyncSocketSession(this, Processor, connector.Socket,
                    new SocketAsyncEventArgsBuffer(readBuffer), new SocketAsyncEventArgsBuffer(writeBuffer),
                    ReuseBuffer), connector);
            }
            else
            {
                EndConnect(new SocketException((int) e.SocketError), connector);
            }
        }
    }
}
