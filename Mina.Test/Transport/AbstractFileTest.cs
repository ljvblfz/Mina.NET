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

namespace Mina.Transport
{
    [TestClass]
    public abstract class AbstractFileTest
    {
        private const int FileSize = 1 * 1024 * 1024; // 1MB file
        private FileInfo _file;

        [TestInitialize]
        public void SetUp()
        {
            _file = CreateLargeFile();
        }

        [TestCleanup]
        public void TearDown()
        {
            _file.Delete();
        }

        [TestMethod]
        public void TestSendLargeFile()
        {
            Assert.AreEqual(FileSize, _file.Length, "Test file not as big as specified");

            var countdown = new CountdownEvent(1);
            bool[] success = { false };
            Exception[] exception = { null };

            var port = 12345;
            var acceptor = CreateAcceptor();
            var connector = CreateConnector();

            try
            {
                acceptor.ExceptionCaught += (s, e) =>
                {
                    exception[0] = e.Exception;
                    e.Session.Close(true);
                };

                var index = 0;
                acceptor.MessageReceived += (s, e) =>
                {
                    var buffer = (IOBuffer)e.Message;
                    while (buffer.HasRemaining)
                    {
                        var x = buffer.GetInt32();
                        if (x != index)
                        {
                            throw new Exception(string.Format("Integer at {0} was {1} but should have been {0}", index, x));
                        }
                        index++;
                    }
                    if (index > FileSize / 4)
                    {
                        throw new Exception("Read too much data");
                    }
                    if (index == FileSize / 4)
                    {
                        success[0] = true;
                        e.Session.Close(true);
                    }
                };

                acceptor.Bind(CreateEndPoint(port));

                connector.ExceptionCaught += (s, e) =>
                {
                    exception[0] = e.Exception;
                    e.Session.Close(true);
                };
                connector.SessionClosed += (s, e) => countdown.Signal();

                var future = connector.Connect(CreateEndPoint(port));
                future.Await();

                var session = future.Session;
                session.Write(_file);

                countdown.Wait();

                if (exception[0] != null)
                    throw exception[0];

                Assert.IsTrue(success[0], "Did not complete file transfer successfully");
                Assert.AreEqual(1, session.WrittenMessages, "Written messages should be 1 (we wrote one file)");
                Assert.AreEqual(FileSize, session.WrittenBytes, "Written bytes should match file size");
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

        private static FileInfo CreateLargeFile()
        {
            var buffer = IOBuffer.Allocate(FileSize);
            for (var i = 0; i < FileSize / 4; i++)
            {
                buffer.PutInt32(i);
            }
            buffer.Flip();

            var path = Path.GetTempFileName();
            var data = new byte[buffer.Remaining];
            buffer.Get(data, 0, data.Length);
            File.WriteAllBytes(path, data);
            return new FileInfo(path);
        }
    }
}
