using System;
using System.IO;
using System.Threading;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core
{
    [TestClass]
    public class FutureTest
    {
        [TestMethod]
        public void TestCloseFuture()
        {
            var future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            var thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);
        }

        [TestMethod]
        public void TestConnectFuture()
        {
            var future = new DefaultConnectFuture();
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Connected);
            Assert.IsNull(future.Session);
            Assert.IsNull(future.Exception);

            var thread = new TestThread(future);
            thread.Start();

            IOSession session = new DummySession();

            future.SetSession(session);
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Connected);
            Assert.AreSame(session, future.Session);
            Assert.IsNull(future.Exception);

            future = new DefaultConnectFuture();
            thread = new TestThread(future);
            thread.Start();
            future.Exception = new IOException();
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsFalse(future.Connected);
            Assert.IsTrue(future.Exception is IOException);

            try
            {
                var s = future.Session;
                Assert.Fail("IOException should be thrown.");
            }
            catch (Exception)
            {
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void TestWriteFuture()
        {
            var future = new DefaultWriteFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Written);

            var thread = new TestThread(future);
            thread.Start();

            future.Written = true;
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Written);

            future = new DefaultWriteFuture(null);
            thread = new TestThread(future);
            thread.Start();

            future.Exception = new Exception();
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsFalse(future.Written);
            Assert.IsTrue(future.Exception.GetType() == typeof(Exception));
        }

        [TestMethod]
        public void TestAddListener()
        {
            var future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            IOFuture f1 = null, f2 = null;

            future.Complete += (s, e) => f1 = e.Future;
            future.Complete += (s, e) => f2 = e.Future;

            var thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            Assert.AreSame(future, f1);
            Assert.AreSame(future, f2);
        }

        [TestMethod]
        public void TestLateAddListener()
        {
            var future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            var thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            IOFuture f1 = null;
            future.Complete += (s, e) => f1 = e.Future;
            Assert.AreSame(future, f1);
        }
        
        [TestMethod]
        public void TestRemoveListener1()
        {
            var future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            IOFuture f1 = null, f2 = null;
            EventHandler<IoFutureEventArgs> listener1 = (s, e) => f1 = e.Future;
            EventHandler<IoFutureEventArgs> listener2 = (s, e) => f2 = e.Future;

            future.Complete += listener1;
            future.Complete += listener2;
            future.Complete -= listener1;

            var thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            Assert.AreSame(null, f1);
            Assert.AreSame(future, f2);
        }

        [TestMethod]
        public void TestRemoveListener2()
        {
            var future = new DefaultCloseFuture(null);
            Assert.IsFalse(future.Done);
            Assert.IsFalse(future.Closed);

            IOFuture f1 = null, f2 = null;
            EventHandler<IoFutureEventArgs> listener1 = (s, e) => f1 = e.Future;
            EventHandler<IoFutureEventArgs> listener2 = (s, e) => f2 = e.Future;

            future.Complete += listener1;
            future.Complete += listener2;
            future.Complete -= listener2;

            var thread = new TestThread(future);
            thread.Start();

            future.Closed = true;
            thread.Join();

            Assert.IsTrue(thread.Success);
            Assert.IsTrue(future.Done);
            Assert.IsTrue(future.Closed);

            Assert.AreSame(future, f1);
            Assert.AreSame(null, f2);
        }

        private class TestThread
        {
            public bool Success;
            readonly IOFuture _future;
            readonly Thread _t;

            public TestThread(IOFuture future)
            {
                this._future = future;
                _t = new Thread(Run);
            }

            public void Start()
            {
                _t.Start();
            }

            public void Join()
            {
                _t.Join();
            }

            public void Run()
            {
                Success = _future.Await(10000);
            }
        }
    }
}
