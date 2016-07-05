using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IOSessionConfig"/> for datagram transport type.
    /// </summary>
    public interface IDatagramSessionConfig : IOSessionConfig
    {
        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.EnableBroadcast"/>
        /// </summary>
        bool? EnableBroadcast { get; set; }
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
        /// Gets or sets <see cref="System.Net.Sockets.MulticastOption"/>.
        /// </summary>
        System.Net.Sockets.MulticastOption MulticastOption { get; set; }
    }
}
