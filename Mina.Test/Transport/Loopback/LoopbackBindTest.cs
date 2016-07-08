#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Service;

namespace Mina.Transport.Loopback
{
    [TestClass]
    public class LoopbackBindTest : AbstractBindTest
    {
        public LoopbackBindTest()
            : base(new LoopbackAcceptor())
        { }

        protected override System.Net.EndPoint CreateEndPoint(int port)
        {
            return new LoopbackEndPoint(port);
        }

        protected override int GetPort(System.Net.EndPoint ep)
        {
            return ((LoopbackEndPoint)ep).Port;
        }

        protected override IOConnector NewConnector()
        {
            return new LoopbackConnector();
        }
    }
}
