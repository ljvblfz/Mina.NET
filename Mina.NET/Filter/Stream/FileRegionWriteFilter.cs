using System;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Service;

namespace Mina.Filter.Stream
{
    /// <summary>
    /// Filter that converts a <see cref="IFileRegion"/> to <see cref="IOBuffer"/>
    /// objects and writes those buffers to the next filter.
    /// </summary>
    /// <remarks>
    /// Normall FileInfo objects should be handled by the <see cref="IOProcessor"/>
    /// but this is not always possible if a filter is being used that needs to
    /// modify the contents of the file before sending over the network (i.e. the
    /// <see cref="Filter.Ssl.SslFilter"/> or a data compression filter.)
    /// </remarks>
    public class FileRegionWriteFilter : AbstractStreamWriteFilter<IFileRegion>
    {
        /// <inheritdoc/>
        protected override IOBuffer GetNextBuffer(IFileRegion fileRegion)
        {
            if (fileRegion.RemainingBytes <= 0L)
                return null;

            var bufferSize = (int)Math.Min(WriteBufferSize, fileRegion.RemainingBytes);
            var buffer = IOBuffer.Allocate(bufferSize);
            fileRegion.Read(buffer);
            buffer.Flip();
            return buffer;
        }
    }
}
