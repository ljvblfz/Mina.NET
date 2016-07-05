using System;
using System.Net.Sockets;
using Mina.Core.Buffer;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IOBuffer"/> that use <see cref="SocketAsyncEventArgs"/>
    /// as internal implementation.
    /// </summary>
    public class SocketAsyncEventArgsBuffer : AbstractIOBuffer, IDisposable
    {
        /// <summary>
        /// </summary>
        public SocketAsyncEventArgsBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
            : base((IOBufferAllocator) null, -1, 0, socketAsyncEventArgs.Count, socketAsyncEventArgs.Count)
        {
            SocketAsyncEventArgs = socketAsyncEventArgs;
        }

        /// <summary>
        /// </summary>
        public SocketAsyncEventArgsBuffer(IOBufferAllocator allocator, int cap, int lim)
            : this(allocator, new byte[cap], 0, lim)
        {
        }

        /// <summary>
        /// </summary>
        public SocketAsyncEventArgsBuffer(IOBufferAllocator allocator, byte[] buffer, int offset, int count)
            : base(allocator, -1, 0, count, buffer.Length)
        {
            SocketAsyncEventArgs = new SocketAsyncEventArgs();
            SocketAsyncEventArgs.SetBuffer(buffer, offset, count);
        }

        /// <summary>
        /// Gets the inner <see cref="SocketAsyncEventArgs"/>.
        /// </summary>
        public SocketAsyncEventArgs SocketAsyncEventArgs { get; }

        /// <inheritdoc/>
        public override bool ReadOnly => false;

        /// <inheritdoc/>
        public override bool HasArray => true;

        /// <summary>
        /// Sets data buffer for inner <see cref="SocketAsyncEventArgs"/>.
        /// </summary>
        public void SetBuffer()
        {
            if (SocketAsyncEventArgs.Count != Limit)
                SocketAsyncEventArgs.SetBuffer(SocketAsyncEventArgs.Offset, Limit);
        }

        /// <inheritdoc/>
        public override byte Get()
        {
            return SocketAsyncEventArgs.Buffer[Offset(NextGetIndex())];
        }

        /// <inheritdoc/>
        public override IOBuffer Get(byte[] dst, int offset, int length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
                throw new BufferUnderflowException();
            Array.Copy(SocketAsyncEventArgs.Buffer, Offset(Position), dst, offset, length);
            Position += length;
            return this;
        }

        /// <inheritdoc/>
        public override byte Get(int index)
        {
            return SocketAsyncEventArgs.Buffer[Offset(CheckIndex(index))];
        }

        /// <inheritdoc/>
        public override ArraySegment<byte> GetRemaining()
        {
            return new ArraySegment<byte>(SocketAsyncEventArgs.Buffer, SocketAsyncEventArgs.Offset, Limit);
        }

        /// <inheritdoc/>
        public override IOBuffer Shrink()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override int Offset(int pos)
        {
            return SocketAsyncEventArgs.Offset + pos;
        }

        /// <inheritdoc/>
        protected override byte GetInternal(int i)
        {
            return SocketAsyncEventArgs.Buffer[i];
        }

        /// <inheritdoc/>
        protected override void PutInternal(int i, byte b)
        {
            SocketAsyncEventArgs.Buffer[i] = b;
        }

        /// <inheritdoc/>
        protected override void PutInternal(byte[] src, int offset, int length)
        {
            System.Buffer.BlockCopy(src, offset, SocketAsyncEventArgs.Buffer, Offset(Position), length);
            Position += length;
        }

        /// <inheritdoc/>
        protected override void PutInternal(IOBuffer src)
        {
            var array = src.GetRemaining();
            if (array.Count > Remaining)
                throw new OverflowException();
            PutInternal(array.Array, array.Offset, array.Count);
            src.Position += array.Count;
        }

        /// <inheritdoc/>
        public override IOBuffer Compact()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void Free()
        {
            // TODO free buffer?
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                SocketAsyncEventArgs.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override IOBuffer Slice0()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override IOBuffer AsReadOnlyBuffer0()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override IOBuffer Duplicate0()
        {
            throw new NotImplementedException();
        }
    }
}
