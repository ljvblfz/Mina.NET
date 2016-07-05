#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Future;
using Mina.Core.Service;

namespace Mina.Transport.Loopback
{
    [TestClass]
    public class LoopbackTrafficControlTest : AbstractTrafficControlTest
    {
        public LoopbackTrafficControlTest()
            : base(new LoopbackAcceptor())
        { }

        protected override System.Net.EndPoint CreateServerEndPoint(int port)
        {
            return new LoopbackEndPoint(port);
        }

        protected override int GetPort(System.Net.EndPoint ep)
        {
            return ((LoopbackEndPoint)ep).Port;
        }

        protected override IConnectFuture Connect(int port, IOHandler handler)
        {
            IOConnector connector = new LoopbackConnector();
            connector.Handler = handler;
            return connector.Connect(new LoopbackEndPoint(port));
        }
    }
}
