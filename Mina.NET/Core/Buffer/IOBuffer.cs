using System;
using System.Text;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A byte buffer used by MINA applications.
    /// </summary>
    public abstract class IOBuffer : Buffer
    {
        private static IOBufferAllocator _allocator = ByteBufferAllocator.Instance;

        /// <summary>
        /// Gets or sets the allocator used by new buffers.
        /// </summary>
        public static IOBufferAllocator Allocator
        {
            get { return _allocator; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (_allocator != null && _allocator != value && _allocator is IDisposable)
                {
                    ((IDisposable) _allocator).Dispose();
                }
                _allocator = value;
            }
        }

        /// <summary>
        /// Returns the direct or heap buffer which is capable to store the specified amount of bytes.
        /// </summary>
        /// <param name="capacity">the capacity of the buffer</param>
        /// <returns>the allocated buffer</returns>
        /// <exception cref="ArgumentException">If the <paramref name="capacity"/> is a negative integer</exception>
        public static IOBuffer Allocate(int capacity)
        {
            return _allocator.Allocate(capacity);
        }

        /// <summary>
        /// Wraps the specified byte array into MINA buffer.
        /// </summary>
        public static IOBuffer Wrap(byte[] array)
        {
            return _allocator.Wrap(array);
        }

        /// <summary>
        /// Wraps the specified byte array into MINA buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// If the preconditions on the <paramref name="offset"/> and <paramref name="length"/>
        /// parameters do not hold
        /// </exception>
        public static IOBuffer Wrap(byte[] array, int offset, int length)
        {
            return _allocator.Wrap(array, offset, length);
        }

        /// <summary>
        /// Normalizes the specified capacity of the buffer to power of 2, which is
        /// often helpful for optimal memory usage and performance. If it is greater
        /// than or equal to <see cref="int.MaxValue"/>, it returns
        /// <see cref="int.MaxValue"/>. If it is zero, it returns zero.
        /// </summary>
        /// <param name="requestedCapacity"></param>
        /// <returns></returns>
        public static int NormalizeCapacity(int requestedCapacity)
        {
            if (requestedCapacity < 0)
            {
                return int.MaxValue;
            }

            var newCapacity = HighestOneBit(requestedCapacity);
            newCapacity <<= ((newCapacity < requestedCapacity) ? 1 : 0);
            return newCapacity < 0 ? int.MaxValue : newCapacity;
        }

        private static int HighestOneBit(int i)
        {
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            return i - (i >> 1);
        }

        /// <summary>
        /// 
        /// </summary>
        protected IOBuffer(int mark, int pos, int lim, int cap)
            : base(mark, pos, lim, cap)
        {
        }

        /// <summary>
        /// Gets the the allocator used by this buffer.
        /// </summary>
        public abstract IOBufferAllocator BufferAllocator { get; }

        /// <summary>
        /// Gets or sets the current byte order.
        /// </summary>
        public abstract ByteOrder Order { get; set; }

        /// <inheritdoc/>
        public new virtual int Capacity
        {
            get { return base.Capacity; }
            set { throw new NotSupportedException(); }
        }

        /// <inheritdoc/>
        public new virtual int Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }

        /// <inheritdoc/>
        public new virtual int Limit
        {
            get { return base.Limit; }
            set { base.Limit = value; }
        }

        /// <inheritdoc/>
        public new virtual int Remaining => base.Remaining;

        /// <inheritdoc/>
        public new virtual bool HasRemaining => base.HasRemaining;

        /// <summary>
        /// Turns on or off auto-expanding.
        /// </summary>
        public abstract bool AutoExpand { get; set; }

        /// <summary>
        /// Turns on or off auto-shrinking.
        /// </summary>
        public abstract bool AutoShrink { get; set; }

        /// <summary>
        /// Checks if this buffer is derived from another buffer
        /// via <see cref="Duplicate()"/>, <see cref="Slice()"/> or <see cref="AsReadOnlyBuffer()"/>.
        /// </summary>
        public abstract bool Derived { get; }

        /// <summary>
        /// Gets or sets the minimum capacity.
        /// </summary>
        public abstract int MinimumCapacity { get; set; }

        /// <summary>
        /// Tells whether or not this buffer is backed by an accessible byte array.
        /// </summary>
        public abstract bool HasArray { get; }

        /// <inheritdoc/>
        public new virtual IOBuffer Mark()
        {
            base.Mark();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IOBuffer Reset()
        {
            base.Reset();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IOBuffer Clear()
        {
            base.Clear();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IOBuffer Flip()
        {
            base.Flip();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IOBuffer Rewind()
        {
            base.Rewind();
            return this;
        }

        /// <summary>
        /// Changes the capacity and limit of this buffer so this buffer get the
        /// specified <paramref name="expectedRemaining"/> room from the current position.
        /// </summary>
        /// <param name="expectedRemaining">the expected remaining room</param>
        /// <returns>itself</returns>
        public abstract IOBuffer Expand(int expectedRemaining);

        /// <summary>
        /// Changes the capacity and limit of this buffer so this buffer get the
        /// specified <paramref name="expectedRemaining"/> room from the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">the start position</param>
        /// <param name="expectedRemaining">the expected remaining room</param>
        /// <returns>itself</returns>
        public abstract IOBuffer Expand(int position, int expectedRemaining);

        /// <summary>
        /// Changes the capacity of this buffer so this buffer occupies
        /// as less memory as possible while retaining the position,
        /// limit and the buffer content between the position and limit.
        /// The capacity of the buffer never becomes less than <see cref="MinimumCapacity"/>.
        /// The mark is discarded once the capacity changes.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Shrink();

        /// <summary>
        ///  Clears this buffer and fills its content with zero. The position is
        ///  set to zero, the limit is set to the capacity, and the mark is discarded.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Sweep();

        /// <summary>
        ///  Clears this buffer and fills its content with <paramref name="value"/>. The position is
        ///  set to zero, the limit is set to the capacity, and the mark is discarded.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Sweep(byte value);

        /// <summary>
        /// Fills this buffer with zero.
        /// This method does not change buffer position.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer FillAndReset(int size);

        /// <summary>
        /// Fills this buffer with <paramref name="value"/>.
        /// This method does not change buffer position.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer FillAndReset(byte value, int size);

        /// <summary>
        /// Fills this buffer with zero.
        /// This method moves buffer position forward.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Fill(int size);

        /// <summary>
        /// Fills this buffer with <paramref name="value"/>.
        /// This method moves buffer position forward.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Fill(byte value, int size);

        /// <summary>
        /// Gets hexdump of this buffer.
        /// </summary>
        /// <returns>hexidecimal representation of this buffer</returns>
        public abstract string GetHexDump();

        /// <summary>
        /// Gets hexdump of this buffer with limited length.
        /// </summary>
        /// <param name="lengthLimit">the maximum number of bytes to dump from the current buffer</param>
        /// <returns>hexidecimal representation of this buffer</returns>
        public abstract string GetHexDump(int lengthLimit);

        /// <summary>
        /// Returns true if this buffer contains a data which has a data
        /// length as a prefix and the buffer has remaining data as enough as
        /// specified in the data length field.
        /// <remarks>
        /// Please notes that using this method can allow DoS (Denial of Service)
        /// attack in case the remote peer sends too big data length value.
        /// It is recommended to use <see cref="PrefixedDataAvailable(int, int)"/> instead.
        /// </remarks>
        /// </summary>
        /// <param name="prefixLength">the length of the prefix field (1, 2, or 4)</param>
        /// <returns>true if data available</returns>
        public abstract bool PrefixedDataAvailable(int prefixLength);

        /// <summary>
        /// Returns true if this buffer contains a data which has a data
        /// length as a prefix and the buffer has remaining data as enough as
        /// specified in the data length field.
        /// </summary>
        /// <param name="prefixLength">the length of the prefix field (1, 2, or 4)</param>
        /// <param name="maxDataLength">the allowed maximum of the read data length</param>
        /// <returns>true if data available</returns>
        public abstract bool PrefixedDataAvailable(int prefixLength, int maxDataLength);

        /// <summary>
        /// Returns the first occurence position of the specified byte from the
        /// current position to the current limit.
        /// </summary>
        /// <param name="b">the byte to find</param>
        /// <returns>-1 if the specified byte is not found</returns>
        public abstract int IndexOf(byte b);

        /// <summary>
        /// Reads a string which has a 16-bit length field before the actual encoded string.
        /// This method is a shortcut for <code>GetPrefixedString(2, encoding)</code>.
        /// </summary>
        /// <param name="encoding">the encoding of the string</param>
        /// <returns>the prefixed string</returns>
        public abstract string GetPrefixedString(Encoding encoding);

        /// <summary>
        /// Reads a string which has a length field before the actual encoded string.
        /// </summary>
        /// <param name="prefixLength">the length of the length field (1, 2, or 4)</param>
        /// <param name="encoding">the encoding of the string</param>
        /// <returns>the prefixed string</returns>
        public abstract string GetPrefixedString(int prefixLength, Encoding encoding);

        /// <summary>
        /// Writes the string into this buffer which has a 16-bit length field
        /// before the actual encoded string.
        /// This method is a shortcut for <code>PutPrefixedString(value, 2, encoding)</code>.
        /// </summary>
        /// <param name="value">the string to write</param>
        /// <param name="encoding">the encoding of the string</param>
        public abstract IOBuffer PutPrefixedString(string value, Encoding encoding);

        /// <summary>
        /// Writes the string into this buffer which has a prefixLength field
        /// before the actual encoded string.
        /// </summary>
        /// <param name="value">the string to write</param>
        /// <param name="prefixLength">the length of the length field (1, 2, or 4)</param>
        /// <param name="encoding">the encoding of the string</param>
        public abstract IOBuffer PutPrefixedString(string value, int prefixLength, Encoding encoding);

        /// <summary>
        /// Reads an object from the buffer.
        /// </summary>
        public abstract object GetObject();

        /// <summary>
        /// Writes the specified object to the buffer.
        /// </summary>
        public abstract IOBuffer PutObject(object o);

        /// <summary>
        /// Reads the byte at this buffer's current position, and then increments the position. 
        /// </summary>
        /// <returns>the byte at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">
        /// the buffer's current position is not smaller than its limit
        /// </exception>
        public abstract byte Get();

        /// <summary>
        /// Reads the byte at the given index.
        /// </summary>
        /// <param name="index">the index from which the byte will be read</param>
        /// <returns>the byte at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// the index is negative or not smaller than the buffer's limit
        /// </exception>
        public abstract byte Get(int index);

        /// <summary>
        /// Reads bytes of <paramref name="length"/> into <paramref name="dst"/> array.
        /// </summary>
        /// <param name="dst">the array into which bytes are to be written</param>
        /// <param name="offset">the offset within the array of the first byte to be written</param>
        /// <param name="length">the maximum number of bytes to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// the preconditions on the offset and length parameters do not hold
        /// </exception>
        /// <exception cref="BufferUnderflowException">
        /// there are fewer than length bytes remaining in this buffer
        /// </exception>
        public abstract IOBuffer Get(byte[] dst, int offset, int length);

        /// <summary>
        /// Gets all remaining bytes as an <see cref="ArraySegment&lt;Byte&gt;"/>.
        /// </summary>
        public abstract ArraySegment<byte> GetRemaining();

        /// <summary>
        /// Declares this buffer and all its derived buffers are not used anymore so
        /// that it can be reused by some implementations.
        /// </summary>
        public abstract void Free();

        /// <summary>
        /// Creates a new byte buffer whose content is a
        /// shared subsequence of this buffer's content.
        /// </summary>
        /// <remarks>
        /// The new buffer's position will be zero, its capacity and its limit
        /// will be the number of bytes remaining in this buffer, and its mark
        /// will be undefined.
        /// </remarks>
        /// <returns>the new buffer</returns>
        public abstract IOBuffer Slice();

        public abstract IOBuffer GetSlice(int index, int length);

        public abstract IOBuffer GetSlice(int length);

        /// <summary>
        /// Creates a new byte buffer that shares this buffer's content. 
        /// </summary>
        /// <remarks>
        /// The two buffers' position, limit, and mark values will be independent.
        /// </remarks>
        /// <returns>the new buffer</returns>
        public abstract IOBuffer Duplicate();

        /// <summary>
        /// Creates a new, read-only byte buffer that shares this buffer's content.
        /// </summary>
        /// <returns>the new, read-only buffer</returns>
        public abstract IOBuffer AsReadOnlyBuffer();

        /// <summary>
        /// Forwards the position of this buffer as the specified <paramref name="size"/> bytes.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Skip(int size);

        /// <summary>
        /// Writes the given byte into this buffer at the current position,
        /// and then increments the position.
        /// </summary>
        /// <param name="b">the byte to be written</param>
        /// <returns>itself</returns>
        public abstract IOBuffer Put(byte b);

        /// <summary>
        /// Writes the given byte into this buffer at the given index.
        /// </summary>
        /// <param name="i">the index at which the byte will be written</param>
        /// <param name="b">the byte to be written</param>
        /// <returns>itself</returns>
        public abstract IOBuffer Put(int i, byte b);

        /// <summary>
        /// Writes the given array into this buffer at the current position,
        /// and then increments the position.
        /// </summary>
        /// <param name="src">the array from which bytes are to be read</param>
        /// <param name="offset">the offset within the array of the first byte to be read</param>
        /// <param name="length">the number of bytes to be read from the given array</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// the preconditions on the offset and length parameters do not hold
        /// </exception>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer Put(byte[] src, int offset, int length);

        /// <summary>
        /// Writes the content of the specified <paramref name="src"/> into this buffer.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Put(IOBuffer src);

        /// <summary>
        /// Compacts this buffer.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IOBuffer Compact();

        /// <summary>
        /// Writes the given array into this buffer at the current position,
        /// and then increments the position.
        /// </summary>
        /// <remarks>
        /// This method behaves in exactly the same way as
        /// <example>
        /// Put(src, 0, src.Length)
        /// </example>
        /// </remarks>
        /// <param name="src">the array from which bytes are to be read</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer Put(byte[] src);

        /// <summary>
        /// Writes the content of <paramref name="s"/> into this buffer using <see cref="Encoding.UTF8"/>.
        /// </summary>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutString(string s);

        /// <summary>
        /// Writes the content of <paramref name="s"/> into this buffer using
        /// the specified <see cref="Encoding"/>.
        /// This method doesn't terminate string with <tt>NUL</tt>.
        /// You have to do it by yourself.
        /// </summary>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutString(string s, Encoding encoding);

        /// <summary>
        /// Writes the content of <paramref name="s"/> into this buffer as a
        /// <code>NUL</code>-terminated string using the specified <see cref="Encoding"/>.
        /// </summary>
        /// <remarks>
        /// If the charset name of the encoder is UTF-16, you cannot specify odd
        /// <code>fieldSize</code>, and this method will append two <code>NUL</code>s
        /// as a terminator.
        /// Please note that this method doesn't terminate with <code>NUL</code> if
        /// the input string is longer than <tt>fieldSize</tt>.
        /// </remarks>
        /// <param name="s"></param>
        /// <param name="fieldSize">the maximum number of bytes to write</param>
        /// <param name="encoding"></param>
        public abstract IOBuffer PutString(string s, int fieldSize, Encoding encoding);

        /// <summary>
        /// Reads a NUL-terminated string from this buffer using the specified encoding.
        /// This method reads until the limit of this buffer if no NUL is found.
        /// </summary>
        public abstract string GetString(Encoding encoding);

        /// <summary>
        /// Reads a NUL-terminated string from this buffer using the specified decoder and returns it.
        /// </summary>
        /// <param name="fieldSize">the maximum number of bytes to read</param>
        /// <param name="encoding"></param>
        public abstract string GetString(int fieldSize, Encoding encoding);

        #region

        /// <summary>
        /// Reads the next two bytes at this buffer's current position,
        /// composing them into a char value according to the current byte order,
        /// and then increments the position by two.
        /// </summary>
        /// <returns>the char value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than two bytes remaining in this buffer</exception>
        public abstract char GetChar();

        /// <summary>
        /// Reads two bytes at the given index, composing them into a char value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the char value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        public abstract char GetChar(int index);

        /// <summary>
        /// Writes two bytes containing the given char value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by two.
        /// </summary>
        /// <param name="value">the char value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than two bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutChar(char value);

        /// <summary>
        /// Writes two bytes containing the given char value, in the current byte order,
        /// into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the char value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutChar(int index, char value);

        /// <summary>
        /// Reads the next two bytes at this buffer's current position,
        /// composing them into a short value according to the current byte order,
        /// and then increments the position by two.
        /// </summary>
        /// <returns>the short value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than two bytes remaining in this buffer</exception>
        public abstract short GetInt16();

        /// <summary>
        /// Reads two bytes at the given index, composing them into a short value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the short value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        public abstract short GetInt16(int index);

        /// <summary>
        /// Writes two bytes containing the given short value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by two.
        /// </summary>
        /// <param name="value">the short value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than two bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutInt16(short value);

        /// <summary>
        /// Writes two bytes containing the given short value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the short value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutInt16(int index, short value);

        /// <summary>
        /// Reads the next four bytes at this buffer's current position,
        /// composing them into a int value according to the current byte order,
        /// and then increments the position by four.
        /// </summary>
        /// <returns>the int value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than four bytes remaining in this buffer</exception>
        public abstract int GetInt32();

        /// <summary>
        /// Reads four bytes at the given index, composing them into a int value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the int value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        public abstract int GetInt32(int index);

        /// <summary>
        /// Writes four bytes containing the given int value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by four.
        /// </summary>
        /// <param name="value">the int value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than four bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutInt32(int value);

        /// <summary>
        /// Writes four bytes containing the given int value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the int value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutInt32(int index, int value);

        /// <summary>
        /// Reads the next eight bytes at this buffer's current position,
        /// composing them into a long value according to the current byte order,
        /// and then increments the position by eight.
        /// </summary>
        /// <returns>the long value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than eight bytes remaining in this buffer</exception>
        public abstract long GetInt64();

        /// <summary>
        /// Reads eight bytes at the given index, composing them into a long value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the long value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        public abstract long GetInt64(int index);

        /// <summary>
        /// Writes eight bytes containing the given long value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by eight.
        /// </summary>
        /// <param name="value">the long value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than eight bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutInt64(long value);

        /// <summary>
        /// Writes eight bytes containing the given long value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the long value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutInt64(int index, long value);

        /// <summary>
        /// Reads the next four bytes at this buffer's current position,
        /// composing them into a float value according to the current byte order,
        /// and then increments the position by four.
        /// </summary>
        /// <returns>the float value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than four bytes remaining in this buffer</exception>
        public abstract float GetSingle();

        /// <summary>
        /// Reads four bytes at the given index, composing them into a float value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the float value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        public abstract float GetSingle(int index);

        /// <summary>
        /// Writes four bytes containing the given float value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by four.
        /// </summary>
        /// <param name="value">the float value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than four bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutSingle(float value);

        /// <summary>
        /// Writes four bytes containing the given float value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the float value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutSingle(int index, float value);

        /// <summary>
        /// Reads the next eight bytes at this buffer's current position,
        /// composing them into a double value according to the current byte order,
        /// and then increments the position by eight.
        /// </summary>
        /// <returns>the double value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than eight bytes remaining in this buffer</exception>
        public abstract double GetDouble();

        /// <summary>
        /// Reads eight bytes at the given index, composing them into a double value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the double value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        public abstract double GetDouble(int index);

        /// <summary>
        /// Writes eight bytes containing the given double value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by eight.
        /// </summary>
        /// <param name="value">the double value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than eight bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutDouble(double value);

        /// <summary>
        /// Writes eight bytes containing the given double value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the double value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IOBuffer PutDouble(int index, double value);

        #endregion
    }
}
