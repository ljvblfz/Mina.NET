using System.Net;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOConnector"/> for socket transport (UDP/IP)
    /// </summary>
    public interface IDatagramConnector : IOConnector
    {
        /// <inheritdoc/>
        new IDatagramSessionConfig SessionConfig { get; }
        /// <inheritdoc/>
        new IPEndPoint DefaultRemoteEndPoint { get; set; }
    }
}
