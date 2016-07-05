using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// Base implementation of <see cref="IOSession"/>.
    /// </summary>
    public abstract class AbstractIOSession : IOSession, IDisposable
    {
        private static readonly IWriteRequest CloseRequest = new DefaultWriteRequest(new object());
        private static long _idGenerator;
        private object _syncRoot = new byte[0];
        private IWriteRequestQueue _writeRequestQueue;
        private volatile bool _closing;

        /// <summary>
        /// </summary>
        protected AbstractIOSession(IOService service)
        {
            Service = service;
            Handler = service.Handler;

            CreationTime = DateTime.Now;
            _lastThroughputCalculationTime = CreationTime;
            LastReadTime = LastWriteTime = CreationTime;

            Id = Interlocked.Increment(ref _idGenerator);

            CloseFuture = new DefaultCloseFuture(this);
            CloseFuture.Complete += ResetCounter;
        }

        /// <inheritdoc/>
        public long Id { get; }

        /// <inheritdoc/>
        public IOSessionConfig Config { get; protected set; }

        /// <inheritdoc/>
        public IOService Service { get; }

        /// <inheritdoc/>
        public abstract IOProcessor Processor { get; }

        /// <inheritdoc/>
        public virtual IOHandler Handler { get; }

        /// <inheritdoc/>
        public abstract IOFilterChain FilterChain { get; }

        /// <inheritdoc/>
        public abstract ITransportMetadata TransportMetadata { get; }

        /// <inheritdoc/>
        public bool Connected => !CloseFuture.Closed;

        /// <inheritdoc/>
        public bool Closing => _closing || CloseFuture.Closed;

        /// <inheritdoc/>
        public virtual bool Secured => false;

        /// <inheritdoc/>
        public ICloseFuture CloseFuture { get; }

        /// <inheritdoc/>
        public abstract EndPoint LocalEndPoint { get; }

        /// <inheritdoc/>
        public abstract EndPoint RemoteEndPoint { get; }

        /// <inheritdoc/>
        public IOSessionAttributeMap AttributeMap { get; set; }

        /// <inheritdoc/>
        public IWriteRequestQueue WriteRequestQueue
        {
            get
            {
                if (_writeRequestQueue == null)
                {
                    throw new InvalidOperationException();
                }
                return _writeRequestQueue;
            }
        }

        /// <inheritdoc/>
        public IWriteRequest CurrentWriteRequest { get; set; }

        /// <inheritdoc/>
        public void SetWriteRequestQueue(IWriteRequestQueue queue)
        {
            _writeRequestQueue = new CloseAwareWriteQueue(this, queue);
        }

        /// <inheritdoc/>
        public IWriteFuture Write(object message)
        {
            return Write(message, null);
        }

        /// <inheritdoc/>
        public IWriteFuture Write(object message, EndPoint remoteEp)
        {
            if (message == null)
            {
                return null;
            }

            if (!TransportMetadata.Connectionless && remoteEp != null)
            {
                throw new InvalidOperationException();
            }

            // If the session has been closed or is closing, we can't either
            // send a message to the remote side. We generate a future
            // containing an exception.
            if (Closing || !Connected)
            {
                IWriteFuture future = new DefaultWriteFuture(this);
                IWriteRequest request = new DefaultWriteRequest(message, future, remoteEp);
                future.Exception = new WriteToClosedSessionException(request);
                return future;
            }

            var buf = message as IOBuffer;
            if (buf == null)
            {
                var fi = message as System.IO.FileInfo;
                if (fi != null)
                {
                    message = new FileInfoFileRegion(fi);
                }
            }
            else if (!buf.HasRemaining)
            {
                return DefaultWriteFuture.NewNotWrittenFuture(this,
                    new ArgumentException("message is empty. Forgot to call flip()?", nameof(message)));
            }

            // Now, we can write the message. First, create a future
            IWriteFuture writeFuture = new DefaultWriteFuture(this);
            IWriteRequest writeRequest = new DefaultWriteRequest(message, writeFuture, remoteEp);

            // Then, get the chain and inject the WriteRequest into it
            var filterChain = FilterChain;
            filterChain.FireFilterWrite(writeRequest);

            return writeFuture;
        }

        /// <inheritdoc/>
        public ICloseFuture Close(bool rightNow)
        {
            if (Closing)
            {
                return CloseFuture;
            }
            if (rightNow)
            {
                return Close();
            }
            return CloseOnFlush();
        }

        /// <inheritdoc/>
        public ICloseFuture Close()
        {
            lock (_syncRoot)
            {
                if (Closing)
                {
                    return CloseFuture;
                }
                _closing = true;
            }
            FilterChain.FireFilterClose();
            return CloseFuture;
        }

        private ICloseFuture CloseOnFlush()
        {
            WriteRequestQueue.Offer(this, CloseRequest);
            Processor.Flush(this);
            return CloseFuture;
        }

        /// <inheritdoc/>
        public object GetAttribute(object key)
        {
            return GetAttribute(key, null);
        }

        /// <inheritdoc/>
        public object GetAttribute(object key, object defaultValue)
        {
            return AttributeMap.GetAttribute(this, key, defaultValue);
        }

        /// <inheritdoc/>
        public T GetAttribute<T>(object key)
        {
            return GetAttribute(key, default(T));
        }

        /// <inheritdoc/>
        public T GetAttribute<T>(object key, T defaultValue)
        {
            return (T) AttributeMap.GetAttribute(this, key, defaultValue);
        }

        /// <inheritdoc/>
        public object SetAttribute(object key, object value)
        {
            return AttributeMap.SetAttribute(this, key, value);
        }

        /// <inheritdoc/>
        public object SetAttribute(object key)
        {
            return SetAttribute(key, true);
        }

        /// <inheritdoc/>
        public object SetAttributeIfAbsent(object key, object value)
        {
            return AttributeMap.SetAttributeIfAbsent(this, key, value);
        }

        /// <inheritdoc/>
        public object SetAttributeIfAbsent(object key)
        {
            return SetAttributeIfAbsent(key, true);
        }

        /// <inheritdoc/>
        public object RemoveAttribute(object key)
        {
            return AttributeMap.RemoveAttribute(this, key);
        }

        /// <inheritdoc/>
        public bool ContainsAttribute(object key)
        {
            return AttributeMap.ContainsAttribute(this, key);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                ((DefaultCloseFuture) CloseFuture).Dispose();
            }
        }

        #region Traffic control

        /// <inheritdoc/>
        public bool ReadSuspended { get; private set; }

        /// <inheritdoc/>
        public bool WriteSuspended { get; private set; }

        /// <inheritdoc/>
        public void SuspendRead()
        {
            ReadSuspended = true;
            if (Closing || !Connected)
            {
                return;
            }
            Processor.UpdateTrafficControl(this);
        }

        /// <inheritdoc/>
        public void SuspendWrite()
        {
            WriteSuspended = true;
            if (Closing || !Connected)
            {
                return;
            }
            Processor.UpdateTrafficControl(this);
        }

        /// <inheritdoc/>
        public void ResumeRead()
        {
            ReadSuspended = false;
            if (Closing || !Connected)
            {
                return;
            }
            Processor.UpdateTrafficControl(this);
        }

        /// <inheritdoc/>
        public void ResumeWrite()
        {
            WriteSuspended = false;
            if (Closing || !Connected)
            {
                return;
            }
            Processor.UpdateTrafficControl(this);
        }

        #endregion

        #region Status variables

        private int _scheduledWriteBytes;
        private int _scheduledWriteMessages;
        private DateTime _lastThroughputCalculationTime;
        private long _lastReadBytes;
        private long _lastWrittenBytes;
        private long _lastReadMessages;
        private long _lastWrittenMessages;
        private int _idleCountForBoth;
        private int _idleCountForRead;
        private int _idleCountForWrite;
        private DateTime _lastIdleTimeForBoth;
        private DateTime _lastIdleTimeForRead;
        private DateTime _lastIdleTimeForWrite;

        /// <inheritdoc/>
        public long ReadBytes { get; private set; }

        /// <inheritdoc/>
        public long WrittenBytes { get; private set; }

        /// <inheritdoc/>
        public long ReadMessages { get; private set; }

        /// <inheritdoc/>
        public long WrittenMessages { get; private set; }

        /// <inheritdoc/>
        public double ReadBytesThroughput { get; private set; }

        /// <inheritdoc/>
        public double WrittenBytesThroughput { get; private set; }

        /// <inheritdoc/>
        public double ReadMessagesThroughput { get; private set; }

        /// <inheritdoc/>
        public double WrittenMessagesThroughput { get; private set; }

        /// <inheritdoc/>
        public DateTime CreationTime { get; }

        /// <inheritdoc/>
        public DateTime LastIoTime => LastReadTime > LastWriteTime ? LastReadTime : LastWriteTime;

        /// <inheritdoc/>
        public DateTime LastReadTime { get; private set; }

        /// <inheritdoc/>
        public DateTime LastWriteTime { get; private set; }

        /// <inheritdoc/>
        public bool IsIdle(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _idleCountForBoth > 0;
                case IdleStatus.ReaderIdle:
                    return _idleCountForRead > 0;
                case IdleStatus.WriterIdle:
                    return _idleCountForWrite > 0;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        /// <inheritdoc/>
        public bool IsReaderIdle => IsIdle(IdleStatus.ReaderIdle);

        /// <inheritdoc/>
        public bool IsWriterIdle => IsIdle(IdleStatus.WriterIdle);

        /// <inheritdoc/>
        public bool IsBothIdle => IsIdle(IdleStatus.BothIdle);

        /// <inheritdoc/>
        public int GetIdleCount(IdleStatus status)
        {
            if (Config.GetIdleTime(status) == 0)
            {
                switch (status)
                {
                    case IdleStatus.BothIdle:
                        Interlocked.Exchange(ref _idleCountForBoth, 0);
                        break;
                    case IdleStatus.ReaderIdle:
                        Interlocked.Exchange(ref _idleCountForRead, 0);
                        break;
                    case IdleStatus.WriterIdle:
                        Interlocked.Exchange(ref _idleCountForWrite, 0);
                        break;
                }
            }

            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _idleCountForBoth;
                case IdleStatus.ReaderIdle:
                    return _idleCountForRead;
                case IdleStatus.WriterIdle:
                    return _idleCountForWrite;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        /// <inheritdoc/>
        public int BothIdleCount => GetIdleCount(IdleStatus.BothIdle);

        /// <inheritdoc/>
        public int ReaderIdleCount => GetIdleCount(IdleStatus.ReaderIdle);

        /// <inheritdoc/>
        public int WriterIdleCount => GetIdleCount(IdleStatus.WriterIdle);

        /// <inheritdoc/>
        public DateTime GetLastIdleTime(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _lastIdleTimeForBoth;
                case IdleStatus.ReaderIdle:
                    return _lastIdleTimeForRead;
                case IdleStatus.WriterIdle:
                    return _lastIdleTimeForWrite;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        /// <inheritdoc/>
        public DateTime LastBothIdleTime => GetLastIdleTime(IdleStatus.BothIdle);

        /// <inheritdoc/>
        public DateTime LastReaderIdleTime => GetLastIdleTime(IdleStatus.ReaderIdle);

        /// <inheritdoc/>
        public DateTime LastWriterIdleTime => GetLastIdleTime(IdleStatus.WriterIdle);

        /// <summary>
        /// Increases idle count.
        /// </summary>
        /// <param name="status">the <see cref="IdleStatus"/></param>
        /// <param name="currentTime">the time</param>
        public void IncreaseIdleCount(IdleStatus status, DateTime currentTime)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    Interlocked.Increment(ref _idleCountForBoth);
                    _lastIdleTimeForBoth = currentTime;
                    break;
                case IdleStatus.ReaderIdle:
                    Interlocked.Increment(ref _idleCountForRead);
                    _lastIdleTimeForRead = currentTime;
                    break;
                case IdleStatus.WriterIdle:
                    Interlocked.Increment(ref _idleCountForWrite);
                    _lastIdleTimeForWrite = currentTime;
                    break;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        /// <summary>
        /// Increases read bytes.
        /// </summary>
        /// <param name="increment">the amount to increase</param>
        /// <param name="currentTime">the time</param>
        public void IncreaseReadBytes(long increment, DateTime currentTime)
        {
            if (increment <= 0)
            {
                return;
            }

            ReadBytes += increment;
            LastReadTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForRead, 0);

            Service.Statistics.IncreaseReadBytes(increment, currentTime);
        }

        /// <summary>
        /// Increases read messages.
        /// </summary>
        /// <param name="currentTime">the time</param>
        public void IncreaseReadMessages(DateTime currentTime)
        {
            ReadMessages++;
            LastReadTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForRead, 0);

            Service.Statistics.IncreaseReadMessages(currentTime);
        }

        /// <summary>
        /// Increases written bytes.
        /// </summary>
        /// <param name="increment">the amount to increase</param>
        /// <param name="currentTime">the time</param>
        public void IncreaseWrittenBytes(int increment, DateTime currentTime)
        {
            if (increment <= 0)
            {
                return;
            }

            WrittenBytes += increment;
            LastWriteTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForWrite, 0);

            Service.Statistics.IncreaseWrittenBytes(increment, currentTime);
            IncreaseScheduledWriteBytes(-increment);
        }

        /// <summary>
        /// Increases written messages.
        /// </summary>
        /// <param name="request">the request written</param>
        /// <param name="currentTime">the time</param>
        public void IncreaseWrittenMessages(IWriteRequest request, DateTime currentTime)
        {
            var buf = request.Message as IOBuffer;
            if (buf != null && buf.HasRemaining)
            {
                return;
            }

            WrittenMessages++;
            LastWriteTime = currentTime;

            Service.Statistics.IncreaseWrittenMessages(currentTime);
            DecreaseScheduledWriteMessages();
        }

        /// <summary>
        /// Increase the number of scheduled write bytes for the session.
        /// </summary>
        /// <param name="increment">the number of newly added bytes to write</param>
        public void IncreaseScheduledWriteBytes(int increment)
        {
            Interlocked.Add(ref _scheduledWriteBytes, increment);
            Service.Statistics.IncreaseScheduledWriteBytes(increment);
        }

        public void IncreaseScheduledWriteMessages()
        {
            Interlocked.Increment(ref _scheduledWriteMessages);
            Service.Statistics.IncreaseScheduledWriteMessages();
        }

        public void DecreaseScheduledWriteMessages()
        {
            Interlocked.Decrement(ref _scheduledWriteMessages);
            Service.Statistics.DecreaseScheduledWriteMessages();
        }

        /// <inheritdoc/>
        public void UpdateThroughput(DateTime currentTime, bool force)
        {
            var interval = (long) (currentTime - _lastThroughputCalculationTime).TotalMilliseconds;

            var minInterval = Config.ThroughputCalculationIntervalInMillis;
            if ((minInterval == 0 || interval < minInterval) && !force)
            {
                return;
            }

            ReadBytesThroughput = (ReadBytes - _lastReadBytes)*1000.0/interval;
            WrittenBytesThroughput = (WrittenBytes - _lastWrittenBytes)*1000.0/interval;
            ReadMessagesThroughput = (ReadMessages - _lastReadMessages)*1000.0/interval;
            WrittenMessagesThroughput = (WrittenMessages - _lastWrittenMessages)*1000.0/interval;

            _lastReadBytes = ReadBytes;
            _lastWrittenBytes = WrittenBytes;
            _lastReadMessages = ReadMessages;
            _lastWrittenMessages = WrittenMessages;

            _lastThroughputCalculationTime = currentTime;
        }

        private static void ResetCounter(object sender, IoFutureEventArgs e)
        {
            var session = (AbstractIOSession) e.Future.Session;
            Interlocked.Exchange(ref session._scheduledWriteBytes, 0);
            Interlocked.Exchange(ref session._scheduledWriteMessages, 0);
            session.ReadBytesThroughput = 0;
            session.ReadMessagesThroughput = 0;
            session.WrittenBytesThroughput = 0;
            session.WrittenMessagesThroughput = 0;
        }

        #endregion

        /// <summary>
        /// Fires a <see cref="IOEventType.SessionIdle"/> event to any applicable sessions in the specified collection.
        /// </summary>
        /// <param name="sessions"></param>
        /// <param name="currentTime"></param>
        public static void NotifyIdleness(IEnumerable<IOSession> sessions, DateTime currentTime)
        {
            foreach (var s in sessions)
            {
                NotifyIdleSession(s, currentTime);
            }
        }

        /// <summary>
        /// Fires a <see cref="IOEventType.SessionIdle"/> event if applicable for the
        /// specified <see cref="IOSession"/>.
        /// </summary>
        public static void NotifyIdleSession(IOSession session, DateTime currentTime)
        {
            NotifyIdleSession(session, currentTime, IdleStatus.BothIdle, session.LastIoTime);
            NotifyIdleSession(session, currentTime, IdleStatus.ReaderIdle, session.LastReadTime);
            NotifyIdleSession(session, currentTime, IdleStatus.WriterIdle, session.LastWriteTime);
            NotifyWriteTimeout(session, currentTime);
        }

        private static void NotifyIdleSession(IOSession session, DateTime currentTime, IdleStatus status,
            DateTime lastIoTime)
        {
            var idleTime = session.Config.GetIdleTimeInMillis(status);
            if (idleTime > 0)
            {
                var lastIdleTime = session.GetLastIdleTime(status);
                if (lastIoTime < lastIdleTime)
                {
                    lastIoTime = lastIdleTime;
                }

                if ((currentTime - lastIoTime).TotalMilliseconds >= idleTime)
                {
                    session.FilterChain.FireSessionIdle(status);
                }
            }
        }

        private static void NotifyWriteTimeout(IOSession session, DateTime currentTime)
        {
            var writeTimeout = session.Config.WriteTimeoutInMillis;
            if ((writeTimeout > 0) && ((currentTime - session.LastWriteTime).TotalMilliseconds >= writeTimeout)
                && !session.WriteRequestQueue.IsEmpty(session))
            {
                var request = session.CurrentWriteRequest;
                if (request != null)
                {
                    session.CurrentWriteRequest = null;
                    var cause = new WriteTimeoutException(request);
                    request.Future.Exception = cause;
                    session.FilterChain.FireExceptionCaught(cause);
                    // WriteException is an IOException, so we close the session.
                    session.Close(true);
                }
            }
        }

        /// <summary>
        /// A queue which handles the CLOSE request.
        /// </summary>
        class CloseAwareWriteQueue : IWriteRequestQueue
        {
            private readonly AbstractIOSession _session;
            private readonly IWriteRequestQueue _queue;

            /// <summary>
            /// </summary>
            public CloseAwareWriteQueue(AbstractIOSession session, IWriteRequestQueue queue)
            {
                _session = session;
                _queue = queue;
            }

            /// <inheritdoc/>
            public int Size => _queue.Size;

            /// <inheritdoc/>
            public IWriteRequest Poll(IOSession session)
            {
                var answer = _queue.Poll(session);
                if (ReferenceEquals(answer, CloseRequest))
                {
                    _session.Close();
                    Dispose(_session);
                    answer = null;
                }
                return answer;
            }

            /// <inheritdoc/>
            public void Offer(IOSession session, IWriteRequest writeRequest)
            {
                _queue.Offer(session, writeRequest);
            }

            /// <inheritdoc/>
            public bool IsEmpty(IOSession session)
            {
                return _queue.IsEmpty(session);
            }

            /// <inheritdoc/>
            public void Clear(IOSession session)
            {
                _queue.Clear(session);
            }

            /// <inheritdoc/>
            public void Dispose(IOSession session)
            {
                _queue.Dispose(session);
            }
        }
    }
}
