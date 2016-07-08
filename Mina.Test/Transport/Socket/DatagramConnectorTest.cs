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
    public class DatagramConnectorTest : AbstractConnectorTest
    {
        protected override IOAcceptor CreateAcceptor()
        {
            return new AsyncDatagramAcceptor();
        }

        protected override IOConnector CreateConnector()
        {
            return new AsyncDatagramConnector();
        }

        protected override EndPoint CreateEndPoint(int port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        public override void TestConnectFutureFailureTiming()
        {
            // Skip the test; Datagram connection can be made even if there's no
            // server at the endpoint.
        }
    }
}
