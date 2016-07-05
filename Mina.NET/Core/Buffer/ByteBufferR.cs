using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A read-only ByteBuffer.
    /// </summary>
    public class ByteBufferR : ByteBuffer
    {
        public ByteBufferR(IOBufferAllocator allocator, int capacity, int limit)
            : base(allocator, capacity, limit)
        {
            _readOnly = true;
        }

        public ByteBufferR(IOBufferAllocator allocator, byte[] buffer, int offset, int len)
            : base(allocator, buffer, offset, len)
        {
            _readOnly = true;
        }

        public ByteBufferR(ByteBuffer parent, byte[] buffer, int mark, int position, int limit, int capacity, int offset)
            : base(parent, buffer, mark, position, limit, capacity, offset)
        {
            _readOnly = true;
        }

        public override bool ReadOnly => true;

        public override IOBuffer Fill(byte value, int size)
        {
            throw new NotSupportedException();
        }

        public override IOBuffer Fill(int size)
        {
            throw new NotSupportedException();
        }

        public override IOBuffer Compact()
        {
            throw new NotSupportedException();
        }

        protected override IOBuffer Slice0()
        {
            return new ByteBufferR(this, Hb, -1, 0, Remaining, Remaining, Position + _offset);
        }

        protected override IOBuffer Duplicate0()
        {
            return new ByteBufferR(this, Hb, MarkValue, Position, Limit, Capacity, _offset);
        }

        protected override IOBuffer AsReadOnlyBuffer0()
        {
            return Duplicate();
        }

        protected override void PutInternal(int i, byte b)
        {
            throw new NotSupportedException();
        }

        protected override void PutInternal(byte[] src, int offset, int length)
        {
            throw new NotSupportedException();
        }

        protected override void PutInternal(IOBuffer src)
        {
            throw new NotSupportedException();
        }
    }
}
