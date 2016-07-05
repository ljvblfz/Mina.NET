using System;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A base implementation of <see cref="IOBuffer"/>.
    /// </summary>
    public abstract class AbstractIOBuffer : IOBuffer
    {
        private IOBufferAllocator _allocator;
        private bool _autoExpand;
        private bool _autoShrink;
        private int _minimumCapacity;

        /// <summary>
        /// 
        /// </summary>
        protected AbstractIOBuffer(IOBufferAllocator allocator, int mark, int pos, int lim, int cap)
            : base(mark, pos, lim, cap)
        {
            _allocator = allocator;
            RecapacityAllowed = true;
            Derived = false;
            _minimumCapacity = cap;
        }

        /// <summary>
        /// 
        /// </summary>
        protected AbstractIOBuffer(AbstractIOBuffer parent, int mark, int pos, int lim, int cap)
            : base(mark, pos, lim, cap)
        {
            _allocator = parent._allocator;
            RecapacityAllowed = false;
            Derived = true;
            _minimumCapacity = parent._minimumCapacity;
        }

        /// <inheritdoc/>
        public override ByteOrder Order { get; set; } = ByteOrder.BigEndian;

        /// <inheritdoc/>
        public override int MinimumCapacity
        {
            get { return _minimumCapacity; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("MinimumCapacity");
                _minimumCapacity = value;
            }
        }

        /// <inheritdoc/>
        public override IOBufferAllocator BufferAllocator => _allocator;

        /// <inheritdoc/>
        public override int Position
        {
            get { return base.Position; }
            set
            {
                base.Position = value;
                AutoExpand0(value, 0);
            }
        }

        /// <inheritdoc/>
        public override int Limit
        {
            get { return base.Limit; }
            set
            {
                base.Limit = value;
                AutoExpand0(value, 0);
            }
        }

        /// <inheritdoc/>
        public override bool AutoExpand
        {
            get { return _autoExpand && RecapacityAllowed; }
            set
            {
                if (!RecapacityAllowed)
                    throw new InvalidOperationException("Derived buffers and their parent can't be expanded.");
                _autoExpand = value;
            }
        }

        /// <inheritdoc/>
        public override bool AutoShrink
        {
            get { return _autoShrink && RecapacityAllowed; }
            set
            {
                if (!RecapacityAllowed)
                    throw new InvalidOperationException("Derived buffers and their parent can't be shrinked.");
                _autoShrink = value;
            }
        }

        /// <inheritdoc/>
        public override bool Derived { get; }

        /// <inheritdoc/>
        public override IOBuffer Expand(int expectedRemaining)
        {
            return Expand(Position, expectedRemaining, false);
        }

        /// <inheritdoc/>
        public override IOBuffer Expand(int position, int expectedRemaining)
        {
            return Expand(position, expectedRemaining, false);
        }

        /// <inheritdoc/>
        public override IOBuffer Sweep()
        {
            Clear();
            return FillAndReset(Remaining);
        }

        /// <inheritdoc/>
        public override IOBuffer Sweep(byte value)
        {
            Clear();
            return FillAndReset(value, Remaining);
        }

        /// <inheritdoc/>
        public override IOBuffer FillAndReset(int size)
        {
            AutoExpand0(size);
            var pos = Position;
            try
            {
                Fill(size);
            }
            finally
            {
                Position = pos;
            }
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer FillAndReset(byte value, int size)
        {
            AutoExpand0(size);
            var pos = Position;
            try
            {
                Fill(value, size);
            }
            finally
            {
                Position = pos;
            }
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Fill(int size)
        {
            AutoExpand0(size);
            var q = size >> 3;
            var r = size & 7;

            for (var i = q; i > 0; i--)
            {
                PutInt64(0L);
            }

            q = r >> 2;
            r = r & 3;

            if (q > 0)
            {
                PutInt32(0);
            }

            q = r >> 1;
            r = r & 1;

            if (q > 0)
            {
                PutInt16(0);
            }

            if (r > 0)
            {
                Put(0);
            }

            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Fill(byte value, int size)
        {
            AutoExpand0(size);
            var q = size >> 3;
            var r = size & 7;

            if (q > 0)
            {
                var intValue = value | value << 8 | value << 16 | value << 24;
                long longValue = intValue;
                longValue <<= 32;
                longValue |= (uint)intValue;

                for (var i = q; i > 0; i--)
                {
                    PutInt64(longValue);
                }
            }

            q = r >> 2;
            r = r & 3;

            if (q > 0)
            {
                var intValue = value | value << 8 | value << 16 | value << 24;
                PutInt32(intValue);
            }

            q = r >> 1;
            r = r & 1;

            if (q > 0)
            {
                var shortValue = (short)(value | value << 8);
                PutInt16(shortValue);
            }

            if (r > 0)
            {
                Put(value);
            }

            return this;
        }

        /// <inheritdoc/>
        public override string GetHexDump()
        {
            return GetHexDump(int.MaxValue);
        }

        /// <inheritdoc/>
        public override string GetHexDump(int lengthLimit)
        {
            return IOBufferHexDumper.GetHexdump(this, lengthLimit);
        }

        /// <inheritdoc/>
        public override bool PrefixedDataAvailable(int prefixLength)
        {
            return PrefixedDataAvailable(prefixLength, int.MaxValue);
        }

        /// <inheritdoc/>
        public override bool PrefixedDataAvailable(int prefixLength, int maxDataLength)
        {
            if (Remaining < prefixLength)
                return false;

            int dataLength;
            switch (prefixLength)
            {
                case 1:
                    dataLength = Get(Position) & 0xff;
                    break;
                case 2:
                    dataLength = GetInt16(Position) & 0xffff;
                    break;
                case 4:
                    dataLength = GetInt32(Position);
                    break;
                default:
                    throw new ArgumentException("Expect prefixLength (1,2,4) but " + prefixLength);
            }

            if (dataLength < 0 || dataLength > maxDataLength)
            {
                throw new BufferDataException("dataLength: " + dataLength);
            }

            return Remaining - prefixLength >= dataLength;
        }

        /// <inheritdoc/>
        public override int IndexOf(byte b)
        {
            if (HasArray)
            {
                var array = GetRemaining();
                for (var i = 0; i < array.Count; i++)
                {
                    if (array.Array[i + array.Offset] == b)
                    {
                        return i + Position;
                    }
                }
            }
            else
            {
                var beginPos = Position;
                var limit = Limit;

                for (var i = beginPos; i < limit; i++)
                {
                    if (Get(i) == b)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <inheritdoc/>
        public override string GetPrefixedString(Encoding encoding)
        {
            return GetPrefixedString(2, encoding);
        }

        /// <inheritdoc/>
        public override string GetPrefixedString(int prefixLength, Encoding encoding)
        {
            if (!PrefixedDataAvailable(prefixLength))
                throw new BufferUnderflowException();

            var dataLength = 0;
            switch (prefixLength)
            {
                case 1:
                    dataLength = Get() & 0xff;
                    break;
                case 2:
                    dataLength = GetInt16() & 0xffff;
                    break;
                case 4:
                    dataLength = GetInt32();
                    break;
            }

            if (dataLength == 0)
                return string.Empty;

            var bytes = new byte[dataLength];
            Get(bytes, 0, dataLength);
            return encoding.GetString(bytes, 0, dataLength);
        }

        /// <inheritdoc/>
        public override IOBuffer PutPrefixedString(string value, Encoding encoding)
        {
            return PutPrefixedString(value, 2, encoding);
        }

        /// <inheritdoc/>
        public override IOBuffer PutPrefixedString(string value, int prefixLength, Encoding encoding)
        {
            int maxLength;
            switch (prefixLength)
            {
                case 1:
                    maxLength = 255;
                    break;
                case 2:
                    maxLength = 65535;
                    break;
                case 4:
                    maxLength = int.MaxValue;
                    break;
                default:
                    throw new ArgumentException("prefixLength: " + prefixLength);
            }

            if (value.Length > maxLength)
                throw new ArgumentException("The specified string is too long.");

            if (value.Length == 0)
            {
                switch (prefixLength)
                {
                    case 1:
                        Put(0);
                        break;
                    case 2:
                        PutInt16(0);
                        break;
                    case 4:
                        PutInt32(0);
                        break;
                }
                return this;
            }

            var bytes = encoding.GetBytes(value);
            switch (prefixLength)
            {
                case 1:
                    Put((byte)bytes.Length);
                    break;
                case 2:
                    PutInt16((short)bytes.Length);
                    break;
                case 4:
                    PutInt32(bytes.Length);
                    break;
            }
            Put(bytes);
            return this;
        }

        /// <inheritdoc/>
        public override object GetObject()
        {
            IFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(new IOBufferStream(this));
        }

        /// <inheritdoc/>
        public override IOBuffer PutObject(object o)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(new IOBufferStream(this), o);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Slice()
        {
            RecapacityAllowed = false;
            return Slice0();
        }

        /// <inheritdoc/>
        public override IOBuffer GetSlice(int index, int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var pos = Position;
            var limit = Limit;

            if (index > limit)
                throw new ArgumentOutOfRangeException(nameof(index));

            var endIndex = index + length;

            if (endIndex > limit)
                throw new IndexOutOfRangeException("index + length (" + endIndex + ") is greater "
                    + "than limit (" + limit + ").");

            Clear();
            Limit = endIndex;
            Position = index;

            var slice = Slice();
            Limit = limit;
            Position = pos;

            return slice;
        }

        /// <inheritdoc/>
        public override IOBuffer GetSlice(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var pos = Position;
            var limit = Limit;
            var nextPos = pos + length;
            if (limit < nextPos)
                throw new IndexOutOfRangeException("position + length (" + nextPos + ") is greater "
                    + "than limit (" + limit + ").");

            Limit = pos + length;
            var slice = Slice();
            Position = nextPos;
            Limit = limit;
            return slice;
        }

        /// <inheritdoc/>
        public override IOBuffer Duplicate()
        {
            RecapacityAllowed = false;
            return Duplicate0();
        }

        /// <inheritdoc/>
        public override IOBuffer AsReadOnlyBuffer()
        {
            RecapacityAllowed = false;
            return AsReadOnlyBuffer0();
        }

        /// <inheritdoc/>
        public override IOBuffer Skip(int size)
        {
            AutoExpand0(size);
            Position = Position + size;
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(byte b)
        {
            AutoExpand0(1);
            PutInternal(Offset(NextPutIndex()), b);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(int i, byte b)
        {
            AutoExpand0(i, 1);
            PutInternal(Offset(CheckIndex(i)), b);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(byte[] src, int offset, int length)
        {
            CheckBounds(offset, length, src.Length);
            AutoExpand0(length);

            if (length > Remaining)
                throw new OverflowException();

            PutInternal(src, offset, length);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(IOBuffer src)
        {
            if (ReferenceEquals(src, this))
                throw new ArgumentException("Cannot put myself", nameof(src));

            AutoExpand0(src.Remaining);
            PutInternal(src);

            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer Put(byte[] src)
        {
            return Put(src, 0, src.Length);
        }

        /// <inheritdoc/>
        public override IOBuffer PutString(string s)
        {
            return PutString(s, Encoding.UTF8);
        }

        /// <inheritdoc/>
        public override IOBuffer PutString(string s, Encoding encoding)
        {
            if (string.IsNullOrEmpty(s))
                return this;
            return Put(encoding.GetBytes(s));
        }

        /// <inheritdoc/>
        public override IOBuffer PutString(string s, int fieldSize, Encoding encoding)
        {
            if (fieldSize < 0)
                throw new ArgumentException("fieldSize cannot be negative: " + fieldSize, nameof(fieldSize));
            if (fieldSize == 0)
                return this;

            AutoExpand0(fieldSize);
            var utf16 = encoding.WebName.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase);
         
            if (utf16 && (fieldSize & 1) != 0)
                throw new ArgumentException("fieldSize is not even.", nameof(fieldSize));

            var oldLimit = Limit;
            var end = Position + fieldSize;

            if (oldLimit < end)
                throw new OverflowException();

            if (!string.IsNullOrEmpty(s))
            {
                var bytes = encoding.GetBytes(s);
                Put(bytes, 0, fieldSize < bytes.Length ? fieldSize : bytes.Length);
            }

            if (Position < end)
            {
                if (utf16)
                {
                    Put(0x00);
                    Put(0x00);
                }
                else
                {
                    Put(0x00);
                }
            }

            Position = end;
            return this;
        }

        /// <inheritdoc/>
        public override string GetString(Encoding encoding)
        {
            if (!HasRemaining)
                return string.Empty;

            var utf16 = encoding.WebName.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase);

            var oldPos = Position;
            var oldLimit = Limit;
            var end = -1;
            int newPos;

            if (utf16)
            {
                var i = oldPos;
                while (true)
                {
                    var wasZero = Get(i) == 0;
                    i++;

                    if (i >= oldLimit)
                        break;

                    if (Get(i) != 0)
                    {
                        i++;
                        if (i >= oldLimit)
                            break;
                        continue;
                    }

                    if (wasZero)
                    {
                        end = i - 1;
                        break;
                    }
                }

                if (end < 0)
                    newPos = end = oldPos + (int)(oldLimit - oldPos & 0xFFFFFFFE);
                else if (end + 2 <= oldLimit)
                    newPos = end + 2;
                else
                    newPos = end;
            }
            else
            {
                end = IndexOf(0x00);
                if (end < 0)
                    newPos = end = oldLimit;
                else
                    newPos = end + 1;
            }

            if (oldPos == end)
            {
                Position = newPos;
                return string.Empty;
            }

            Limit = end;

            string result;
            if (HasArray)
            {
                var array = GetRemaining();
                result = encoding.GetString(array.Array, array.Offset, array.Count);
            }
            else
            {
                var bytes = new byte[Remaining];
                Get(bytes, 0, bytes.Length);
                result = encoding.GetString(bytes, 0, bytes.Length);
            }

            Limit = oldLimit;
            Position = newPos;
            return result;
        }

        /// <inheritdoc/>
        public override string GetString(int fieldSize, Encoding encoding)
        {
            if (fieldSize < 0)
                throw new ArgumentException("fieldSize cannot be negative: " + fieldSize, nameof(fieldSize));
            if (fieldSize == 0 || !HasRemaining)
                return string.Empty;

            var utf16 = encoding.WebName.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase);

            if (utf16 && (fieldSize & 1) != 0)
                throw new ArgumentException("fieldSize is not even.", nameof(fieldSize));

            var oldPos = Position;
            var oldLimit = Limit;
            var end = oldPos + fieldSize;

            if (oldLimit < end)
                throw new BufferUnderflowException();

            int i;

            if (utf16)
            {
                for (i = oldPos; i < end; i += 2)
                {
                    if (Get(i) == 0 && Get(i + 1) == 0)
                        break;
                }

                Limit = i;
            }
            else
            {
                for (i = oldPos; i < end; i++)
                {
                    if (Get(i) == 0)
                        break;
                }

                Limit = i;
            }

            if (!HasRemaining)
            {
                Limit = oldLimit;
                Position = end;
                return string.Empty;
            }

            string result;
            if (HasArray)
            {
                var array = GetRemaining();
                result = encoding.GetString(array.Array, array.Offset, array.Count);
            }
            else
            {
                var bytes = new byte[Remaining];
                Get(bytes, 0, bytes.Length);
                result = encoding.GetString(bytes, 0, bytes.Length);
            }

            Limit = oldLimit;
            Position = end;
            return result;
        }
        
        /// <summary>
        /// Indicating whether recapacity is allowed.
        /// </summary>
        protected bool RecapacityAllowed { get; private set; } = true;

        /// <summary>
        /// Gets the actual position in internal buffer of the given index.
        /// </summary>
        protected virtual int Offset(int i)
        {
            return i;
        }

        /// <summary>
        /// Writes an <see cref="IOBuffer"/>. Override this method for better implementation.
        /// </summary>
        protected virtual void PutInternal(IOBuffer src)
        {
            var n = src.Remaining;
            if (n > Remaining)
                throw new OverflowException();
            for (var i = 0; i < n; i++)
            {
                Put(src.Get());
            }
        }

        /// <summary>
        /// Writes an array of bytes. Override this method for better implementation.
        /// </summary>
        protected virtual void PutInternal(byte[] src, int offset, int length)
        {
            var end = offset + length;
            for (var i = offset; i < end; i++)
                Put(src[i]);
        }

        /// <summary>
        /// Gets the byte at the given index in internal buffer.
        /// </summary>
        /// <param name="i">the index from which the byte will be read</param>
        /// <returns>the byte at the given index</returns>
        protected abstract byte GetInternal(int i);

        /// <summary>
        /// Pus the given byte into internal buffer at the given index.
        /// </summary>
        /// <param name="i">the index at which the byte will be written</param>
        /// <param name="b">the byte to be written</param>
        protected abstract void PutInternal(int i, byte b);

        /// <summary>
        /// <see cref="Slice()"/>
        /// </summary>
        protected abstract IOBuffer Slice0();
        /// <summary>
        /// <see cref="Duplicate()"/>
        /// </summary>
        protected abstract IOBuffer Duplicate0();
        /// <summary>
        /// <see cref="AsReadOnlyBuffer()"/>
        /// </summary>
        protected abstract IOBuffer AsReadOnlyBuffer0();

        private IOBuffer Expand(int expectedRemaining, bool autoExpand)
        {
            return Expand(Position, expectedRemaining, autoExpand);
        }

        private IOBuffer Expand(int pos, int expectedRemaining, bool autoExpand)
        {
            if (!RecapacityAllowed)
                throw new InvalidOperationException("Derived buffers and their parent can't be expanded.");

            var end = pos + expectedRemaining;
            int newCapacity;
            if (autoExpand)
                newCapacity = NormalizeCapacity(end);
            else
                newCapacity = end;

            if (newCapacity > Capacity)
            {
                // The buffer needs expansion.
                Capacity = newCapacity;
            }

            if (end > Limit)
            {
                // We call base.Limit directly to prevent StackOverflowError
                base.Limit = end;
            }

            return this;
        }

        private void AutoExpand0(int expectedRemaining)
        {
            if (AutoExpand)
                Expand(expectedRemaining, true);
        }

        private void AutoExpand0(int pos, int expectedRemaining)
        {
            if (AutoExpand)
                Expand(pos, expectedRemaining, true);
        }

        #region

        /// <inheritdoc/>
        public override char GetChar()
        {
            return Bits.GetChar(this, Offset(NextGetIndex(2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override char GetChar(int index)
        {
            return Bits.GetChar(this, Offset(CheckIndex(index, 2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IOBuffer PutChar(char value)
        {
            AutoExpand0(2);
            Bits.PutChar(this, Offset(NextPutIndex(2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutChar(int index, char value)
        {
            AutoExpand0(index, 2);
            Bits.PutChar(this, Offset(CheckIndex(index, 2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override short GetInt16()
        {
            return Bits.GetShort(this, Offset(NextGetIndex(2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override short GetInt16(int index)
        {
            return Bits.GetShort(this, Offset(CheckIndex(index, 2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt16(short value)
        {
            AutoExpand0(2);
            Bits.PutShort(this, Offset(NextPutIndex(2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt16(int index, short value)
        {
            AutoExpand0(index, 2);
            Bits.PutShort(this, Offset(CheckIndex(index, 2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override int GetInt32()
        {
            return Bits.GetInt(this, Offset(NextGetIndex(4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override int GetInt32(int index)
        {
            return Bits.GetInt(this, Offset(CheckIndex(index, 4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt32(int value)
        {
            AutoExpand0(4);
            Bits.PutInt(this, Offset(NextPutIndex(4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt32(int index, int value)
        {
            AutoExpand0(index, 4);
            Bits.PutInt(this, Offset(CheckIndex(index, 4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override long GetInt64()
        {
            return Bits.GetLong(this, Offset(NextGetIndex(8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override long GetInt64(int index)
        {
            return Bits.GetLong(this, Offset(CheckIndex(index, 8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt64(long value)
        {
            AutoExpand0(8);
            Bits.PutLong(this, Offset(NextPutIndex(8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutInt64(int index, long value)
        {
            AutoExpand0(index, 8);
            Bits.PutLong(this, Offset(CheckIndex(index, 8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override float GetSingle()
        {
            return Bits.GetFloat(this, Offset(NextGetIndex(4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override float GetSingle(int index)
        {
            return Bits.GetFloat(this, Offset(CheckIndex(index, 4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IOBuffer PutSingle(float value)
        {
            AutoExpand0(4);
            Bits.PutFloat(this, Offset(NextPutIndex(4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutSingle(int index, float value)
        {
            AutoExpand0(index, 4);
            Bits.PutFloat(this, Offset(CheckIndex(index, 4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override double GetDouble()
        {
            return Bits.GetDouble(this, Offset(NextGetIndex(8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override double GetDouble(int index)
        {
            return Bits.GetDouble(this, Offset(CheckIndex(index, 8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IOBuffer PutDouble(double value)
        {
            AutoExpand0(8);
            Bits.PutDouble(this, Offset(NextPutIndex(8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IOBuffer PutDouble(int index, double value)
        {
            AutoExpand0(index, 8);
            Bits.PutDouble(this, Offset(CheckIndex(index, 8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        static class Bits
        {
            public static short Swap(short x)
            {
                return (short)((x << 8) |
                           ((x >> 8) & 0xff));
            }

            public static char Swap(char x)
            {
                return (char)((x << 8) |
                          ((x >> 8) & 0xff));
            }

            public static int Swap(int x)
            {
                return (Swap((short)x) << 16) |
                         (Swap((short)(x >> 16)) & 0xffff);
            }

            public static long Swap(long x)
            {
                return ((long)Swap((int)(x)) << 32) |
                          ((long)Swap((int)(x >> 32)) & 0xffffffffL);
            }

            // -- get/put char --

            private static char MakeChar(byte b1, byte b0)
            {
                return (char)((b1 << 8) | (b0 & 0xff));
            }

            public static char GetCharL(AbstractIOBuffer bb, int bi)
            {
                return MakeChar(bb.GetInternal(bi + 1),
                        bb.GetInternal(bi + 0));
            }

            public static char GetCharB(AbstractIOBuffer bb, int bi)
            {
                return MakeChar(bb.GetInternal(bi + 0),
                        bb.GetInternal(bi + 1));
            }

            public static char GetChar(AbstractIOBuffer bb, int bi, bool bigEndian)
            {
                return (bigEndian ? GetCharB(bb, bi) : GetCharL(bb, bi));
            }

            private static byte Char1(char x) { return (byte)(x >> 8); }
            private static byte Char0(char x) { return (byte)(x >> 0); }

            public static void PutCharL(AbstractIOBuffer bb, int bi, char x)
            {
                bb.PutInternal(bi + 0, Char0(x));
                bb.PutInternal(bi + 1, Char1(x));
            }

            public static void PutCharB(AbstractIOBuffer bb, int bi, char x)
            {
                bb.PutInternal(bi + 0, Char1(x));
                bb.PutInternal(bi + 1, Char0(x));
            }

            public static void PutChar(AbstractIOBuffer bb, int bi, char x, bool bigEndian)
            {
                if (bigEndian)
                    PutCharB(bb, bi, x);
                else
                    PutCharL(bb, bi, x);
            }

            // -- get/put short --

            private static short MakeShort(byte b1, byte b0)
            {
                return (short)((b1 << 8) | (b0 & 0xff));
            }

            public static short GetShortL(AbstractIOBuffer bb, int bi)
            {
                return MakeShort(bb.GetInternal(bi + 1),
                         bb.GetInternal(bi + 0));
            }

            public static short GetShortB(AbstractIOBuffer bb, int bi)
            {
                return MakeShort(bb.GetInternal(bi + 0),
                         bb.GetInternal(bi + 1));
            }

            public static short GetShort(AbstractIOBuffer bb, int bi, bool bigEndian)
            {
                return (bigEndian ? GetShortB(bb, bi) : GetShortL(bb, bi));
            }

            private static byte Short1(short x) { return (byte)(x >> 8); }
            private static byte Short0(short x) { return (byte)(x >> 0); }

            public static void PutShortL(AbstractIOBuffer bb, int bi, short x)
            {
                bb.PutInternal(bi + 0, Short0(x));
                bb.PutInternal(bi + 1, Short1(x));
            }

            public static void PutShortB(AbstractIOBuffer bb, int bi, short x)
            {
                bb.PutInternal(bi + 0, Short1(x));
                bb.PutInternal(bi + 1, Short0(x));
            }

            public static void PutShort(AbstractIOBuffer bb, int bi, short x, bool bigEndian)
            {
                if (bigEndian)
                    PutShortB(bb, bi, x);
                else
                    PutShortL(bb, bi, x);
            }

            // -- get/put int --

            private static int MakeInt(byte b3, byte b2, byte b1, byte b0)
            {
                return (((b3 & 0xff) << 24) |
                          ((b2 & 0xff) << 16) |
                          ((b1 & 0xff) << 8) |
                          ((b0 & 0xff) << 0));
            }

            public static int GetIntL(AbstractIOBuffer bb, int bi)
            {
                return MakeInt(bb.GetInternal(bi + 3),
                           bb.GetInternal(bi + 2),
                           bb.GetInternal(bi + 1),
                           bb.GetInternal(bi + 0));
            }

            public static int GetIntB(AbstractIOBuffer bb, int bi)
            {
                return MakeInt(bb.GetInternal(bi + 0),
                           bb.GetInternal(bi + 1),
                           bb.GetInternal(bi + 2),
                           bb.GetInternal(bi + 3));
            }

            public static int GetInt(AbstractIOBuffer bb, int bi, bool bigEndian)
            {
                return (bigEndian ? GetIntB(bb, bi) : GetIntL(bb, bi));
            }

            private static byte Int3(int x) { return (byte)(x >> 24); }
            private static byte Int2(int x) { return (byte)(x >> 16); }
            private static byte Int1(int x) { return (byte)(x >> 8); }
            private static byte Int0(int x) { return (byte)(x >> 0); }

            public static void PutIntL(AbstractIOBuffer bb, int bi, int x)
            {
                bb.PutInternal(bi + 3, Int3(x));
                bb.PutInternal(bi + 2, Int2(x));
                bb.PutInternal(bi + 1, Int1(x));
                bb.PutInternal(bi + 0, Int0(x));
            }

            public static void PutIntB(AbstractIOBuffer bb, int bi, int x)
            {
                bb.PutInternal(bi + 0, Int3(x));
                bb.PutInternal(bi + 1, Int2(x));
                bb.PutInternal(bi + 2, Int1(x));
                bb.PutInternal(bi + 3, Int0(x));
            }

            public static void PutInt(AbstractIOBuffer bb, int bi, int x, bool bigEndian)
            {
                if (bigEndian)
                    PutIntB(bb, bi, x);
                else
                    PutIntL(bb, bi, x);
            }

            // -- get/put long --

            private static long MakeLong(byte b7, byte b6, byte b5, byte b4,
                         byte b3, byte b2, byte b1, byte b0)
            {
                return ((((long)b7 & 0xff) << 56) |
                    (((long)b6 & 0xff) << 48) |
                    (((long)b5 & 0xff) << 40) |
                    (((long)b4 & 0xff) << 32) |
                    (((long)b3 & 0xff) << 24) |
                    (((long)b2 & 0xff) << 16) |
                    (((long)b1 & 0xff) << 8) |
                    (((long)b0 & 0xff) << 0));
            }

            public static long GetLongL(AbstractIOBuffer bb, int bi)
            {
                return MakeLong(bb.GetInternal(bi + 7),
                        bb.GetInternal(bi + 6),
                        bb.GetInternal(bi + 5),
                        bb.GetInternal(bi + 4),
                        bb.GetInternal(bi + 3),
                        bb.GetInternal(bi + 2),
                        bb.GetInternal(bi + 1),
                        bb.GetInternal(bi + 0));
            }

            public static long GetLongB(AbstractIOBuffer bb, int bi)
            {
                return MakeLong(bb.GetInternal(bi + 0),
                        bb.GetInternal(bi + 1),
                        bb.GetInternal(bi + 2),
                        bb.GetInternal(bi + 3),
                        bb.GetInternal(bi + 4),
                        bb.GetInternal(bi + 5),
                        bb.GetInternal(bi + 6),
                        bb.GetInternal(bi + 7));
            }

            public static long GetLong(AbstractIOBuffer bb, int bi, bool bigEndian)
            {
                return (bigEndian ? GetLongB(bb, bi) : GetLongL(bb, bi));
            }

            private static byte Long7(long x) { return (byte)(x >> 56); }
            private static byte Long6(long x) { return (byte)(x >> 48); }
            private static byte Long5(long x) { return (byte)(x >> 40); }
            private static byte Long4(long x) { return (byte)(x >> 32); }
            private static byte Long3(long x) { return (byte)(x >> 24); }
            private static byte Long2(long x) { return (byte)(x >> 16); }
            private static byte Long1(long x) { return (byte)(x >> 8); }
            private static byte Long0(long x) { return (byte)(x >> 0); }

            public static void PutLongL(AbstractIOBuffer bb, int bi, long x)
            {
                bb.PutInternal(bi + 7, Long7(x));
                bb.PutInternal(bi + 6, Long6(x));
                bb.PutInternal(bi + 5, Long5(x));
                bb.PutInternal(bi + 4, Long4(x));
                bb.PutInternal(bi + 3, Long3(x));
                bb.PutInternal(bi + 2, Long2(x));
                bb.PutInternal(bi + 1, Long1(x));
                bb.PutInternal(bi + 0, Long0(x));
            }

            public static void PutLongB(AbstractIOBuffer bb, int bi, long x)
            {
                bb.PutInternal(bi + 0, Long7(x));
                bb.PutInternal(bi + 1, Long6(x));
                bb.PutInternal(bi + 2, Long5(x));
                bb.PutInternal(bi + 3, Long4(x));
                bb.PutInternal(bi + 4, Long3(x));
                bb.PutInternal(bi + 5, Long2(x));
                bb.PutInternal(bi + 6, Long1(x));
                bb.PutInternal(bi + 7, Long0(x));
            }

            public static void PutLong(AbstractIOBuffer bb, int bi, long x, bool bigEndian)
            {
                if (bigEndian)
                    PutLongB(bb, bi, x);
                else
                    PutLongL(bb, bi, x);
            }

            // -- get/put float --

            public static float GetFloatL(AbstractIOBuffer bb, int bi)
            {
                return Int32BitsToSingle(GetIntL(bb, bi));
            }

            public static float GetFloatB(AbstractIOBuffer bb, int bi)
            {
                return Int32BitsToSingle(GetIntB(bb, bi));
            }

            public static float GetFloat(AbstractIOBuffer bb, int bi, bool bigEndian)
            {
                return (bigEndian ? GetFloatB(bb, bi) : GetFloatL(bb, bi));
            }

            public static void PutFloatL(AbstractIOBuffer bb, int bi, float x)
            {
                PutIntL(bb, bi, SingleToInt32Bits(x));
            }

            public static void PutFloatB(AbstractIOBuffer bb, int bi, float x)
            {
                PutIntB(bb, bi, SingleToInt32Bits(x));
            }

            public static void PutFloat(AbstractIOBuffer bb, int bi, float x, bool bigEndian)
            {
                if (bigEndian)
                    PutFloatB(bb, bi, x);
                else
                    PutFloatL(bb, bi, x);
            }

            // -- get/put double --

            public static double GetDoubleL(AbstractIOBuffer bb, int bi)
            {
                return BitConverter.Int64BitsToDouble(GetLongL(bb, bi));
            }

            public static double GetDoubleB(AbstractIOBuffer bb, int bi)
            {
                return BitConverter.Int64BitsToDouble(GetLongB(bb, bi));
            }

            public static double GetDouble(AbstractIOBuffer bb, int bi, bool bigEndian)
            {
                return (bigEndian ? GetDoubleB(bb, bi) : GetDoubleL(bb, bi));
            }

            public static void PutDoubleL(AbstractIOBuffer bb, int bi, double x)
            {
                PutLongL(bb, bi, BitConverter.DoubleToInt64Bits(x));
            }

            public static void PutDoubleB(AbstractIOBuffer bb, int bi, double x)
            {
                PutLongB(bb, bi, BitConverter.DoubleToInt64Bits(x));
            }

            public static void PutDouble(AbstractIOBuffer bb, int bi, double x, bool bigEndian)
            {
                if (bigEndian)
                    PutDoubleB(bb, bi, x);
                else
                    PutDoubleL(bb, bi, x);
            }

            private static int SingleToInt32Bits(float f)
            {
                var bytes = BitConverter.GetBytes(f);
                return BitConverter.ToInt32(bytes, 0);
            }

            private static float Int32BitsToSingle(int i)
            {
                var bytes = BitConverter.GetBytes(i);
                return BitConverter.ToSingle(bytes, 0);
            }
        }

        #endregion
    }
}
