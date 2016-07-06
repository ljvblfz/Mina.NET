using System;
using System.IO;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Handler.Stream
{
    /// <summary>
    /// A <see cref="System.IO.Stream"/> that buffers data read from
    /// <see cref="IOHandler.MesIOSessionved(IoSession, object)"/> events,
    /// and forwards all write operations to the associated <see cref="IOSession"/>.
    /// </summary>
    public class IOSessionStream : System.IO.Stream
    {
        private volatile bool _closed;

        private readonly IOBuffer _buf;
        private readonly object _syncRoot;
        private volatile bool _released;
        private IOException _exception;

        private readonly IOSession _session;
        private IWriteFuture _lastWriteFuture;

        /// <summary>
        /// </summary>
        public IOSessionStream()
        {
            _syncRoot = new byte[0];
            _buf = IOBuffer.Allocate(16);
            _buf.AutoExpand = true;
            _buf.Limit = 0;
        }

        /// <summary>
        /// </summary>
        public IOSessionStream(IOSession session)
        {
            _session = session;
        }

        /// <inheritdoc/>
        public override bool CanRead => _buf != null;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => _session != null;

        /// <inheritdoc/>
        public override long Length
        {
            get
            {
                if (CanRead)
                {
                    if (_released)
                        return 0;
                    lock (_syncRoot)
                    {
                        return _buf.Remaining;
                    }
                }
                throw new NotSupportedException();
            }
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            lock (_syncRoot)
            {
                if (!WaitForData())
                    return 0;
                return _buf.Get() & 0xff;
            }
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_syncRoot)
            {
                if (!WaitForData())
                    return 0;

                var readBytes = Math.Min(count, _buf.Remaining);
                _buf.Get(buffer, offset, readBytes);
                return readBytes;
            }
        }

        private bool WaitForData()
        {
            if (_released)
                return false;

            lock (_syncRoot)
            {
                while (!_released && _buf.Remaining == 0 && _exception == null)
                {
                    try
                    {
                        Monitor.Wait(_syncRoot);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        throw new IOException("Interrupted while waiting for more data", e);
                    }
                }
            }

            if (_exception != null)
            {
                ReleaseBuffer();
                throw _exception;
            }

            if (_closed && _buf.Remaining == 0)
            {
                ReleaseBuffer();
                return false;
            }

            return true;
        }

        private void ReleaseBuffer()
        {
            if (_released)
                return;
            _released = true;
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Write(IOBuffer.Wrap((byte[])buffer.Clone(), offset, count));
        }

        /// <inheritdoc/>
        public override void Close()
        {
            base.Close();

            if (_closed)
                return;

            if (_session == null)
            {
                lock (_syncRoot)
                {
                    _closed = true;
                    ReleaseBuffer();
                    Monitor.PulseAll(_syncRoot);
                }
            }
            else
            {
                try
                {
                    Flush();
                }
                finally
                {
                    _closed = true;
                    _session.Close(true).Await();
                }
            }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            if (_lastWriteFuture == null)
                return;
            _lastWriteFuture.Await();
            if (!_lastWriteFuture.Written)
                throw new IOException("The bytes could not be written to the session");
            _lastWriteFuture = null;
        }

        /// <summary>
        /// Writes the given buffer.
        /// </summary>
        public void Write(IOBuffer buf)
        {
            if (CanRead)
            {
                if (_closed)
                    return;

                lock (_syncRoot)
                {
                    if (_buf.HasRemaining)
                    {
                        _buf.Compact().Put(buf).Flip();
                    }
                    else
                    {
                        _buf.Clear().Put(buf).Flip();
                        Monitor.PulseAll(_syncRoot);
                    }
                }
            }
            else if (CanWrite)
            {
                if (!_session.Connected)
                    throw new IOException("The session has been closed.");

                _lastWriteFuture = _session.Write(buf);
            }
            else
                throw new NotSupportedException();
        }

        /// <summary>
        /// Sets an exception and notifies threads wating for this object.
        /// </summary>
        public IOException Exception
        {
            set
            {
                if (_exception == null)
                {
                    lock (_syncRoot)
                    {
                        _exception = value;
                        Monitor.PulseAll(_syncRoot);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
    }
}
