using System;
using System.Threading;
using Common.Logging;
using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Throttles incoming or outgoing events.
    /// </summary>
    public class IoEventQueueThrottle : IOEventQueueHandler
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(IoEventQueueThrottle));

        private volatile int _threshold;
        private readonly IOEventSizeEstimator _sizeEstimator;
        private readonly object _syncRoot = new byte[0];
        private int _counter;
        private int _waiters;

        public IoEventQueueThrottle()
            : this(new DefaultIoEventSizeEstimator(), 65536)
        { }

        public IoEventQueueThrottle(int threshold)
            : this(new DefaultIoEventSizeEstimator(), threshold)
        { }

        public IoEventQueueThrottle(IOEventSizeEstimator sizeEstimator, int threshold)
        {
            if (sizeEstimator == null)
                throw new ArgumentNullException(nameof(sizeEstimator));
            _sizeEstimator = sizeEstimator;
            Threshold = threshold;
        }

        public int Threshold
        {
            get { return _threshold; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Threshold should be greater than 0", nameof(value));
                _threshold = value;
            }
        }

        /// <inheritdoc/>
        public bool Accept(object source, IoEvent ioe)
        {
            return true;
        }

        /// <inheritdoc/>
        public void Offered(object source, IoEvent ioe)
        {
            var eventSize = EstimateSize(ioe);
            var currentCounter = Interlocked.Add(ref _counter, eventSize);

            if (Log.IsDebugEnabled)
                Log.Debug(Thread.CurrentThread.Name + " state: " + _counter + " / " + _threshold);

            if (currentCounter >= _threshold)
                Block();
        }

        /// <inheritdoc/>
        public void Polled(object source, IoEvent ioe)
        {
            var eventSize = EstimateSize(ioe);
            var currentCounter = Interlocked.Add(ref _counter, -eventSize);

            if (Log.IsDebugEnabled)
                Log.Debug(Thread.CurrentThread.Name + " state: " + _counter + " / " + _threshold);

            if (currentCounter < _threshold)
                Unblock();
        }

        protected void Block()
        {
            if (Log.IsDebugEnabled)
                Log.Debug(Thread.CurrentThread.Name + " blocked: " + _counter + " >= " + _threshold);

            lock (_syncRoot)
            {
                while (_counter >= _threshold)
                {
                    _waiters++;
                    try
                    {
                        Monitor.Wait(_syncRoot);
                    }
                    catch (ThreadInterruptedException)
                    {
                        // Wait uninterruptably.
                    }
                    finally
                    {
                        _waiters--;
                    }
                }
            }

            if (Log.IsDebugEnabled)
                Log.Debug(Thread.CurrentThread.Name + " unblocked: " + _counter + " < " + _threshold);
        }

        protected void Unblock()
        {
            lock (_syncRoot)
            {
                if (_waiters > 0)
                {
                    Monitor.PulseAll(_syncRoot);
                }
            }
        }

        private int EstimateSize(IoEvent ioe)
        {
            var size = _sizeEstimator.EstimateSize(ioe);
            if (size < 0)
                throw new InvalidOperationException(_sizeEstimator.GetType().Name + " returned "
                        + "a negative value (" + size + "): " + ioe);
            return size;
        }
    }
}
