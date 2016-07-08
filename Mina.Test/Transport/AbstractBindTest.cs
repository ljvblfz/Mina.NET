using System;
using System.IO;
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
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractBindTest
    {
        protected int Port;
        protected readonly IOAcceptor Acceptor;

        public AbstractBindTest(IOAcceptor acceptor)
        {
            this.Acceptor = acceptor;
        }

        [TestCleanup]
        public void TearDown()
        {
            Acceptor.Unbind();
            Acceptor.Dispose();
            Acceptor.DefaultLocalEndPoint = null;
        }

        [TestMethod]
        public void TestAnonymousBind()
        {
            Acceptor.DefaultLocalEndPoint = null;
            Acceptor.Bind();
            Assert.IsNotNull(Acceptor.LocalEndPoint);;
            Acceptor.Unbind(Acceptor.LocalEndPoint);
            Assert.IsNull(Acceptor.LocalEndPoint);
            Acceptor.DefaultLocalEndPoint = CreateEndPoint(0);
            Acceptor.Bind();
            Assert.IsNotNull(Acceptor.LocalEndPoint);
            Assert.IsTrue(GetPort(Acceptor.LocalEndPoint) != 0);
            Acceptor.Unbind(Acceptor.LocalEndPoint);
        }

        [TestMethod]
        public void TestDuplicateBind()
        {
            Bind(false);

            try
            {
                Acceptor.Bind();
                Assert.Fail("Exception is not thrown");
            }
            catch (Exception)
            {
                // Signifies a successfull test case execution
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void TestDuplicateUnbind()
        {
            Bind(false);

            // this should succeed
            Acceptor.Unbind();

            // this shouldn't fail
            Acceptor.Unbind();
        }

        [TestMethod]
        public void TestManyTimes()
        {
            Bind(true);

            for (var i = 0; i < 1024; i++)
            {
                Acceptor.Unbind();
                Acceptor.Bind();
            }
        }

        [TestMethod]
        public void TestUnbindDisconnectsClients()
        {
            Bind(true);
            var connector = NewConnector();
            var sessions = new IOSession[5];
            for (var i = 0; i < sessions.Length; i++)
            {
                var future = connector.Connect(CreateEndPoint(Port));
                future.Await();
                sessions[i] = future.Session;
                Assert.IsTrue(sessions[i].Connected);
                Assert.IsTrue(sessions[i].Write(IOBuffer.Allocate(1)).Await().Written);
            }

            // Wait for the server side sessions to be created.
            Thread.Sleep(500);

            var managedSessions = Acceptor.ManagedSessions.Values;
            Assert.AreEqual(5, managedSessions.Count);

            Acceptor.Unbind();

            // Wait for the client side sessions to close.
            Thread.Sleep(500);

            //Assert.AreEqual(0, managedSessions.Count);
            foreach (var element in managedSessions)
            {
                Assert.IsFalse(element.Connected);
            }
        }

        [TestMethod]
        public void TestUnbindResume()
        {
            Bind(true);
            var connector = NewConnector();
            IOSession session = null;

            var future = connector.Connect(CreateEndPoint(Port));
            future.Await();
            session = future.Session;
            Assert.IsTrue(session.Connected);
            Assert.IsTrue(session.Write(IOBuffer.Allocate(1)).Await().Written);

            // Wait for the server side session to be created.
            Thread.Sleep(500);

            var managedSession = Acceptor.ManagedSessions.Values;
            Assert.AreEqual(1, managedSession.Count);

            Acceptor.Unbind();

            // Wait for the client side sessions to close.
            Thread.Sleep(500);

            //Assert.AreEqual(0, managedSession.Count);
            foreach (var element in managedSession)
            {
                Assert.IsFalse(element.Connected);
            }

            // Rebind
            Bind(true);

            // Check again the connection
            future = connector.Connect(CreateEndPoint(Port));
            future.Await();
            session = future.Session;
            Assert.IsTrue(session.Connected);
            Assert.IsTrue(session.Write(IOBuffer.Allocate(1)).Await().Written);

            // Wait for the server side session to be created.
            Thread.Sleep(500);

            managedSession = Acceptor.ManagedSessions.Values;
            Assert.AreEqual(1, managedSession.Count);
        }

        protected abstract EndPoint CreateEndPoint(int port);
        protected abstract int GetPort(EndPoint ep);
        protected abstract IOConnector NewConnector();

        protected void Bind(bool reuseAddress)
        {
            Acceptor.Handler = new EchoProtocolHandler();

            SetReuseAddress(reuseAddress);

            var socketBound = false;
            for (Port = 1024; Port <= 65535; Port++)
            {
                socketBound = false;
                try
                {
                    Acceptor.DefaultLocalEndPoint = CreateEndPoint(Port);
                    Acceptor.Bind();
                    socketBound = true;
                    break;
                }
                catch (IOException)
                { }
            }

            if (!socketBound)
                throw new IOException("Cannot bind any test port.");
        }

        private void SetReuseAddress(bool reuseAddress)
        {
            if (Acceptor is ISocketAcceptor)
            {
                ((ISocketAcceptor)Acceptor).ReuseAddress = reuseAddress;
            }
        }

        class EchoProtocolHandler : IOHandlerAdapter
        {
            public override void SessionCreated(IOSession session)
            {
                session.Config.SetIdleTime(IdleStatus.BothIdle, 10);
            }

            public override void SessionIdle(IOSession session, IdleStatus status)
            {
                Console.WriteLine("*** IDLE #" + session.GetIdleCount(IdleStatus.BothIdle) + " ***");
            }

            public override void ExceptionCaught(IOSession session, Exception cause)
            {
                session.Close(true);
            }

            public override void MessageReceived(IOSession session, object message)
            {
                var rb = message as IOBuffer;
                if (rb == null)
                    return;

                // Write the received data back to remote peer
                var wb = IOBuffer.Allocate(rb.Remaining);
                wb.Put(rb);
                wb.Flip();
                session.Write(wb);
            }
        }
    }
}
