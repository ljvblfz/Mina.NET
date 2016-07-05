using System;
using System.Net.Sockets;

namespace Mina.Transport.Socket
{
    class SessionConfigImpl : AbstractSocketSessionConfig
    {
        private readonly System.Net.Sockets.Socket _socket;

        public SessionConfigImpl(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }

        public override int? ReceiveBufferSize
        {
            get { return _socket.ReceiveBufferSize; }
            set { if (value.HasValue) _socket.ReceiveBufferSize = value.Value; }
        }

        public override int? SendBufferSize
        {
            get { return _socket.SendBufferSize; }
            set { if (value.HasValue) _socket.SendBufferSize = value.Value; }
        }

        public override bool? ExclusiveAddressUse
        {
            get { return _socket.ExclusiveAddressUse; }
            set { if (value.HasValue) _socket.ExclusiveAddressUse = value.Value; }
        }

        public override bool? ReuseAddress
        {
            get
            {
                return Convert.ToBoolean(_socket.GetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress));
            }
            set
            {
                if (value.HasValue)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value.Value);
            }
        }

        public override int? TrafficClass
        {
            get
            {
                return Convert.ToInt32(_socket.GetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.TypeOfService));
            }
            set
            {
                if (value.HasValue)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.TypeOfService, value.Value);
            }
        }

        public override bool? KeepAlive
        {
            get
            {
                return Convert.ToBoolean(_socket.GetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.KeepAlive));
            }
            set
            {
                if (value.HasValue)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, value.Value);
            }
        }

        public override bool? OobInline
        {
            get
            {
                return Convert.ToBoolean(_socket.GetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.OutOfBandInline));
            }
            set
            {
                if (value.HasValue)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.OutOfBandInline, value.Value);
            }
        }

        public override bool? NoDelay
        {
            get { return _socket.NoDelay; }
            set { if (value.HasValue) _socket.NoDelay = value.Value; }
        }

        public override int? SoLinger
        {
            get { return _socket.LingerState.LingerTime; }
            set
            {
                if (value.HasValue)
                {
                    if (value < 0)
                        _socket.LingerState = new LingerOption(false, 0);
                    else
                        _socket.LingerState = new LingerOption(true, value.Value);
                }
            }
        }
    }
}
