using System.Net;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class SocketConnectorTest : AbstractConnectorTest
    {
        protected override IOAcceptor CreateAcceptor()
        {
            return new AsyncSocketAcceptor();
        }

        protected override IOConnector CreateConnector()
        {
            return new AsyncSocketConnector();
        }

        protected override EndPoint CreateEndPoint(int port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }
    }
}
