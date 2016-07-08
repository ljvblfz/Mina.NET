using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Mina.Util
{
    class ExpiringMap<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public const int DefaultTimeToLive = 60;
        public const int DefaultExpirationInterval = 1;

        private static volatile int _expirerCount = 1;
        private readonly ConcurrentDictionary<TKey, ExpiringObject> _dict;
        private readonly ReaderWriterLock _stateLock = new ReaderWriterLock();
        private int _timeToLiveMillis;
        private int _expirationIntervalMillis;
        private readonly Thread _expirerThread;
        private bool _running;

        public event EventHandler<ExpirationEventArgs<TValue>> Expired;

        public ExpiringMap()
            : this(DefaultTimeToLive, DefaultExpirationInterval)
        {
        }

        public ExpiringMap(int timeToLive)
            : this(timeToLive, DefaultExpirationInterval)
        {
        }

        public ExpiringMap(int timeToLive, int expirationInterval)
        {
            _dict = new ConcurrentDictionary<TKey, ExpiringObject>();
            TimeToLive = timeToLive;
            ExpirationInterval = expirationInterval;
            _expirerThread = new Thread(Expiring);
            _expirerThread.Name = "ExpiringMapExpirer-" + _expirerCount++;
            _expirerThread.IsBackground = true;
        }

        public int TimeToLive
        {
            get
            {
                _stateLock.AcquireReaderLock(Timeout.Infinite);
                var i = _timeToLiveMillis / 1000;
                _stateLock.ReleaseReaderLock();
                return i;
            }
            set
            {
                _stateLock.AcquireWriterLock(Timeout.Infinite);
                _timeToLiveMillis = value * 1000;
                _stateLock.ReleaseWriterLock();
            }
        }

        public int ExpirationInterval
        {
            get
            {
                _stateLock.AcquireReaderLock(Timeout.Infinite);
                var i = _expirationIntervalMillis / 1000;
                _stateLock.ReleaseReaderLock();
                return i;
            }
            set
            {
                _stateLock.AcquireWriterLock(Timeout.Infinite);
                _expirationIntervalMillis = value * 1000;
                _stateLock.ReleaseWriterLock();
            }
        }

        public bool Running
        {
            get
            {
                _stateLock.AcquireReaderLock(Timeout.Infinite);
                var running = _running;
                _stateLock.ReleaseReaderLock();
                return running;
            }
        }

        public void StartExpiring()
        {
            if (Running)
            {
                return;
            }

            _stateLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (!_running)
                {
                    _running = true;
                    _expirerThread.Start();
                }
            }
            finally
            {
                _stateLock.ReleaseWriterLock();
            }
        }

        public void StopExpiring()
        {
            _stateLock.AcquireWriterLock(Timeout.Infinite);
            try
            {
                if (_running)
                {
                    _running = false;
                    _expirerThread.Interrupt();
                }
            }
            finally
            {
                _stateLock.ReleaseWriterLock();
            }
        }

        private void Expiring()
        {
            while (_running)
            {
                ProcessExpires();
                try
                {
                    Thread.Sleep(_expirationIntervalMillis);
                }
                catch (ThreadInterruptedException)
                {
                    // do nothing
                }
            }
        }

        private void ProcessExpires()
        {
            var now = DateTime.Now;
            ExpiringObject dummy;
            foreach (var o in _dict.Values)
            {
                if (_timeToLiveMillis <= 0)
                {
                    continue;
                }

                if ((now - o.LastAccessTime).TotalMilliseconds >= _timeToLiveMillis)
                {
                    _dict.TryRemove(o.Key, out dummy);
                    DelegateUtils.SafeInvoke(Expired, this, new ExpirationEventArgs<TValue>(o.Value));
                }
            }
        }

        public void Add(TKey key, TValue value)
        {
            _dict.TryAdd(key, new ExpiringObject(key, value, DateTime.Now));
        }

        public bool ContainsKey(TKey key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<TKey> Keys => _dict.Keys;

        public bool Remove(TKey key)
        {
            ExpiringObject obj;
            return _dict.TryRemove(key, out obj);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            ExpiringObject obj;
            if (_dict.TryGetValue(key, out obj))
            {
                value = obj.Value;
                return true;
            }
            value = default(TValue);
            return false;
        }

        public ICollection<TValue> Values
        {
            get { throw new NotSupportedException(); }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                TryGetValue(key, out value);
                return value;
            }
            set
            {
                var obj = new ExpiringObject(key, value, DateTime.Now);
                _dict.AddOrUpdate(key, obj, (k, v) => obj);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var pair in _dict)
            {
                yield return new KeyValuePair<TKey, TValue>(pair.Key, pair.Value.Value);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class ExpiringObject
        {
            private DateTime _lastAccessTime;
            private readonly ReaderWriterLock _lastAccessTimeLock;

            public ExpiringObject(TKey key, TValue value, DateTime lastAccessTime)
            {
                Key = key;
                Value = value;
                _lastAccessTime = lastAccessTime;
                _lastAccessTimeLock = new ReaderWriterLock();
            }

            public TKey Key { get; }

            public TValue Value { get; }

            public DateTime LastAccessTime
            {
                get
                {
                    _lastAccessTimeLock.AcquireReaderLock(Timeout.Infinite);
                    var time = _lastAccessTime;
                    _lastAccessTimeLock.ReleaseReaderLock();
                    return time;
                }
                set
                {
                    _lastAccessTimeLock.AcquireWriterLock(Timeout.Infinite);
                    _lastAccessTime = value;
                    _lastAccessTimeLock.ReleaseWriterLock();
                }
            }

            public override bool Equals(object obj)
            {
                return Equals(Value, obj);
            }

            public override int GetHashCode()
            {
                return Value.GetHashCode();
            }
        }
    }

    class ExpirationEventArgs<T> : EventArgs
    {
        public ExpirationEventArgs(T obj)
        {
            Object = obj;
        }

        public T Object { get; }
    }
}
