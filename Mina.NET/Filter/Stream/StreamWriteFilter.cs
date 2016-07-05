using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Stream
{
    /// <summary>
    /// Filter implementation which makes it possible to write <see cref="System.IO.Stream"/>
    /// objects directly using <see cref="IOSession.Write(object)"/>.
    /// <remarks>
    /// When an <see cref="System.IO.Stream"/> is written to a session this filter will read the bytes
    /// from the stream into <see cref="IOBuffer"/> objects and write those buffers
    /// to the next filter.
    /// <para>
    /// This filter will ignore written messages which aren't <see cref="System.IO.Stream"/>
    /// instances. Such messages will be passed to the next filter directly.
    /// </para>
    /// <para>
    /// NOTE: this filter does not close the stream after all data from stream has been written.
    /// </para>
    /// </remarks>
    /// </summary>
    public class StreamWriteFilter : AbstractStreamWriteFilter<System.IO.Stream>
    {
        /// <inheritdoc/>
        protected override IOBuffer GetNextBuffer(System.IO.Stream stream)
        {
            var bytes = new byte[WriteBufferSize];

            var off = 0;
            var n = 0;
            while (off < bytes.Length && (n = stream.Read(bytes, off, bytes.Length - off)) > 0)
            {
                off += n;
            }

            if (n <= 0 && off == 0)
                return null;

            return IOBuffer.Wrap(bytes, 0, off);
        }
    }
}
