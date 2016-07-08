using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
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
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Filter.Stream
{
    [TestClass]
    public abstract class AbstractStreamWriteFilterTest<TM, TU>
        where TM : class
        where TU : AbstractStreamWriteFilter<TM>
    {
        [TestMethod]
        public void TestSetWriteBufferSize()
        {
            var filter = CreateFilter();

            try
            {
                filter.WriteBufferSize = 0;
                Assert.Fail("0 writeBuferSize specified. IllegalArgumentException expected.");
            }
            catch (ArgumentException)
            {
                // Pass, exception was thrown
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }

            try
            {
                filter.WriteBufferSize = -100;
                Assert.Fail("Negative writeBuferSize specified. IllegalArgumentException expected.");
            }
            catch (ArgumentException)
            {
                // Pass, exception was thrown
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }

            filter.WriteBufferSize = 1;
            Assert.AreEqual(1, filter.WriteBufferSize);
            filter.WriteBufferSize = 1024;
            Assert.AreEqual(1024, filter.WriteBufferSize);
        }

        [TestMethod]
        public void TestWriteUsingSocketTransport()
        {
            var acceptor = new AsyncSocketAcceptor();
            acceptor.ReuseAddress = true;
            var ep = new IPEndPoint(IPAddress.Loopback, 12345);

            var connector = new AsyncSocketConnector();

            // Generate 4MB of random data
            var data = new byte[4 * 1024 * 1024];
            new Random().NextBytes(data);

            byte[] expectedMd5;
            using (var md5 = MD5.Create())
            {
                expectedMd5 = md5.ComputeHash(data);
            }

            var message = CreateMessage(data);

            var sender = new SenderHandler(message);
            var receiver = new ReceiverHandler(data.Length);

            acceptor.Handler = sender;
            connector.Handler = receiver;

            acceptor.Bind(ep);
            connector.Connect(ep);
            sender.Countdown.Wait();
            receiver.Countdown.Wait();

            acceptor.Dispose();
            connector.Dispose();

            Assert.AreEqual(data.Length, receiver.Ms.Length);
            byte[] actualMd5;
            using (var md5 = MD5.Create())
            {
                actualMd5 = md5.ComputeHash(receiver.Ms.ToArray());
            }
            Assert.AreEqual(expectedMd5.Length, actualMd5.Length);
            for (var i = 0; i < expectedMd5.Length; i++)
            {
                Assert.AreEqual(expectedMd5[i], actualMd5[i]);
            }
        }

        protected abstract AbstractStreamWriteFilter<TM> CreateFilter();
        protected abstract TM CreateMessage(byte[] data);

        class SenderHandler : IOHandlerAdapter
        {
            TM _message;
            StreamWriteFilter _streamWriteFilter = new StreamWriteFilter();
            public CountdownEvent Countdown = new CountdownEvent(1);

            public SenderHandler(TM tm)
            {
                _message = tm;
            }

            public override void SessionCreated(IOSession session)
            {
                session.FilterChain.AddLast("codec", _streamWriteFilter);
            }

            public override void SessionOpened(IOSession session)
            {
                session.Write(_message);
            }

            public override void ExceptionCaught(IOSession session, Exception cause)
            {
                Countdown.Signal();
            }

            public override void SessionClosed(IOSession session)
            {
                Countdown.Signal();
            }

            public override void SessionIdle(IOSession session, IdleStatus status)
            {
                Countdown.Signal();
            }

            public override void MessageSent(IOSession session, object message)
            {
                if (message == this._message)
                {
                    Countdown.Signal();
                }
            }
        }

        class ReceiverHandler : IOHandlerAdapter
        {
            long _size;
            public CountdownEvent Countdown = new CountdownEvent(1);
            public MemoryStream Ms = new MemoryStream();

            public ReceiverHandler(long size)
            {
                this._size = size;
            }

            public override void SessionCreated(IOSession session)
            {
                session.Config.SetIdleTime(IdleStatus.ReaderIdle, 5);
            }

            public override void SessionIdle(IOSession session, IdleStatus status)
            {
                session.Close(true);
            }

            public override void ExceptionCaught(IOSession session, Exception cause)
            {
                Countdown.Signal();
            }

            public override void SessionClosed(IOSession session)
            {
                Countdown.Signal();
            }

            public override void MessageReceived(IOSession session, object message)
            {
                var buf = (IOBuffer)message;
                var bytes = new byte[buf.Remaining];
                buf.Get(bytes, 0, bytes.Length);
                Ms.Write(bytes, 0, bytes.Length);
                if (Ms.Length >= _size)
                    session.Close(true);
            }
        }
    }
}
