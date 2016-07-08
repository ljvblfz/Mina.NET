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
    public class DatagramBindTest : AbstractBindTest
    {
        public DatagramBindTest()
            : base(new AsyncDatagramAcceptor())
        { }

        protected override EndPoint CreateEndPoint(int port)
        {
            return new IPEndPoint(IPAddress.Loopback, port);
        }

        protected override int GetPort(EndPoint ep)
        {
            return ((IPEndPoint)ep).Port;
        }

        protected override IOConnector NewConnector()
        {
            return new AsyncDatagramConnector();
        }
    }
}
