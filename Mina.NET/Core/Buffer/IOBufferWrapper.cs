using System;
using System.Text;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A <see cref="IOBuffer"/> that wraps a buffer and proxies any operations to it.
    /// </summary>
    public class IOBufferWrapper : IOBuffer
    {
        private readonly IOBuffer _buffer;

        /// <summary>
        /// </summary>
        protected IOBufferWrapper(IOBuffer buffer)
            : base(-1, 0, 0, 0)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            _buffer = buffer;
        }

        /// <inheritdoc/>
        public override IOBufferAllocator BufferAllocator => _buffer.BufferAllocator;

        /// <inheritdoc/>
        public override ByteOrder Order
        {
            get { return _buffer.Order; }
            set { _buffer.Order = value; }
        }

        /// <inheritdoc/>
        public override int Capacity
        {
            get { return _buffer.Capacity; }
            set { _buffer.Capacity = value; }
        }

        /// <inheritdoc/>
        public override int Position
        {
            get { return _buffer.Position; }
            set { _buffer.Position = value; }
        }

        /// <inheritdoc/>
        public override int Limit
        {
            get { return _buffer.Limit; }
            set { _buffer.Limit = value; }
        }

        /// <inheritdoc/>
        public override int Remaining => _buffer.Remaining;

        /// <inheritdoc/>
        public override bool HasRemaining => _buffer.HasRemaining;

        /// <inheritdoc/>
        public override bool AutoExpand
        {
            get { return _buffer.AutoExpand; }
            set { _buffer.AutoExpand = value; }
        }

        /// <inheritdoc/>
        public override bool AutoShrink
        {
            get { return _buffer.AutoShrink; }
            set { _buffer.AutoShrink = value; }
        }

        /// <inheritdoc/>
        public override bool Derived => _buffer.Derived;

        /// <inheritdoc/>
        public override int MinimumCapacity
        {
            get { return _buffer.MinimumCapacity; }
            set { _buffer.MinimumCapacity = value; }
        }

        /// <inheritdoc/>
        public override bool HasArray => _buffer.HasArray;

        /// <inheritdoc/>
        public override IOBuffer Expand(int expectedRemaining)
        {
            _buffer.Expand(expectedRemaining);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Expand(int pos, int expectedRemaining)
        {
            _buffer.Expand(pos, expectedRemaining);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Shrink()
        {
            _buffer.Shrink();
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Sweep()
        {
            _buffer.Sweep();
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Sweep(byte value)
        {
            _buffer.Sweep(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer FillAndReset(int size)
        {
            _buffer.FillAndReset(size);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer FillAndReset(byte value, int size)
        {
            _buffer.FillAndReset(value, size);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Fill(int size)
        {
            _buffer.Fill(size);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Fill(byte value, int size)
        {
            _buffer.Fill(value, size);
            return this;
        }

        /// <inheritdoc/>
        public override string GetHexDump()
        {
            return _buffer.GetHexDump();
        }

        /// <inheritdoc/>
        public override string GetHexDump(int lengthLimit)
        {
            return _buffer.GetHexDump(lengthLimit);
        }

        /// <inheritdoc/>
        public override bool PrefixedDataAvailable(int prefixLength)
        {
            return _buffer.PrefixedDataAvailable(prefixLength);
        }

        /// <inheritdoc/>
        public override bool PrefixedDataAvailable(int prefixLength, int maxDataLength)
        {
            return _buffer.PrefixedDataAvailable(prefixLength, maxDataLength);
        }

        /// <inheritdoc/>
        public override int IndexOf(byte b)
        {
            return _buffer.IndexOf(b);
        }

        /// <inheritdoc/>
        public override string GetPrefixedString(Encoding encoding)
        {
            return _buffer.GetPrefixedString(encoding);
        }

        /// <inheritdoc/>
        public override string GetPrefixedString(int prefixLength, Encoding encoding)
        {
            return _buffer.GetPrefixedString(prefixLength, encoding);
        }

        /// <inheritdoc/>
        public override IOBuffer PutPrefixedString(string value, Encoding encoding)
        {
            return _buffer.PutPrefixedString(value, encoding);
        }

        /// <inheritdoc/>
        public override IOBuffer PutPrefixedString(string value, int prefixLength, Encoding encoding)
        {
            _buffer.PutPrefixedString(value, prefixLength, encoding);
            return this;
        }

        /// <inheritdoc/>
        public override object GetObject()
        {
            return _buffer.GetObject();
        }

        /// <inheritdoc/>
        public override IOBuffer PutObject(object o)
        {
            _buffer.PutObject(o);
            return this;
        }

        /// <inheritdoc/>
        public override byte Get()
        {
            return _buffer.Get();
        }

        /// <inheritdoc/>
        public override byte Get(int index)
        {
            return _buffer.Get(index);
        }

        /// <inheritdoc/>
        public override IOBuffer Get(byte[] dst, int offset, int length)
        {
            _buffer.Get(dst, offset, length);
            return this;
        }

        /// <inheritdoc/>
        public override ArraySegment<byte> GetRemaining()
        {
            return _buffer.GetRemaining();
        }

        /// <inheritdoc/>
        public override void Free()
        {
            _buffer.Free();
        }

        /// <inheritdoc/>
        public override IOBuffer Slice()
        {
            return _buffer.Slice();
        }

        /// <inheritdoc/>
        public override IOBuffer GetSlice(int index, int length)
        {
            return _buffer.GetSlice(index, length);
        }

        /// <inheritdoc/>
        public override IOBuffer GetSlice(int length)
        {
            return _buffer.GetSlice(length);
        }

        /// <inheritdoc/>
        public override IOBuffer Duplicate()
        {
            return _buffer.Duplicate();
        }

        /// <inheritdoc/>
        public override IOBuffer AsReadOnlyBuffer()
        {
            return _buffer.AsReadOnlyBuffer();
        }

        /// <inheritdoc/>
        public override IOBuffer Skip(int size)
        {
            _buffer.Skip(size);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(byte b)
        {
            _buffer.Put(b);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(int i, byte b)
        {
            _buffer.Put(i, b);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(byte[] src, int offset, int length)
        {
            _buffer.Put(src, offset, length);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(IOBuffer src)
        {
            _buffer.Put(src);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Compact()
        {
            _buffer.Compact();
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(byte[] src)
        {
            _buffer.Put(src);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutString(string s)
        {
            _buffer.PutString(s);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutString(string s, Encoding encoding)
        {
            _buffer.PutString(s, encoding);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutString(string s, int fieldSize, Encoding encoding)
        {
            return _buffer.PutString(s, fieldSize, encoding);
        }

        /// <inheritdoc/>
        public override string GetString(Encoding encoding)
        {
            return _buffer.GetString(encoding);
        }

        /// <inheritdoc/>
        public override string GetString(int fieldSize, Encoding encoding)
        {
            return _buffer.GetString(fieldSize, encoding);
        }

        /// <inheritdoc/>
        public override char GetChar()
        {
            return _buffer.GetChar();
        }

        /// <inheritdoc/>
        public override char GetChar(int index)
        {
            return _buffer.GetChar(index);
        }

        /// <inheritdoc/>
        public override IOBuffer PutChar(char value)
        {
            _buffer.PutChar(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutChar(int index, char value)
        {
            _buffer.PutChar(index, value);
            return this;
        }

        /// <inheritdoc/>
        public override short GetInt16()
        {
            return _buffer.GetInt16();
        }

        /// <inheritdoc/>
        public override short GetInt16(int index)
        {
            return _buffer.GetInt16(index);
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt16(short value)
        {
            _buffer.PutInt16(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt16(int index, short value)
        {
            _buffer.PutInt16(index, value);
            return this;
        }

        /// <inheritdoc/>
        public override int GetInt32()
        {
            return _buffer.GetInt32();
        }

        /// <inheritdoc/>
        public override int GetInt32(int index)
        {
            return _buffer.GetInt32(index);
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt32(int value)
        {
            _buffer.PutInt32(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt32(int index, int value)
        {
            _buffer.PutInt32(index, value);
            return this;
        }

        /// <inheritdoc/>
        public override long GetInt64()
        {
            return _buffer.GetInt64();
        }

        /// <inheritdoc/>
        public override long GetInt64(int index)
        {
            return _buffer.GetInt64(index);
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt64(long value)
        {
            _buffer.PutInt64(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt64(int index, long value)
        {
            _buffer.PutInt64(index, value);
            return this;
        }

        /// <inheritdoc/>
        public override float GetSingle()
        {
            return _buffer.GetSingle();
        }

        /// <inheritdoc/>
        public override float GetSingle(int index)
        {
            return _buffer.GetSingle(index);
        }

        /// <inheritdoc/>
        public override IOBuffer PutSingle(float value)
        {
            _buffer.PutSingle(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutSingle(int index, float value)
        {
            _buffer.PutSingle(index, value);
            return this;
        }

        /// <inheritdoc/>
        public override double GetDouble()
        {
            return _buffer.GetDouble();
        }

        /// <inheritdoc/>
        public override double GetDouble(int index)
        {
            return _buffer.GetDouble(index);
        }

        /// <inheritdoc/>
        public override IOBuffer PutDouble(double value)
        {
            _buffer.PutDouble(value);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutDouble(int index, double value)
        {
            _buffer.PutDouble(index, value);
            return this;
        }

        /// <inheritdoc/>
        public override bool ReadOnly => _buffer.ReadOnly;
    }
}
