using System.Net;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Session;

namespace Mina.Filter.Firewall
{
    [TestClass]
    public class ConnectionThrottleFilterTest
    {
        private ConnectionThrottleFilter _filter = new ConnectionThrottleFilter();
        private DummySession _sessionOne = new DummySession();
        private DummySession _sessionTwo = new DummySession();

        public ConnectionThrottleFilterTest()
        {
            _sessionOne.SetRemoteEndPoint(new IPEndPoint(IPAddress.Any, 1234));
            _sessionTwo.SetRemoteEndPoint(new IPEndPoint(IPAddress.Any, 1235));
        }

        [TestMethod]
        public void TestGoodConnection()
        {
            _filter.AllowedInterval = 100;
            _filter.IsConnectionOk(_sessionOne);

            Thread.Sleep(1000);

            Assert.IsTrue(_filter.IsConnectionOk(_sessionOne));
        }

        [TestMethod]
        public void TestBadConnection()
        {
            _filter.AllowedInterval = 1000;
            _filter.IsConnectionOk(_sessionTwo);
            Assert.IsFalse(_filter.IsConnectionOk(_sessionTwo));
        }
    }
}
