using System;
using Mina.Core.Buffer;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IOBufferAllocator"/> which allocates <see cref="SocketAsyncEventArgsBuffer"/>.
    /// </summary>
    public class SocketAsyncEventArgsBufferAllocator : IOBufferAllocator
    {
        /// <summary>
        /// Static instance.
        /// </summary>
        public static readonly SocketAsyncEventArgsBufferAllocator Instance = new SocketAsyncEventArgsBufferAllocator();

        /// <summary>
        /// Returns the buffer which is capable of the specified size.
        /// </summary>
        /// <param name="capacity">the capacity of the buffer</param>
        /// <returns>the allocated buffer</returns>
        /// <exception cref="ArgumentException">If the <paramref name="capacity"/> is a negative integer</exception>
        public SocketAsyncEventArgsBuffer Allocate(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity should be >= 0", nameof(capacity));
            return new SocketAsyncEventArgsBuffer(this, capacity, capacity);
        }

        /// <summary>
        /// Wraps the specified byte array into a <see cref="SocketAsyncEventArgsBuffer"/>.
        /// </summary>
        public SocketAsyncEventArgsBuffer Wrap(byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }

        /// <summary>
        /// Wraps the specified byte array into a <see cref="SocketAsyncEventArgsBuffer"/>.
        /// </summary>
        public SocketAsyncEventArgsBuffer Wrap(byte[] array, int offset, int length)
        {
            try
            {
                return new SocketAsyncEventArgsBuffer(this, array, offset, length);
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException();
            }
        }

        IOBuffer IOBufferAllocator.Allocate(int capacity)
        {
            return Allocate(capacity);
        }

        IOBuffer IOBufferAllocator.Wrap(byte[] array)
        {
            return Wrap(array);
        }

        IOBuffer IOBufferAllocator.Wrap(byte[] array, int offset, int length)
        {
            return Wrap(array, offset, length);
        }
    }
}
