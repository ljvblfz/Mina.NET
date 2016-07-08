using System;
using System.IO;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// Wraps an <see cref="IOBuffer"/> as a stream.
    /// </summary>
    public class IOBufferStream : Stream
    {
        private readonly IOBuffer _buffer;

        /// <summary>
        /// </summary>
        public IOBufferStream(IOBuffer buffer)
        {
            _buffer = buffer;
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void Flush()
        {
            // do nothing
        }

        /// <inheritdoc/>
        public override long Length => _buffer.Remaining;

        /// <inheritdoc/>
        public override long Position
        {
            get { return _buffer.Position; }
            set { _buffer.Position = (int)value; }
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = Math.Min(_buffer.Remaining, count);
            _buffer.Get(buffer, offset, read);
            return read;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _buffer.Remaining - offset;
                    break;
                default:
                    break;
            }
            return Position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _buffer.Put(buffer, offset, count);
        }
    }
}
