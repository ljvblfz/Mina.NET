using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IOSessionConfig"/> for socket transport type.
    /// </summary>
    public interface ISocketSessionConfig : IOSessionConfig
    {
        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.ExclusiveAddressUse"/>
        /// </summary>
        bool? ExclusiveAddressUse { get; set; }

        /// <summary>
        /// Gets or sets if <see cref="System.Net.Sockets.SocketOptionName.ReuseAddress"/> is enabled.
        /// </summary>
        bool? ReuseAddress { get; set; }

        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.ReceiveBufferSize"/>
        /// </summary>
        int? ReceiveBufferSize { get; set; }

        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.SendBufferSize"/>
        /// </summary>
        int? SendBufferSize { get; set; }

        /// <summary>
        /// Gets or sets traffic class or <see cref="System.Net.Sockets.SocketOptionName.TypeOfService"/> in the IP datagram header.
        /// </summary>
        int? TrafficClass { get; set; }

        /// <summary>
        /// Enables or disables <see cref="System.Net.Sockets.SocketOptionName.KeepAlive"/>.
        /// </summary>
        bool? KeepAlive { get; set; }

        /// <summary>
        /// Enables or disables <see cref="System.Net.Sockets.SocketOptionName.OutOfBandInline"/>.
        /// </summary>
        bool? OobInline { get; set; }

        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.NoDelay"/>
        /// </summary>
        bool? NoDelay { get; set; }

        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.LingerState"/>
        /// </summary>
        int? SoLinger { get; set; }
    }
}
