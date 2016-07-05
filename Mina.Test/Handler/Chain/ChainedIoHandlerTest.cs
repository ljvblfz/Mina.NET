using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    [TestClass]
    public class ChainedIoHandlerTest
    {
        [TestMethod]
        public void TestChainedCommand()
        {
            var chain = new IOHandlerChain();
            var buf = new StringBuilder();
            chain.AddLast("A", new TestCommand(buf, 'A'));
            chain.AddLast("B", new TestCommand(buf, 'B'));
            chain.AddLast("C", new TestCommand(buf, 'C'));

            new ChainedIOHandler(chain).MessageReceived(new DummySession(), null);

            Assert.AreEqual("ABC", buf.ToString());
        }

        private class TestCommand : IOHandlerCommand
        {
            private readonly StringBuilder _sb;
            private readonly char _ch;

            public TestCommand(StringBuilder sb, char ch)
            {
                _sb = sb;
                _ch = ch;
            }

            public void Execute(INextCommand next, IOSession session, object message)
            {
                _sb.Append(_ch);
                next.Execute(session, message);
            }
        }
    }
}
