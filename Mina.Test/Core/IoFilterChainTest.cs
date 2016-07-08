using System;
using System.Linq;
using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Filter.Util;

namespace Mina.Core
{
    [TestClass]
    public class IoFilterChainTest
    {
        private DummySession _dummySession;
        private DummyHandler _handler;
        private IOFilterChain _chain;
        string _testResult;

        [TestInitialize]
        public void SetUp()
        {
            _dummySession = new DummySession();
            _handler = new DummyHandler(this);
            _dummySession.SetHandler(_handler);
            _chain = _dummySession.FilterChain;
            _testResult = string.Empty;
        }

        [TestMethod]
        public void TestAdd()
        {
            _chain.AddFirst("A", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("B", new EventOrderTestFilter(this, 'A'));
            _chain.AddFirst("C", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("D", new EventOrderTestFilter(this, 'A'));
            _chain.AddBefore("B", "E", new EventOrderTestFilter(this, 'A'));
            _chain.AddBefore("C", "F", new EventOrderTestFilter(this, 'A'));
            _chain.AddAfter("B", "G", new EventOrderTestFilter(this, 'A'));
            _chain.AddAfter("D", "H", new EventOrderTestFilter(this, 'A'));

            var actual = "";

            foreach (var e in _chain.GetAll())
            {
                actual += e.Name;
            }

            Assert.AreEqual("FCAEBGDH", actual);
        }

        [TestMethod]
        public void TestGet()
        {
            IOFilter filterA = new NoopFilter();
            IOFilter filterB = new NoopFilter();
            IOFilter filterC = new NoopFilter();
            IOFilter filterD = new NoopFilter();

            _chain.AddFirst("A", filterA);
            _chain.AddLast("B", filterB);
            _chain.AddBefore("B", "C", filterC);
            _chain.AddAfter("A", "D", filterD);

            Assert.AreSame(filterA, _chain.Get("A"));
            Assert.AreSame(filterB, _chain.Get("B"));
            Assert.AreSame(filterC, _chain.Get("C"));
            Assert.AreSame(filterD, _chain.Get("D"));
        }

        [TestMethod]
        public void TestRemove()
        {
            _chain.AddLast("A", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("B", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("C", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("D", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("E", new EventOrderTestFilter(this, 'A'));

            _chain.Remove("A");
            _chain.Remove("E");
            _chain.Remove("C");
            _chain.Remove("B");
            _chain.Remove("D");

            Assert.AreEqual(0, _chain.GetAll().Count());
        }

        [TestMethod]
        public void TestClear()
        {
            _chain.AddLast("A", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("B", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("C", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("D", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("E", new EventOrderTestFilter(this, 'A'));

            _chain.Clear();

            Assert.AreEqual(0, _chain.GetAll().Count());
        }

        [TestMethod]
        public void TestDefault()
        {
            Run("HS0 HSO HMR HMS HSI HEC HSC");
        }

        [TestMethod]
        public void TestChained()
        {
            _chain.AddLast("A", new EventOrderTestFilter(this, 'A'));
            _chain.AddLast("B", new EventOrderTestFilter(this, 'B'));
            Run("AS0 BS0 HS0" + "ASO BSO HSO" + "AMR BMR HMR" + "BFW AFW AMS BMS HMS" + "ASI BSI HSI" + "AEC BEC HEC"
                    + "ASC BSC HSC");
        }

        [TestMethod]
        public void TestAddRemove()
        {
            IOFilter filter = new AddRemoveTestFilter(this);

            _chain.AddFirst("A", filter);
            Assert.AreEqual("ADDED", _testResult);

            _chain.Remove("A");
            Assert.AreEqual("ADDEDREMOVED", _testResult);
        }

        private void Run(string expectedResult)
        {
            _chain.FireSessionCreated();
            _chain.FireSessionOpened();
            _chain.FireMessageReceived(new object());
            _chain.FireFilterWrite(new DefaultWriteRequest(new object()));
            _chain.FireSessionIdle(IdleStatus.ReaderIdle);
            _chain.FireExceptionCaught(new Exception());
            _chain.FireSessionClosed();

            _testResult = FormatResult(_testResult);
            var formatedExpectedResult = FormatResult(expectedResult);

            Assert.AreEqual(formatedExpectedResult, _testResult);
        }

        private string FormatResult(string result)
        {
            var newResult = result.Replace(" ", "");
            var buf = new StringBuilder(newResult.Length * 4 / 3);

            for (var i = 0; i < newResult.Length; i++)
            {
                buf.Append(newResult[i]);

                if (i % 3 == 2)
                {
                    buf.Append(' ');
                }
            }

            return buf.ToString();
        }

        class DummyHandler : IOHandlerAdapter
        {
            private readonly IoFilterChainTest _test;

            public DummyHandler(IoFilterChainTest test)
            {
                this._test = test;
            }

            public override void SessionCreated(IOSession session)
            {
                _test._testResult += "HS0";
            }

            public override void SessionOpened(IOSession session)
            {
                _test._testResult += "HSO";
            }

            public override void SessionClosed(IOSession session)
            {
                _test._testResult += "HSC";
            }

            public override void SessionIdle(IOSession session, IdleStatus status)
            {
                _test._testResult += "HSI";
            }

            public override void ExceptionCaught(IOSession session, Exception cause)
            {
                _test._testResult += "HEC";
            }

            public override void MessageReceived(IOSession session, object message)
            {
                _test._testResult += "HMR";
            }

            public override void MessageSent(IOSession session, object message)
            {
                _test._testResult += "HMS";
            }
        }

        class EventOrderTestFilter : IOFilterAdapter
        {
            private readonly char _id;
            private readonly IoFilterChainTest _test;

            public EventOrderTestFilter(IoFilterChainTest test, char id)
            {
                this._test = test;
                this._id = id;
            }

            public override void SessionCreated(INextFilter nextFilter, IOSession session)
            {
                _test._testResult += _id + "S0";
                nextFilter.SessionCreated(session);
            }

            public override void SessionOpened(INextFilter nextFilter, IOSession session)
            {
                _test._testResult += _id + "SO";
                nextFilter.SessionOpened(session);
            }

            public override void SessionClosed(INextFilter nextFilter, IOSession session)
            {
                _test._testResult += _id + "SC";
                nextFilter.SessionClosed(session);
            }

            public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
            {
                _test._testResult += _id + "SI";
                nextFilter.SessionIdle(session, status);
            }

            public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
            {
                _test._testResult += _id + "EC";
                nextFilter.ExceptionCaught(session, cause);
            }

            public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
            {
                _test._testResult += _id + "FW";
                nextFilter.FilterWrite(session, writeRequest);
            }

            public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
            {
                _test._testResult += _id + "MR";
                nextFilter.MessageReceived(session, message);
            }

            public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
            {
                _test._testResult += _id + "MS";
                nextFilter.MessageSent(session, writeRequest);
            }

            public override void FilterClose(INextFilter nextFilter, IOSession session)
            {
                nextFilter.FilterClose(session);
            }
        }

        private class AddRemoveTestFilter : IOFilterAdapter
        {
            private readonly IoFilterChainTest _test;

            public AddRemoveTestFilter(IoFilterChainTest test)
            {
                this._test = test;
            }

            public override void OnPostAdd(IOFilterChain parent, string name, INextFilter nextFilter)
            {
                _test._testResult += "ADDED";
            }

            public override void OnPostRemove(IOFilterChain parent, string name, INextFilter nextFilter)
            {
                _test._testResult += "REMOVED";
            }
        }
    }
}
