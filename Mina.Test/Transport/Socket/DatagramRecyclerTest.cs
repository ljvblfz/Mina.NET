using System;
using System.Net;
using System.Text;
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
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class DatagramRecyclerTest
    {
        private AsyncDatagramAcceptor _acceptor;
        private AsyncDatagramConnector _connector;

        [TestInitialize]
        public void SetUp()
        {
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
        public void TestDatagramRecycler()
        {
            var port = 1024;
            var recycler = new ExpiringSessionRecycler(1, 1);

            var acceptorHandler = new MockHandler();
            var connectorHandler = new MockHandler();

            _acceptor.Handler = acceptorHandler;
            _acceptor.SessionRecycler = recycler;
            _acceptor.Bind(new IPEndPoint(IPAddress.Loopback, port));

            try
            {
                _connector.Handler = connectorHandler;
                var future = _connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
                future.Await();

                // Write whatever to trigger the acceptor.
                future.Session.Write(IOBuffer.Allocate(1)).Await();

                // Close the client-side connection.
                // This doesn't mean that the acceptor-side connection is also closed.
                // The life cycle of the acceptor-side connection is managed by the recycler.
                future.Session.Close(true);
                future.Session.CloseFuture.Await();
                Assert.IsTrue(future.Session.CloseFuture.Closed);

                // Wait until the acceptor-side connection is closed.
                while (acceptorHandler.Session == null)
                {
                    Thread.Yield();
                }
                acceptorHandler.Session.CloseFuture.Await(3000);

                // Is it closed?
                Assert.IsTrue(acceptorHandler.Session.CloseFuture.Closed);

                Thread.Sleep(1000);

                Assert.AreEqual("CROPSECL", connectorHandler.Result.ToString());
                Assert.AreEqual("CROPRECL", acceptorHandler.Result.ToString());
            }
            finally
            {
                _acceptor.Unbind();
            }
        }

        [TestMethod]
        public void TestCloseRequest()
        {
            var port = 1024;
            var recycler = new ExpiringSessionRecycler(10, 1);

            var acceptorHandler = new MockHandler();
            var connectorHandler = new MockHandler();

            _acceptor.SessionConfig.SetIdleTime(IdleStatus.ReaderIdle, 1);
            _acceptor.Handler = acceptorHandler;
            _acceptor.SessionRecycler = recycler;
            _acceptor.Bind(new IPEndPoint(IPAddress.Loopback, port));

            try
            {
                _connector.Handler = connectorHandler;
                var future = _connector.Connect(new IPEndPoint(IPAddress.Loopback, port));
                future.Await();

                // Write whatever to trigger the acceptor.
                future.Session.Write(IOBuffer.Allocate(1)).Await();

                // Make sure the connection is closed before recycler closes it.
                while (acceptorHandler.Session == null)
                {
                    Thread.Yield();
                }
                acceptorHandler.Session.Close(true);
                Assert.IsTrue(acceptorHandler.Session.CloseFuture.Await(3000));

                var oldSession = acceptorHandler.Session;

                // Wait until all events are processed and clear the state.
                var startTime = DateTime.Now;
                while (acceptorHandler.Result.ToString().Length < 8)
                {
                    Thread.Yield();
                    if ((DateTime.Now - startTime).TotalMilliseconds > 5000)
                    {
                        throw new Exception();
                    }
                }
                acceptorHandler.Result.Clear();
                acceptorHandler.Session = null;

                // Write whatever to trigger the acceptor again.
                var wf = future.Session.Write(IOBuffer.Allocate(1)).Await();
                Assert.IsTrue(wf.Written);

                // Make sure the connection is closed before recycler closes it.
                while (acceptorHandler.Session == null)
                {
                    Thread.Yield();
                }
                acceptorHandler.Session.Close(true);
                Assert.IsTrue(acceptorHandler.Session.CloseFuture.Await(3000));

                future.Session.Close(true).Await();

                Assert.AreNotSame(oldSession, acceptorHandler.Session);
            }
            finally
            {
                _acceptor.Unbind();
            }
        }

        class MockHandler : IOHandlerAdapter
        {
            public volatile IOSession Session;

            public readonly StringBuilder Result = new StringBuilder();

            public override void ExceptionCaught(IOSession session, Exception cause)
            {
                this.Session = session;
                Result.Append("CA");
            }

            public override void MessageReceived(IOSession session, object message)
            {
                this.Session = session;
                Result.Append("RE");
            }

            public override void MessageSent(IOSession session, object message)
            {
                this.Session = session;
                Result.Append("SE");
            }

            public override void SessionClosed(IOSession session)
            {
                this.Session = session;
                Result.Append("CL");
            }

            public override void SessionCreated(IOSession session)
            {
                this.Session = session;
                Result.Append("CR");
            }

            public override void SessionIdle(IOSession session, IdleStatus status)
            {
                this.Session = session;
                Result.Append("ID");
            }

            public override void SessionOpened(IOSession session)
            {
                this.Session = session;
                Result.Append("OP");
            }
        }
    }
}
