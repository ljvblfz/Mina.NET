using System;
using System.Net;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    [TestClass]
    public class DatagramSessionIdleTest
    {
        private bool _readerIdleReceived;
        private bool _writerIdleReceived;
        private bool _bothIdleReceived;
        private object _mutex = new object();

        [TestMethod]
        public void TestSessionIdle()
        {
            var readerIdleTime = 3;//seconds
            var writerIdleTime = readerIdleTime + 2;//seconds
            var bothIdleTime = writerIdleTime + 2;//seconds

            var acceptor = new AsyncDatagramAcceptor();
            acceptor.SessionConfig.SetIdleTime(IdleStatus.BothIdle, bothIdleTime);
            acceptor.SessionConfig.SetIdleTime(IdleStatus.ReaderIdle, readerIdleTime);
            acceptor.SessionConfig.SetIdleTime(IdleStatus.WriterIdle, writerIdleTime);
            var ep = new IPEndPoint(IPAddress.Loopback, 1234);
            acceptor.SessionIdle += (s, e) =>
            {
                if (e.IdleStatus == IdleStatus.BothIdle)
                {
                    _bothIdleReceived = true;
                }
                else if (e.IdleStatus == IdleStatus.ReaderIdle)
                {
                    _readerIdleReceived = true;
                }
                else if (e.IdleStatus == IdleStatus.WriterIdle)
                {
                    _writerIdleReceived = true;
                }

                lock (_mutex)
                {
                    System.Threading.Monitor.PulseAll(_mutex);
                }
            };
            acceptor.Bind(ep);
            var session = acceptor.NewSession(new IPEndPoint(IPAddress.Loopback, 1024), ep);

            //check properties to be copied from acceptor to session
            Assert.AreEqual(bothIdleTime, session.Config.BothIdleTime);
            Assert.AreEqual(readerIdleTime, session.Config.ReaderIdleTime);
            Assert.AreEqual(writerIdleTime, session.Config.WriterIdleTime);

            //verify that IDLE events really received by handler
            var startTime = DateTime.Now;

            lock (_mutex)
            {
                while (!_readerIdleReceived && (DateTime.Now - startTime).TotalMilliseconds < (readerIdleTime + 1) * 1000)
                    try
                    {
                        System.Threading.Monitor.Wait(_mutex, readerIdleTime * 1000);                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }

            Assert.IsTrue(_readerIdleReceived);

            lock (_mutex)
            {
                while (!_writerIdleReceived && (DateTime.Now - startTime).TotalMilliseconds < (writerIdleTime + 1) * 1000)
                    try
                    {
                        System.Threading.Monitor.Wait(_mutex, (writerIdleTime - readerIdleTime) * 1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }

            Assert.IsTrue(_writerIdleReceived);

            lock (_mutex)
            {
                while (!_bothIdleReceived && (DateTime.Now - startTime).TotalMilliseconds < (bothIdleTime + 1) * 1000)
                    try
                    {
                        System.Threading.Monitor.Wait(_mutex, (bothIdleTime - writerIdleTime) * 1000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
            }

            Assert.IsTrue(_bothIdleReceived);
        }
    }
}
