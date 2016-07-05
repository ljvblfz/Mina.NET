using System;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Connects to endpoint, communicates with the server, and fires events to <see cref="IOHandler"/>s.
    /// </summary>
    public interface IOConnector : IOService
    {
        /// <summary>
        /// Gets or sets connect timeout in seconds. The default value is 1 minute.
        /// <seealso cref="ConnectTimeoutInMillis"/>
        /// </summary>
        int ConnectTimeout { get; set; }
        /// <summary>
        /// Gets or sets connect timeout in milliseconds. The default value is 1 minute.
        /// </summary>
        long ConnectTimeoutInMillis { get; set; }
        /// <summary>
        /// Gets or sets the default remote endpoint to connect to when no argument
        /// is specified in <see cref="Connect()"/> method.
        /// </summary>
        EndPoint DefaultRemoteEndPoint { get; set; }
        /// <summary>
        /// Gets or sets the default local endpoint.
        /// </summary>
        EndPoint DefaultLocalEndPoint { get; set; }
        /// <summary>
        /// Connects to the <see cref="DefaultRemoteEndPoint"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect();
        /// <summary>
        /// Connects to the <see cref="DefaultRemoteEndPoint"/> and invokes the <code>ioSessionInitializer</code>
        /// when the IoSession is created but before <code>SessionCreated</code> is fired.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(Action<IOSession, IConnectFuture> sessionInitializer);
        /// <summary>
        /// Connects to the specified remote endpoint.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEp);
        /// <summary>
        /// Connects to the specified remote endpoint and invokes the <code>ioSessionInitializer</code>
        /// when the IoSession is created but before <code>SessionCreated</code> is fired.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEp, Action<IOSession, IConnectFuture> sessionInitializer);
        /// <summary>
        /// Connects to the specified remote endpoint binding to the specified local endpoint.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEp, EndPoint localEp);
        /// <summary>
        /// Connects to the specified remote endpoint binding to the specified local endpoint,
        /// and invokes the <code>ioSessionInitializer</code>
        /// when the IoSession is created but before <code>SessionCreated</code> is fired.
        /// </summary>
        /// <exception cref="InvalidOperationException">if no default remoted address is set</exception>
        IConnectFuture Connect(EndPoint remoteEp, EndPoint localEp, Action<IOSession, IConnectFuture> sessionInitializer);
    }
}
