using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// A base implementation of <see cref="IOSessionConfig"/>.
    /// </summary>
    public abstract class AbstractIOSessionConfig : IOSessionConfig
    {
        private int _idleTimeForRead;
        private int _idleTimeForWrite;
        private int _idleTimeForBoth;

        /// <inheritdoc/>
        public int ReadBufferSize { get; set; } = 2048;

        /// <inheritdoc/>
        public int ThroughputCalculationInterval { get; set; } = 3;

        /// <inheritdoc/>
        public long ThroughputCalculationIntervalInMillis => ThroughputCalculationInterval * 1000L;

        /// <inheritdoc/>
        public int WriteTimeout { get; set; } = 60;

        /// <inheritdoc/>
        public long WriteTimeoutInMillis => WriteTimeout * 1000L;

        /// <inheritdoc/>
        public int ReaderIdleTime
        {
            get { return GetIdleTime(IdleStatus.ReaderIdle); }
            set { SetIdleTime(IdleStatus.ReaderIdle, value); }
        }

        /// <inheritdoc/>
        public int WriterIdleTime
        {
            get { return GetIdleTime(IdleStatus.WriterIdle); }
            set { SetIdleTime(IdleStatus.WriterIdle, value); }
        }

        /// <inheritdoc/>
        public int BothIdleTime
        {
            get { return GetIdleTime(IdleStatus.BothIdle); }
            set { SetIdleTime(IdleStatus.BothIdle, value); }
        }

        /// <inheritdoc/>
        public int GetIdleTime(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.ReaderIdle:
                    return _idleTimeForRead;
                case IdleStatus.WriterIdle:
                    return _idleTimeForWrite;
                case IdleStatus.BothIdle:
                    return _idleTimeForBoth;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        /// <inheritdoc/>
        public long GetIdleTimeInMillis(IdleStatus status)
        {
            return GetIdleTime(status) * 1000L;
        }

        /// <inheritdoc/>
        public void SetIdleTime(IdleStatus status, int idleTime)
        {
            switch (status)
            {
                case IdleStatus.ReaderIdle:
                    _idleTimeForRead = idleTime;
                    break;
                case IdleStatus.WriterIdle:
                    _idleTimeForWrite = idleTime;
                    break;
                case IdleStatus.BothIdle:
                    _idleTimeForBoth = idleTime;
                    break;
                default:
                    throw new ArgumentException("Unknown status", nameof(status));
            }
        }

        /// <inheritdoc/>
        public void SetAll(IOSessionConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            ReadBufferSize = config.ReadBufferSize;
            SetIdleTime(IdleStatus.BothIdle, config.GetIdleTime(IdleStatus.BothIdle));
            SetIdleTime(IdleStatus.ReaderIdle, config.GetIdleTime(IdleStatus.ReaderIdle));
            SetIdleTime(IdleStatus.WriterIdle, config.GetIdleTime(IdleStatus.WriterIdle));
            ThroughputCalculationInterval = config.ThroughputCalculationInterval;
            DoSetAll(config);
        }

        protected abstract void DoSetAll(IOSessionConfig config);
    }
}
