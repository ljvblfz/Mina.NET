#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.Serialization
{
    [TestClass]
    public class ObjectSerializationTest
    {
        [TestMethod]
        public void TestEncoder()
        {
            var expected = "1234";

            var session = new ProtocolCodecSession();
            var output = session.EncoderOutput;

            IProtocolEncoder encoder = new ObjectSerializationEncoder();
            encoder.Encode(session, expected, output);

            Assert.AreEqual(1, session.EncoderOutputQueue.Count);
            var buf = (IOBuffer)session.EncoderOutputQueue.Dequeue();

            TestDecoderAndInputStream(expected, buf);
        }

        private void TestDecoderAndInputStream(string expected, IOBuffer input)
        {
            // Test ProtocolDecoder
            IProtocolDecoder decoder = new ObjectSerializationDecoder();
            var session = new ProtocolCodecSession();
            var decoderOut = session.DecoderOutput;
            decoder.Decode(session, input.Duplicate(), decoderOut);

            Assert.AreEqual(1, session.DecoderOutputQueue.Count);
            Assert.AreEqual(expected, session.DecoderOutputQueue.Dequeue());
        }
    }
}
