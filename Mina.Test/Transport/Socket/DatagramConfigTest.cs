using System.Net;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Service;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class DatagramConfigTest
    {
        private IOAcceptor _acceptor;
        private IOConnector _connector;
        static string _result;

        [TestInitialize]
        public void SetUp()
        {
            _result = "";
            _acceptor = new AsyncDatagramAcceptor();
            _connector = new AsyncDatagramConnector();
        }

        [TestCleanup]
        public void TearDown()
        {
            _acceptor.Dispose();
            _connector.Dispose();
        }

        [TestMethod]
        public void TestAcceptorFilterChain()
        {
            var port = 1024;
            IOFilter mockFilter = new MockFilter();
            IOHandler mockHandler = new MockHandler();

            _acceptor.FilterChain.AddLast("mock", mockFilter);
            _acceptor.Handler = mockHandler;
            _acceptor.Bind(new IPEndPoint(IPAddress.Loopback, port));

            try
            {
                var future = _connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
                future.Await();

                var writeFuture = future.Session.Write(IOBuffer.Allocate(16).PutInt32(0).Flip());
                writeFuture.Await();
                Assert.IsTrue(writeFuture.Written);

                future.Session.Close(true);

                for (var i = 0; i < 30; i++)
                {
                    if (_result.Length == 2)
                    {
                        break;
                    }
                    Thread.Sleep(100);
                }

                Assert.AreEqual("FH", _result);
            }
            finally
            {
                _acceptor.Unbind();
            }
        }

        class MockFilter : IoFilterAdapter
        {
            public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
            {
                _result += "F";
                nextFilter.MessageReceived(session, message);
            }
        }

        class MockHandler : IOHandlerAdapter
        {
            public override void MessageReceived(IOSession session, object message)
            {
                _result += "H";
            }
        }
    }
}
