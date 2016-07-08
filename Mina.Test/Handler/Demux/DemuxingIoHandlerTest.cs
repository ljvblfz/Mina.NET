﻿#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Mina.Handler.Demux
{
    [TestClass]
    public class DemuxingIoHandlerTest
    {
        [TestInitialize]
        public void SetUp()
        {
        }
    }
}
