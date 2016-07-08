using System;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    [TestClass]
    public class ExecutorFilterRegressionTest
    {
        private ExecutorFilter _filter = new ExecutorFilter();

        [TestMethod]
        public void TestEventOrder()
        {
            var nextFilter = new EventOrderChecker();
            var sessions = new EventOrderCounter[] { new EventOrderCounter(),
                new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(),
                new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(), new EventOrderCounter(),
                new EventOrderCounter(), };
            var loop = 1000000;
            var end = sessions.Length - 1;
            var filter = this._filter;

            for (var i = 0; i < loop; i++)
            {
                for (var j = end; j >= 0; j--)
                {
                    filter.MessageReceived(nextFilter, sessions[j], i);
                }

                if (nextFilter.Exception != null)
                    throw nextFilter.Exception;
            }

            System.Threading.Thread.Sleep(2000);

            for (var i = end; i >= 0; i--)
            {
                Assert.AreEqual(loop - 1, sessions[i].LastCount);
            }
        }

        class EventOrderCounter : DummySession
        {
            int _lastCount = -1;

            public int LastCount
            {
                get { return _lastCount; }
                set
                {
                    if (_lastCount > -1)
                        Assert.AreEqual(_lastCount + 1, value);
                    _lastCount = value;
                }
            }
        }

        class EventOrderChecker : INextFilter
        {
            public Exception Exception;

            public void MessageReceived(IOSession session, object message)
            {
                try
                {
                    ((EventOrderCounter)session).LastCount = (int)message;
                }
                catch (Exception e)
                {
                    if (Exception == null)
                        Exception = e;
                }
            }

            public void ExceptionCaught(IOSession session, Exception cause)
            {
                throw new NotImplementedException();
            }

            public void FilterClose(IOSession session)
            {
                // Do nothing
            }

            public void FilterWrite(IOSession session, IWriteRequest writeRequest)
            {
                // Do nothing
            }

            public void MessageSent(IOSession session, IWriteRequest writeRequest)
            {
                // Do nothing
            }

            public void SessionClosed(IOSession session)
            {
                // Do nothing
            }

            public void SessionCreated(IOSession session)
            {
                // Do nothing
            }

            public void SessionIdle(IOSession session, IdleStatus status)
            {
                // Do nothing
            }

            public void SessionOpened(IOSession session)
            {
                // Do nothing
            }

            public void InputClosed(IOSession session)
            {
                // Do nothing
            }
        }
    }
}
