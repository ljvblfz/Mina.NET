using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A simplistic <see cref="IOBufferAllocator"/> which simply allocates a new
    /// buffer every time.
    /// </summary>
    public class ByteBufferAllocator : IOBufferAllocator
    {
        /// <summary>
        /// Static instance of <see cref="ByteBufferAllocator"/>.
        /// </summary>
        public static readonly ByteBufferAllocator Instance = new ByteBufferAllocator();

        /// <inheritdoc/>
        public IOBuffer Allocate(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("Capacity should be >= 0", nameof(capacity));
            }
            return new ByteBuffer(this, capacity, capacity);
        }

        /// <inheritdoc/>
        public IOBuffer Wrap(byte[] array, int offset, int length)
        {
            try
            {
                return new ByteBuffer(this, array, offset, length);
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        public IOBuffer Wrap(byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }
    }
}
