using System;
using System.Net.Sockets;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOConnector"/> for datagram transport (UDP/IP).
    /// </summary>
    public class AsyncDatagramConnector : AbstractSocketConnector, IDatagramConnector
    {
        /// <summary>
        /// Instantiates.
        /// </summary>
        public AsyncDatagramConnector()
            : base(new DefaultDatagramSessionConfig())
        {
        }

        /// <inheritdoc/>
        public new IDatagramSessionConfig SessionConfig => (IDatagramSessionConfig) base.SessionConfig;

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata => AsyncDatagramSession.Metadata;

        /// <inheritdoc/>
        protected override System.Net.Sockets.Socket NewSocket(AddressFamily addressFamily)
        {
            return new System.Net.Sockets.Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
        }

        /// <inheritdoc/>
        protected override void BeginConnect(ConnectorContext connector)
        {
            /*
             * No idea why get a SocketError.InvalidArgument in ConnectAsync.
             * Call BeginConnect instead.
             */
            connector.Socket.BeginConnect(connector.RemoteEp, ConnectCallback, connector);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            var connector = (ConnectorContext) ar.AsyncState;
            try
            {
                connector.Socket.EndConnect(ar);
            }
            catch (Exception ex)
            {
                EndConnect(ex, connector);
                return;
            }

            var readBuffer = new SocketAsyncEventArgs();
            readBuffer.SetBuffer(new byte[SessionConfig.ReadBufferSize], 0, SessionConfig.ReadBufferSize);
            readBuffer.Completed += SocketAsyncEventArgs_Completed;

            var writeBuffer = new SocketAsyncEventArgs();
            writeBuffer.SetBuffer(new byte[SessionConfig.ReadBufferSize], 0, SessionConfig.ReadBufferSize);
            writeBuffer.Completed += SocketAsyncEventArgs_Completed;

            EndConnect(new AsyncDatagramSession(this, Processor, connector.Socket, connector.RemoteEp,
                new SocketAsyncEventArgsBuffer(readBuffer), new SocketAsyncEventArgsBuffer(writeBuffer),
                ReuseBuffer), connector);
        }

        void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.ReceiveFrom:
                    ((AsyncDatagramSession) e.UserToken).ProcessReceive(e);
                    break;
                case SocketAsyncOperation.SendTo:
                    ((AsyncDatagramSession) e.UserToken).ProcessSend(e);
                    break;
            }
        }
    }
}
