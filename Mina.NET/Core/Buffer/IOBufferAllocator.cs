using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// Allocates <see cref="IOBuffer"/>s and manages them.
    /// Please implement this interface if you need more advanced memory management scheme.
    /// </summary>
    public interface IOBufferAllocator
    {
        /// <summary>
        /// Returns the buffer which is capable of the specified size.
        /// </summary>
        /// <param name="capacity">the capacity of the buffer</param>
        /// <returns>the allocated buffer</returns>
        /// <exception cref="ArgumentException">If the <paramref name="capacity"/> is a negative integer</exception>
        IOBuffer Allocate(int capacity);

        /// <summary>
        /// Wraps the specified byte array into Mina.NET buffer.
        /// </summary>
        IOBuffer Wrap(byte[] array);

        /// <summary>
        /// Wraps the specified byte array into Mina.NET buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// If the preconditions on the <paramref name="offset"/> and <paramref name="length"/>
        /// parameters do not hold
        /// </exception>
        IOBuffer Wrap(byte[] array, int offset, int length);
    }
}
