using System;
using System.Collections.Generic;
using System.Linq;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    [TestClass]
    public class CumulativeProtocolDecoderTest
    {
        ProtocolCodecSession _session = new ProtocolCodecSession();
        IOBuffer _buf;
        IntegerDecoder _decoder;

        [TestInitialize]
        public void SetUp()
        {
            _buf = IOBuffer.Allocate(16);
            _decoder = new IntegerDecoder();
            _session.SetTransportMetadata(new DefaultTransportMetadata("mina", "dummy", false, true, typeof(System.Net.IPEndPoint)));
        }

        [TestMethod]
        public void TestCumulation()
        {
            _buf.Put(0);
            _buf.Flip();

            _decoder.Decode(_session, _buf, _session.DecoderOutput);
            Assert.AreEqual(0, _session.DecoderOutputQueue.Count);
            Assert.AreEqual(_buf.Limit, _buf.Position);

            _buf.Clear();
            _buf.Put(0);
            _buf.Put(0);
            _buf.Put(1);
            _buf.Flip();

            _decoder.Decode(_session, _buf, _session.DecoderOutput);
            Assert.AreEqual(1, _session.DecoderOutputQueue.Count);
            Assert.AreEqual(1, _session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual(_buf.Limit, _buf.Position);
        }

        [TestMethod]
        public void TestRepeatitiveDecode()
        {
            for (var i = 0; i < 4; i++)
            {
                _buf.PutInt32(i);
            }
            _buf.Flip();

            _decoder.Decode(_session, _buf, _session.DecoderOutput);
            Assert.AreEqual(4, _session.DecoderOutputQueue.Count);
            Assert.AreEqual(_buf.Limit, _buf.Position);

            var expected = new List<object>();

            for (var i = 0; i < 4; i++)
            {
                Assert.IsTrue(_session.DecoderOutputQueue.Contains(i));
            }
        }

        [TestMethod]
        public void TestWrongImplementationDetection()
        {
            try
            {
                new WrongDecoder().Decode(_session, _buf, _session.DecoderOutput);
                Assert.Fail();
            }
            catch (InvalidOperationException)
            {
                // OK
            }
        }

        [TestMethod]
        public void TestBufferDerivation()
        {
            _decoder = new DuplicatingIntegerDecoder();

            _buf.PutInt32(1);

            // Put some extra byte to make the decoder create an internal buffer.
            _buf.Put(0);
            _buf.Flip();

            _decoder.Decode(_session, _buf, _session.DecoderOutput);
            Assert.AreEqual(1, _session.DecoderOutputQueue.Count);
            Assert.AreEqual(1, _session.DecoderOutputQueue.Dequeue());
            Assert.AreEqual(_buf.Limit, _buf.Position);

            // Keep appending to the internal buffer.
            // DuplicatingIntegerDecoder will keep duplicating the internal
            // buffer to disable auto-expansion, and CumulativeProtocolDecoder
            // should detect that user derived its internal buffer.
            // Consequently, CumulativeProtocolDecoder will perform 
            // reallocation to avoid putting incoming data into
            // the internal buffer with auto-expansion disabled.
            for (var i = 2; i < 10; i++)
            {
                _buf.Clear();
                _buf.PutInt32(i);
                // Put some extra byte to make the decoder keep the internal buffer.
                _buf.Put(0);
                _buf.Flip();
                _buf.Position = 1;

                _decoder.Decode(_session, _buf, _session.DecoderOutput);
                Assert.AreEqual(1, _session.DecoderOutputQueue.Count);
                Assert.AreEqual(i, _session.DecoderOutputQueue.Dequeue());
                Assert.AreEqual(_buf.Limit, _buf.Position);
            }
        }

        class IntegerDecoder : CumulativeProtocolDecoder
        {
            protected override bool DoDecode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
            {
                Assert.IsTrue(input.HasRemaining);

                if (input.Remaining < 4)
                    return false;

                output.Write(input.GetInt32());
                return true;
            }
        }

        class WrongDecoder : CumulativeProtocolDecoder
        {
            protected override bool DoDecode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
            {
                return true;
            }
        }

        class DuplicatingIntegerDecoder : IntegerDecoder
        {
            protected override bool DoDecode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
            {
                input.Duplicate(); // Will disable auto-expansion.
                Assert.IsFalse(input.AutoExpand);
                return base.DoDecode(session, input, output);
            }
        }
    }
}
