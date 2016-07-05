using System;
using System.Text;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Filter.Executor;

namespace Mina.Transport.Loopback
{
    [TestClass]
    public class LoopbackEventOrderTest
    {
        [TestMethod]
        public void TestServerToClient()
        {
            IOAcceptor acceptor = new LoopbackAcceptor();
            IOConnector connector = new LoopbackConnector();

            acceptor.SessionOpened += (s, e) => e.Session.Write("B");
            acceptor.MessageSent += (s, e) => e.Session.Close(true);

            acceptor.Bind(new LoopbackEndPoint(1));

            var actual = new StringBuilder();

            connector.MessageReceived += (s, e) => actual.Append(e.Message);
            connector.SessionClosed += (s, e) => actual.Append("C");
            connector.SessionOpened += (s, e) => actual.Append("A");

            var future = connector.Connect(new LoopbackEndPoint(1));

            future.Await();
            future.Session.CloseFuture.Await();
            acceptor.Unbind();
            acceptor.Dispose();
            connector.Dispose();

            // sessionClosed() might not be invoked yet
            // even if the connection is closed.
            while (actual.ToString().IndexOf("C") < 0)
            {
                Thread.Yield();
            }

            Assert.AreEqual("ABC", actual.ToString());
        }

        [TestMethod]
        public void TestClientToServer()
        {
            IOAcceptor acceptor = new LoopbackAcceptor();
            IOConnector connector = new LoopbackConnector();

            var actual = new StringBuilder();

            acceptor.MessageReceived += (s, e) => actual.Append(e.Message);
            acceptor.SessionClosed += (s, e) => actual.Append("C");
            acceptor.SessionOpened += (s, e) => actual.Append("A");

            acceptor.Bind(new LoopbackEndPoint(1));

            connector.SessionOpened += (s, e) => e.Session.Write("B");
            connector.MessageSent += (s, e) => e.Session.Close(true);

            var future = connector.Connect(new LoopbackEndPoint(1));

            future.Await();
            future.Session.CloseFuture.Await();
            acceptor.Unbind();
            acceptor.Dispose();
            connector.Dispose();

            // sessionClosed() might not be invoked yet
            // even if the connection is closed.
            while (actual.ToString().IndexOf("C") < 0)
            {
                Thread.Yield();
            }

            Assert.AreEqual("ABC", actual.ToString());
        }

        [TestMethod]
        public void TestSessionCreated()
        {
            var semaphore = new Semaphore(0, 10);
            var sb = new StringBuilder();
            var acceptor = new LoopbackAcceptor();
            var lep = new LoopbackEndPoint(12345);

            acceptor.SessionCreated += (s, e) =>
            {
                // pretend we are doing some time-consuming work. For
                // performance reasons, you would never want to do time
                // consuming work in sessionCreated.
                // However, this increases the likelihood of the timing bug.
                Thread.Sleep(1000);
                sb.Append("A");
            };
            acceptor.SessionOpened += (s, e) => sb.Append("B");
            acceptor.MessageReceived += (s, e) => sb.Append("C");
            acceptor.SessionClosed += (s, e) =>
            {
                sb.Append("D");
                semaphore.Release();
            };

            acceptor.Bind(lep);

            var connector = new LoopbackConnector();
            connector.FilterChain.AddLast("executor", new ExecutorFilter());
            var future = connector.Connect(lep);
            future.Await();
            future.Session.Write(IOBuffer.Wrap(new byte[1])).Await();
            future.Session.Close(false).Await();

            semaphore.WaitOne(TimeSpan.FromSeconds(1));
            acceptor.Unbind(lep);
            Assert.AreEqual(1, future.Session.WrittenBytes);
            Assert.AreEqual("ABCD", sb.ToString());
        }
    }
}
