using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A Byte buffer.
    /// </summary>
    public class ByteBuffer : AbstractIOBuffer
    {
        protected byte[] Hb;
        protected int _offset;
        protected bool _readOnly;

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, capacity,
        /// backing array, and array offset
        /// </summary>
        public ByteBuffer(IOBufferAllocator allocator, int mark, int position, int limit, int capacity, byte[] hb, int offset)
            : base(allocator, mark, position, limit, capacity)
        {
            Hb = hb;
            _offset = offset;
        }

        public ByteBuffer(ByteBuffer parent, int mark, int position, int limit, int capacity, byte[] hb, int offset)
            : base(parent, mark, position, limit, capacity)
        {
            Hb = hb;
            _offset = offset;
        }

        public ByteBuffer(IOBufferAllocator allocator, int capacity, int limit)
            : this(allocator, -1, 0, limit, capacity, new byte[capacity], 0)
        {
        }

        public ByteBuffer(IOBufferAllocator allocator, byte[] buffer, int offset, int len)
            : this(allocator, -1, offset, offset + len, buffer.Length, buffer, 0)
        {
        }

        public ByteBuffer(ByteBuffer parent, byte[] buffer, int mark, int position, int limit, int capacity, int offset)
            : this(parent, mark, position, limit, capacity, buffer, offset)
        {
        }

        /// <inheritdoc/>
        public override int Capacity
        {
            get { return base.Capacity; }
            set
            {
                if (!RecapacityAllowed)
                {
                    throw new InvalidOperationException("Derived buffers and their parent can't be expanded.");
                }

                // Allocate a new buffer and transfer all settings to it.
                var capacity = base.Capacity;
                if (value > capacity)
                {
                    // Reallocate.
                    var newHb = new byte[value];
                    System.Buffer.BlockCopy(Hb, Offset(0), newHb, 0, capacity);

                    Hb = newHb;
                    _offset = 0;

                    Recapacity(value);
                }
            }
        }

        /// <inheritdoc/>
        public override bool HasArray => Hb != null && !_readOnly;

        /// <inheritdoc/>
        public override byte Get()
        {
            return Hb[Offset(NextGetIndex())];
        }

        /// <inheritdoc/>
        public override byte Get(int index)
        {
            return Hb[Offset(CheckIndex(index))];
        }

        /// <inheritdoc/>
        public override IOBuffer Get(byte[] dst, int offset, int length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
            {
                throw new BufferUnderflowException();
            }
            Array.Copy(Hb, Offset(Position), dst, offset, length);
            Position += length;
            return this;
        }

        /// <inheritdoc/>
        public override ArraySegment<byte> GetRemaining()
        {
            return new ArraySegment<byte>(Hb, Offset(Position), Remaining);
        }

        /// <inheritdoc/>
        public override IOBuffer Shrink()
        {
            if (!RecapacityAllowed)
            {
                throw new InvalidOperationException("Derived buffers and their parent can't be shrinked.");
            }

            var capacity = Capacity;
            var limit = Limit;
            if (capacity == limit)
            {
                return this;
            }

            var newCapacity = capacity;
            var minCapacity = Math.Max(MinimumCapacity, limit);
            for (;;)
            {
                if (newCapacity >> 1 < minCapacity)
                {
                    break;
                }
                newCapacity >>= 1;
                if (minCapacity == 0)
                {
                    break;
                }
            }

            newCapacity = Math.Max(minCapacity, newCapacity);

            if (newCapacity == capacity)
            {
                return this;
            }

            // Shrink and compact:
            var newHb = new byte[newCapacity];
            System.Buffer.BlockCopy(Hb, Offset(0), newHb, 0, limit);
            Hb = newHb;
            _offset = 0;

            MarkValue = -1;

            Recapacity(newCapacity);

            return this;
        }

        /// <inheritdoc/>
        public override bool ReadOnly => false;

        /// <inheritdoc/>
        public override IOBuffer Compact()
        {
            var remaining = Remaining;
            var capacity = Capacity;

            if (capacity == 0)
            {
                return this;
            }

            if (AutoShrink && remaining <= (capacity >> 2) && capacity > MinimumCapacity)
            {
                var newCapacity = capacity;
                var minCapacity = Math.Max(MinimumCapacity, Remaining << 1);
                for (;;)
                {
                    if ((newCapacity >> 1) < minCapacity)
                    {
                        break;
                    }
                    newCapacity >>= 1;
                }
                newCapacity = Math.Max(minCapacity, newCapacity);
                if (newCapacity == capacity)
                {
                    return this;
                }

                // Shrink and compact:
                // Sanity check.
                if (remaining > newCapacity)
                {
                    throw new InvalidOperationException(
                        "The amount of the remaining bytes is greater than the new capacity.");
                }

                // Reallocate.
                var newHb = new byte[newCapacity];
                System.Buffer.BlockCopy(Hb, Offset(Position), newHb, 0, remaining);

                Hb = newHb;
                _offset = 0;

                Recapacity(newCapacity);
            }
            else
            {
                System.Buffer.BlockCopy(Hb, Offset(Position), Hb, Offset(0), Remaining);
            }

            Position = Remaining;
            Limit = Capacity;
            return this;
        }

        /// <inheritdoc/>
        public override void Free()
        {
            // do nothing
        }

        /// <inheritdoc/>
        protected override IOBuffer Slice0()
        {
            return new ByteBuffer(this, Hb, -1, 0, Remaining, Remaining, Position + _offset);
        }

        /// <inheritdoc/>
        protected override IOBuffer Duplicate0()
        {
            return new ByteBuffer(this, Hb, MarkValue, Position, Limit, Capacity, _offset);
        }

        /// <inheritdoc/>
        protected override IOBuffer AsReadOnlyBuffer0()
        {
            return new ByteBufferR(this, Hb, MarkValue, Position, Limit, Capacity, _offset);
        }

        /// <inheritdoc/>
        protected override void PutInternal(byte[] src, int offset, int length)
        {
            System.Buffer.BlockCopy(src, offset, Hb, Offset(Position), length);
            Position += length;
        }

        /// <inheritdoc/>
        protected override void PutInternal(IOBuffer src)
        {
            var bb = src as ByteBuffer;
            if (bb == null)
            {
                base.PutInternal(src);
            }
            else
            {
                var n = bb.Remaining;
                if (n > Remaining)
                {
                    throw new OverflowException();
                }
                System.Buffer.BlockCopy(bb.Hb, bb.Offset(bb.Position), Hb, Offset(Position), n);
                bb.Position += n;
                Position += n;
            }
        }

        /// <inheritdoc/>
        protected override byte GetInternal(int i)
        {
            return Hb[i];
        }

        /// <inheritdoc/>
        protected override void PutInternal(int i, byte b)
        {
            Hb[i] = b;
        }

        /// <inheritdoc/>
        protected override int Offset(int i)
        {
            return i + _offset;
        }
    }
}
