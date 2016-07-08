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
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractTrafficControlTest
    {
        protected int Port;
        protected readonly IOAcceptor Acceptor;

        public AbstractTrafficControlTest(IOAcceptor acceptor)
        {
            this.Acceptor = acceptor;
        }

        [TestInitialize]
        public void SetUp()
        {
            Acceptor.MessageReceived += (s, e) =>
            {
                // Just echo the received bytes.
                var rb = (IOBuffer)e.Message;
                var wb = IOBuffer.Allocate(rb.Remaining);
                wb.Put(rb);
                wb.Flip();
                e.Session.Write(wb);
            };
            Acceptor.Bind(CreateServerEndPoint(0));
            Port = GetPort(Acceptor.LocalEndPoint);
        }

        [TestCleanup]
        public void TearDown()
        {
            Acceptor.Unbind();
            Acceptor.Dispose();
        }

        [TestMethod]
        public void TestSuspendResumeReadWrite()
        {
            var future = Connect(Port, new ClientIoHandler());
            future.Await();
            var session = future.Session;

            // We wait for the SessionCreated() event is fired because we
            // cannot guarantee that it is invoked already.
            while (session.GetAttribute("lock") == null)
            {
                Thread.Yield();
            }

            var sync = session.GetAttribute("lock");
            lock (sync)
            {
                Write(session, "1");
                Assert.AreEqual('1', Read(session));
                Assert.AreEqual("1", GetReceived(session));
                Assert.AreEqual("1", GetSent(session));

                session.SuspendRead();

                Thread.Sleep(100);

                Write(session, "2");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("1", GetReceived(session));
                Assert.AreEqual("12", GetSent(session));

                session.SuspendWrite();

                Thread.Sleep(100);

                Write(session, "3");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("1", GetReceived(session));
                Assert.AreEqual("12", GetSent(session));

                session.ResumeRead();

                Thread.Sleep(100);

                Write(session, "4");
                Assert.AreEqual('2', Read(session));
                Assert.AreEqual("12", GetReceived(session));
                Assert.AreEqual("12", GetSent(session));

                session.ResumeWrite();

                Thread.Sleep(100);

                Assert.AreEqual('3', Read(session));
                Assert.AreEqual('4', Read(session));

                Write(session, "5");
                Assert.AreEqual('5', Read(session));
                Assert.AreEqual("12345", GetReceived(session));
                Assert.AreEqual("12345", GetSent(session));

                session.SuspendWrite();

                Thread.Sleep(100);

                Write(session, "6");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("12345", GetReceived(session));
                Assert.AreEqual("12345", GetSent(session));

                session.SuspendRead();
                session.ResumeWrite();

                Thread.Sleep(100);

                Write(session, "7");
                Assert.IsFalse(CanRead(session));
                Assert.AreEqual("12345", GetReceived(session));
                Assert.AreEqual("1234567", GetSent(session));

                session.ResumeRead();

                Thread.Sleep(100);

                Assert.AreEqual('6', Read(session));
                Assert.AreEqual('7', Read(session));

                Assert.AreEqual("1234567", GetReceived(session));
                Assert.AreEqual("1234567", GetSent(session));
            }

            session.Close(true).Await();
        }

        protected abstract EndPoint CreateServerEndPoint(int port);
        protected abstract int GetPort(EndPoint ep);
        protected abstract IConnectFuture Connect(int port, IOHandler handler);

        private void Write(IOSession session, string s)
        {
            session.Write(IOBuffer.Wrap(Encoding.ASCII.GetBytes(s)));
        }

        private char Read(IOSession session)
        {
            var pos = session.GetAttribute<int>("pos");
            for (var i = 0; i < 10 && pos == GetReceived(session).Length; i++)
            {
                var sync = session.GetAttribute("lock");
                lock (sync)
                {
                    Monitor.Wait(sync, 200);
                }
            }
            session.SetAttribute("pos", pos + 1);
            var received = GetReceived(session);
            Assert.IsTrue(received.Length > pos);
            return GetReceived(session)[pos];
        }

        private string GetReceived(IOSession session)
        {
            return session.GetAttribute("received").ToString();
        }

        private string GetSent(IOSession session)
        {
            return session.GetAttribute("sent").ToString();
        }

        private bool CanRead(IOSession session)
        {
            var pos = session.GetAttribute<int>("pos");
            var sync = session.GetAttribute("lock");
            lock (sync)
            {
                Monitor.Wait(sync, 250);
            }
            var received = GetReceived(session);
            return pos < received.Length;
        }

        class ClientIoHandler : IOHandlerAdapter
        {
            public override void SessionCreated(IOSession session)
            {
                session.SetAttribute("pos", 0);
                session.SetAttribute("received", new StringBuilder());
                session.SetAttribute("sent", new StringBuilder());
                session.SetAttribute("lock", new object());
            }

            public override void MessageReceived(IOSession session, object message)
            {
                var buffer = (IOBuffer)message;
                var data = new byte[buffer.Remaining];
                buffer.Get(data, 0, data.Length);
                var sync = session.GetAttribute("lock");
                lock (sync)
                {
                    var sb = session.GetAttribute<StringBuilder>("received");
                    sb.Append(Encoding.ASCII.GetString(data));
                    Monitor.PulseAll(sync);
                }
            }

            public override void MessageSent(IOSession session, object message)
            {
                var buffer = (IOBuffer)message;
                buffer.Rewind();
                var data = new byte[buffer.Remaining];
                buffer.Get(data, 0, data.Length);
                var sb = session.GetAttribute<StringBuilder>("sent");
                sb.Append(Encoding.ASCII.GetString(data));
            }
        }
    }
}
