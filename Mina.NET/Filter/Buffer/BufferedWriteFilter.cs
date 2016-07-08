using System;
using System.Collections.Concurrent;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Buffer
{
    /// <summary>
    /// An <see cref="IOFilter"/> implementation used to buffer outgoing <see cref="IWriteRequest"/>.
    /// Using this filter allows to be less dependent from network latency.
    /// It is also useful when a session is generating very small messages
    /// too frequently and consequently generating unnecessary traffic overhead.
    /// <remarks>
    /// Please note that it should always be placed before the <see cref="Filter.Codec.ProtocolCodecFilter"/> 
    /// as it only handles <see cref="IWriteRequest"/>s carrying <see cref="IOBuffer"/> objects.
    /// </remarks>
    /// </summary>
    public class BufferedWriteFilter : IOFilterAdapter
    {
        /// <summary>
        /// Default buffer size value in bytes.
        /// </summary>
        public const int DefaultBufferSize = 8192;

        static readonly ILog Log = LogManager.GetLogger(typeof(BufferedWriteFilter));

        private ConcurrentDictionary<IOSession, Lazy<IOBuffer>> _buffersMap;

        public BufferedWriteFilter()
            : this(DefaultBufferSize, null)
        {
        }

        public BufferedWriteFilter(int bufferSize)
            : this(bufferSize, null)
        {
        }

#if NET20
        internal
#else
        public
#endif
            BufferedWriteFilter(int bufferSize, ConcurrentDictionary<IOSession, Lazy<IOBuffer>> buffersMap)
        {
            BufferSize = bufferSize;
            _buffersMap = buffersMap == null
                ? new ConcurrentDictionary<IOSession, Lazy<IOBuffer>>()
                : buffersMap;
        }

        /// <summary>
        /// Gets or sets the buffer size (only for the newly created buffers).
        /// </summary>
        public int BufferSize { get; set; }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var buf = writeRequest.Message as IOBuffer;
            if (buf == null)
            {
                throw new ArgumentException("This filter should only buffer IoBuffer objects");
            }
            Write(session, buf);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            Free(session);
            base.SessionClosed(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
        {
            Free(session);
            base.ExceptionCaught(nextFilter, session, cause);
        }

        public void Flush(IOSession session)
        {
            Lazy<IOBuffer> lazy;
            _buffersMap.TryGetValue(session, out lazy);
            try
            {
                InternalFlush(session.FilterChain.GetNextFilter(this), session, lazy.Value);
            }
            catch (Exception e)
            {
                session.FilterChain.FireExceptionCaught(e);
            }
        }

        private void Write(IOSession session, IOBuffer data)
        {
            var dest = _buffersMap.GetOrAdd(session,
                new Lazy<IOBuffer>(() => IOBuffer.Allocate(BufferSize)));
            Write(session, data, dest.Value);
        }

        private void Write(IOSession session, IOBuffer data, IOBuffer buf)
        {
            try
            {
                var len = data.Remaining;
                if (len >= buf.Capacity)
                {
                    /*
                     * If the request length exceeds the size of the output buffer,
                     * flush the output buffer and then write the data directly.
                     */
                    var nextFilter = session.FilterChain.GetNextFilter(this);
                    InternalFlush(nextFilter, session, buf);
                    nextFilter.FilterWrite(session, new DefaultWriteRequest(data));
                    return;
                }
                if (len > (buf.Limit - buf.Position))
                {
                    InternalFlush(session.FilterChain.GetNextFilter(this), session, buf);
                }

                lock (buf)
                {
                    buf.Put(data);
                }
            }
            catch (Exception e)
            {
                session.FilterChain.FireExceptionCaught(e);
            }
        }

        private void InternalFlush(INextFilter nextFilter, IOSession session, IOBuffer buf)
        {
            IOBuffer tmp = null;
            lock (buf)
            {
                buf.Flip();
                tmp = buf.Duplicate();
                buf.Clear();
            }
            if (Log.IsDebugEnabled)
            {
                Log.Debug("Flushing buffer: " + tmp);
            }
            nextFilter.FilterWrite(session, new DefaultWriteRequest(tmp));
        }

        private void Free(IOSession session)
        {
            Lazy<IOBuffer> lazy;
            if (_buffersMap.TryRemove(session, out lazy))
            {
                lazy.Value.Free();
            }
        }
    }
}
