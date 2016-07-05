using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using Mina.Core.Future;

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractConnectorTest
    {
        [TestMethod]
        public void TestConnectFutureSuccessTiming()
        {
            var port = 12345;
            var acceptor = CreateAcceptor();
            acceptor.Bind(CreateEndPoint(port));

            var buf = new StringBuilder();
            try
            {
                var connector = CreateConnector();
                connector.SessionCreated += (s, e) => buf.Append("1");
                connector.SessionOpened += (s, e) => buf.Append("2");
                connector.ExceptionCaught += (s, e) => buf.Append("X");

                var future = connector.Connect(CreateEndPoint(port));
                future.Await();
                buf.Append("3");
                future.Session.Close(true);
                // sessionCreated() will fire before the connect future completes
                // but sessionOpened() may not
                Assert.IsTrue(new Regex("12?32?").IsMatch(buf.ToString()));
            }
            finally
            {
                acceptor.Unbind();
                acceptor.Dispose();
            }
        }

        [TestMethod]
        public virtual void TestConnectFutureFailureTiming()
        {
            var port = 12345;
            var buf = new StringBuilder();

            var connector = CreateConnector();
            connector.SessionCreated += (s, e) => buf.Append("X");
            connector.SessionOpened += (s, e) => buf.Append("Y");
            connector.ExceptionCaught += (s, e) => buf.Append("Z");

            try
            {
                var future = connector.Connect(CreateEndPoint(port));
                future.Await();
                buf.Append("1");
                try
                {
                    future.Session.Close(true);
                    Assert.Fail();
                }
                catch
                {
                    // Signifies a successful test execution
                    Assert.IsTrue(true);
                }
                Assert.AreEqual("1", buf.ToString());
            }
            finally
            {
                connector.Dispose();
            }
        }

        [TestMethod]
        public void TestSessionCallbackInvocation()
        {
            var callbackInvoked = 0;
            var sessionCreatedInvoked = 1;
            var sessionCreatedInvokedBeforeCallback = 2;
            bool[] assertions = { false, false, false };
            var countdown = new CountdownEvent(2);
            var callbackFuture = new IConnectFuture[1];

            var port = 12345;

            var acceptor = CreateAcceptor();
            var connector = CreateConnector();

            try
            {
                acceptor.Bind(CreateEndPoint(port));

                connector.SessionCreated += (s, e) =>
                {
                    assertions[sessionCreatedInvoked] = true;
                    assertions[sessionCreatedInvokedBeforeCallback] = !assertions[callbackInvoked];
                    countdown.Signal();
                };

                var future = connector.Connect(CreateEndPoint(port), (s, f) =>
                {
                    assertions[callbackInvoked] = true;
                    callbackFuture[0] = f;
                    countdown.Signal();
                });

                Assert.IsTrue(countdown.Wait(TimeSpan.FromSeconds(5)), "Timed out waiting for callback and IoHandler.sessionCreated to be invoked");
                Assert.IsTrue(assertions[callbackInvoked], "Callback was not invoked");
                Assert.IsTrue(assertions[sessionCreatedInvoked], "IoHandler.sessionCreated was not invoked");
                Assert.IsFalse(assertions[sessionCreatedInvokedBeforeCallback], "IoHandler.sessionCreated was invoked before session callback");
                Assert.AreSame(future, callbackFuture[0], "Callback future should have been same future as returned by connect");
            }
            finally
            {
                try
                {
                    connector.Dispose();
                }
                finally
                {
                    acceptor.Dispose();
                }
            }
        }

        protected abstract IOAcceptor CreateAcceptor();
        protected abstract IOConnector CreateConnector();
        protected abstract EndPoint CreateEndPoint(int port);
    }
}
