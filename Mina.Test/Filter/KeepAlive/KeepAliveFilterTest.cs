using System;
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
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Filter.KeepAlive
{
    [TestClass]
    public class KeepAliveFilterTest
    {
        static readonly IOBuffer Ping = IOBuffer.Wrap(new byte[] { 1 });
        static readonly IOBuffer Pong = IOBuffer.Wrap(new byte[] { 2 });
        private static readonly int Interval = 5;
        private static readonly int Timeout = 1;

        private int _port;
        private AsyncSocketAcceptor _acceptor;

        [TestInitialize]
        public void SetUp()
        {
            _acceptor = new AsyncSocketAcceptor();
            IKeepAliveMessageFactory factory = new ServerFactory();
            var filter = new KeepAliveFilter(factory, IdleStatus.BothIdle);
            _acceptor.FilterChain.AddLast("keep-alive", filter);
            _acceptor.DefaultLocalEndPoint = new IPEndPoint(IPAddress.Loopback, 0);
            _acceptor.Bind();
            _port = _acceptor.LocalEndPoint.Port;
        }

        [TestCleanup]
        public void TearDown()
        {
            _acceptor.Unbind();
            _acceptor.Dispose();
        }

        [TestMethod]
        public void TestKeepAliveFilterForReaderIdle()
        {
            KeepAliveFilterForIdleStatus(IdleStatus.ReaderIdle);
        }

        [TestMethod]
        public void TestKeepAliveFilterForWriterIdle()
        {
            KeepAliveFilterForIdleStatus(IdleStatus.WriterIdle);
        }

        [TestMethod]
        public void TestKeepAliveFilterForBothIdle()
        {
            KeepAliveFilterForIdleStatus(IdleStatus.BothIdle);
        }

        private void KeepAliveFilterForIdleStatus(IdleStatus status)
        {
            using (var connector = new AsyncSocketConnector())
            {
                var filter = new KeepAliveFilter(new ClientFactory(), status, KeepAliveRequestTimeoutHandler.Exception, Interval, Timeout);
                filter.ForwardEvent = true;
                connector.FilterChain.AddLast("keep-alive", filter);

                var gotException = false;
                connector.ExceptionCaught += (s, e) =>
                {
                    // A KeepAliveRequestTimeoutException will be thrown if no keep-alive response is received.
                    Console.WriteLine(e.Exception);
                    gotException = true;
                };

                var future = connector.Connect(new IPEndPoint(IPAddress.Loopback, _port)).Await();
                var session = future.Session;
                Assert.IsNotNull(session);

                Thread.Sleep((Interval + Timeout + 3) * 1000);

                Assert.IsFalse(gotException, "got an exception on the client");

                session.Close(true);
            }
        }

        static bool CheckRequest(IOBuffer message)
        {
            var buff = message;
            var check = buff.Get() == 1;
            buff.Rewind();
            return check;
        }

        static bool CheckResponse(IOBuffer message)
        {
            var buff = message;
            var check = buff.Get() == 2;
            buff.Rewind();
            return check;
        }

        class ServerFactory : IKeepAliveMessageFactory
        {
            public object GetRequest(IOSession session)
            {
                return null;
            }

            public object GetResponse(IOSession session, object request)
            {
                return Pong.Duplicate();
            }

            public bool IsRequest(IOSession session, object message)
            {
                if (message is IOBuffer)
                {
                    return CheckRequest((IOBuffer)message);
                }
                return false;
            }

            public bool IsResponse(IOSession session, object message)
            {
                if (message is IOBuffer)
                {
                    return CheckResponse((IOBuffer)message);
                }
                return false;
            }
        }

        class ClientFactory : IKeepAliveMessageFactory
        {
            public object GetRequest(IOSession session)
            {
                return Ping.Duplicate();
            }

            public object GetResponse(IOSession session, object request)
            {
                return null;
            }

            public bool IsRequest(IOSession session, object message)
            {
                if (message is IOBuffer)
                {
                    return CheckRequest((IOBuffer)message);
                }
                return false;
            }

            public bool IsResponse(IOSession session, object message)
            {
                if (message is IOBuffer)
                {
                    return CheckResponse((IOBuffer)message);
                }
                return false;
            }
        }
    }
}
