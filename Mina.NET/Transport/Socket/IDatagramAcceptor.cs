using System.Net;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOAcceptor"/> for socket transport (TCP/IP).  This class handles incoming TCP/IP based socket connections.
    /// </summary>
    public interface IDatagramAcceptor : IOAcceptor
    {
        /// <inheritdoc/>
        new IDatagramSessionConfig SessionConfig { get; }
        /// <inheritdoc/>
        new IPEndPoint LocalEndPoint { get; }
        /// <inheritdoc/>
        new IPEndPoint DefaultLocalEndPoint { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="IOSessionRecycler"/> for this service.
        /// </summary>
        IOSessionRecycler SessionRecycler { get; set; }
    }
}
