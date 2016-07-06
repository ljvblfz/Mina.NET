using System;
using System.Threading;

namespace Mina.Core.Service
{
    /// <summary>
    /// Provides usage statistics for an <see cref="IOService"/> instance.
    /// </summary>
    public class IOServiceStatistics
    {
        private readonly IOService _service;
        private readonly object _throughputCalculationLock = new byte[0];

        private double _readBytesThroughput;
        private double _writtenBytesThroughput;
        private double _readMessagesThroughput;
        private double _writtenMessagesThroughput;
        private long _readBytes;
        private long _writtenBytes;
        private long _readMessages;
        private long _writtenMessages;
        private long _lastReadBytes;
        private long _lastWrittenBytes;
        private long _lastReadMessages;
        private long _lastWrittenMessages;
        private int _scheduledWriteBytes;
        private int _scheduledWriteMessages;
        private int _throughputCalculationInterval = 3;

        /// <summary>
        /// Initializes.
        /// </summary>
        public IOServiceStatistics(IOService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the time when I/O occurred lastly.
        /// </summary>
        public DateTime LastIoTime => LastReadTime > LastWriteTime ? LastReadTime : LastWriteTime;

        /// <summary>
        /// Gets or sets last time at which a read occurred on the service.
        /// </summary>
        public DateTime LastReadTime { get; set; }

        /// <summary>
        /// Gets or sets last time at which a write occurred on the service.
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Gets the number of bytes read by this service.
        /// </summary>
        public long ReadBytes => _readBytes;

        /// <summary>
        /// Gets the number of bytes written out by this service.
        /// </summary>
        public long WrittenBytes => _writtenBytes;

        /// <summary>
        /// Gets the number of messages this services has read.
        /// </summary>
        public long ReadMessages => _readMessages;

        /// <summary>
        /// Gets the number of messages this service has written.
        /// </summary>
        public long WrittenMessages => _writtenMessages;

        /// <summary>
        /// Gets the number of read bytes per second.
        /// </summary>
        public double ReadBytesThroughput
        {
            get
            {
                ResetThroughput();
                return _readBytesThroughput;
            }
        }

        /// <summary>
        /// Gets the number of written bytes per second.
        /// </summary>
        public double WrittenBytesThroughput
        {
            get
            {
                ResetThroughput();
                return _writtenBytesThroughput;
            }
        }

        /// <summary>
        /// Gets the number of read messages per second.
        /// </summary>
        public double ReadMessagesThroughput
        {
            get
            {
                ResetThroughput();
                return _readMessagesThroughput;
            }
        }

        /// <summary>
        /// Gets the number of written messages per second.
        /// </summary>
        public double WrittenMessagesThroughput
        {
            get
            {
                ResetThroughput();
                return _writtenMessagesThroughput;
            }
        }

        /// <summary>
        /// Gets the maximum of the <see cref="ReadBytesThroughput"/>.
        /// </summary>
        public double LargestReadBytesThroughput { get; private set; }

        /// <summary>
        /// Gets the maximum of the <see cref="WrittenBytesThroughput"/>.
        /// </summary>
        public double LargestWrittenBytesThroughput { get; private set; }

        /// <summary>
        /// Gets the maximum of the <see cref="ReadMessagesThroughput"/>.
        /// </summary>
        public double LargestReadMessagesThroughput { get; private set; }

        /// <summary>
        /// Gets the maximum of the <see cref="WrittenMessagesThroughput"/>.
        /// </summary>
        public double LargestWrittenMessagesThroughput { get; private set; }

        /// <summary>
        /// Gets or sets the interval (seconds) between each throughput calculation. The default value is 3 seconds.
        /// </summary>
        public int ThroughputCalculationInterval
        {
            get { return _throughputCalculationInterval; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("ThroughputCalculationInterval should be greater than 0", nameof(value));
                }
                _throughputCalculationInterval = value;
            }
        }

        /// <summary>
        /// Gets the interval (milliseconds) between each throughput calculation.
        /// </summary>
        public long ThroughputCalculationIntervalInMillis => _throughputCalculationInterval * 1000L;

        internal DateTime LastThroughputCalculationTime { get; set; }

        /// <summary>
        /// Gets the count of bytes scheduled for write.
        /// </summary>
        public int ScheduledWriteBytes => _scheduledWriteBytes;

        /// <summary>
        /// Gets the count of messages scheduled for write.
        /// </summary>
        public int ScheduledWriteMessages => _scheduledWriteMessages;

        /// <summary>
        /// Updates the throughput counters.
        /// </summary>
        public void UpdateThroughput(DateTime currentTime)
        {
            lock (_throughputCalculationLock)
            {
                var interval = (long) (currentTime - LastThroughputCalculationTime).TotalMilliseconds;
                var minInterval = ThroughputCalculationIntervalInMillis;
                if (minInterval == 0 || interval < minInterval)
                {
                    return;
                }

                var readBytes = _readBytes;
                var writtenBytes = _writtenBytes;
                var readMessages = _readMessages;
                var writtenMessages = _writtenMessages;

                _readBytesThroughput = (readBytes - _lastReadBytes) * 1000.0 / interval;
                _writtenBytesThroughput = (writtenBytes - _lastWrittenBytes) * 1000.0 / interval;
                _readMessagesThroughput = (readMessages - _lastReadMessages) * 1000.0 / interval;
                _writtenMessagesThroughput = (writtenMessages - _lastWrittenMessages) * 1000.0 / interval;

                if (_readBytesThroughput > LargestReadBytesThroughput)
                {
                    LargestReadBytesThroughput = _readBytesThroughput;
                }
                if (_writtenBytesThroughput > LargestWrittenBytesThroughput)
                {
                    LargestWrittenBytesThroughput = _writtenBytesThroughput;
                }
                if (_readMessagesThroughput > LargestReadMessagesThroughput)
                {
                    LargestReadMessagesThroughput = _readMessagesThroughput;
                }
                if (_writtenMessagesThroughput > LargestWrittenMessagesThroughput)
                {
                    LargestWrittenMessagesThroughput = _writtenMessagesThroughput;
                }

                _lastReadBytes = readBytes;
                _lastWrittenBytes = writtenBytes;
                _lastReadMessages = readMessages;
                _lastWrittenMessages = writtenMessages;

                LastThroughputCalculationTime = currentTime;
            }
        }

        /// <summary>
        /// Increases the count of read bytes.
        /// </summary>
        /// <param name="increment">the number of bytes read</param>
        /// <param name="currentTime">current time</param>
        public void IncreaseReadBytes(long increment, DateTime currentTime)
        {
            Interlocked.Add(ref _readBytes, increment);
            LastReadTime = currentTime;
        }

        /// <summary>
        /// Increases the count of read messages by 1 and sets the last read time to current time.
        /// </summary>
        /// <param name="currentTime">current time</param>
        public void IncreaseReadMessages(DateTime currentTime)
        {
            Interlocked.Increment(ref _readMessages);
            LastReadTime = currentTime;
        }

        /// <summary>
        /// Increases the count of written bytes.
        /// </summary>
        /// <param name="increment">the number of bytes written</param>
        /// <param name="currentTime">current time</param>
        public void IncreaseWrittenBytes(int increment, DateTime currentTime)
        {
            Interlocked.Add(ref _writtenBytes, increment);
            LastWriteTime = currentTime;
        }

        /// <summary>
        /// Increases the count of written messages by 1 and sets the last write time to current time.
        /// </summary>
        /// <param name="currentTime">current time</param>
        public void IncreaseWrittenMessages(DateTime currentTime)
        {
            Interlocked.Increment(ref _writtenMessages);
            LastWriteTime = currentTime;
        }

        /// <summary>
        /// Increments by <code>increment</code> the count of bytes scheduled for write.
        /// </summary>
        public void IncreaseScheduledWriteBytes(int increment)
        {
            Interlocked.Add(ref _scheduledWriteBytes, increment);
        }

        /// <summary>
        /// Increments by 1 the count of messages scheduled for write.
        /// </summary>
        public void IncreaseScheduledWriteMessages()
        {
            Interlocked.Increment(ref _scheduledWriteMessages);
        }

        /// <summary>
        /// Decrements by 1 the count of messages scheduled for write.
        /// </summary>
        public void DecreaseScheduledWriteMessages()
        {
            Interlocked.Decrement(ref _scheduledWriteMessages);
        }

        private void ResetThroughput()
        {
            if (_service.ManagedSessions.Count == 0)
            {
                _readBytesThroughput = 0;
                _writtenBytesThroughput = 0;
                _readMessagesThroughput = 0;
                _writtenMessagesThroughput = 0;
            }
        }
    }
}
