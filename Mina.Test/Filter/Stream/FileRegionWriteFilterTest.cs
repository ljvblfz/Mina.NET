using System.IO;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.File;

namespace Mina.Filter.Stream
{
    [TestClass]
    public class FileRegionWriteFilterTest : AbstractStreamWriteFilterTest<IFileRegion, FileRegionWriteFilter>
    {
        private string _file;

        [TestInitialize]
        public void SetUp()
        {
            _file = Path.GetTempFileName();
        }

        [TestCleanup]
        public void TearDown()
        {
            File.Delete(_file);
        }

        protected override AbstractStreamWriteFilter<IFileRegion> CreateFilter()
        {
            return new FileRegionWriteFilter();
        }

        protected override IFileRegion CreateMessage(byte[] data)
        {
            File.WriteAllBytes(_file, data);
            return new FileInfoFileRegion(new FileInfo(_file));
        }
    }
}
