using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.TextLine
{
    [TestClass]
    public class TextLineEncoderTest
    {
        [TestMethod]
        public void TestEncode()
        {
            var encoder = new TextLineEncoder(Encoding.UTF8, LineDelimiter.Windows);
            var session = new ProtocolCodecSession();
            var output = session.EncoderOutput;

            encoder.Encode(session, "ABC", output);
            Assert.AreEqual(1, session.EncoderOutputQueue.Count);
            var buf = (IOBuffer)session.EncoderOutputQueue.Dequeue();
            Assert.AreEqual(5, buf.Remaining);
            Assert.AreEqual((byte)'A', buf.Get());
            Assert.AreEqual((byte)'B', buf.Get());
            Assert.AreEqual((byte)'C', buf.Get());
            Assert.AreEqual((byte)'\r', buf.Get());
            Assert.AreEqual((byte)'\n', buf.Get());
        }
    }
}
