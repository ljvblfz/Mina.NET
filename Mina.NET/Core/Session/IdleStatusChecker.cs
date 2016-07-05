using System;
using System.Collections.Generic;
using System.Threading;

namespace Mina.Core.Session
{
    /// <summary>
    /// Detects idle sessions and fires <tt>SessionIdle</tt> events to them.
    /// </summary>
    public class IdleStatusChecker : IDisposable
    {
        public const int IdleCheckingInterval = 1000;

        private readonly Timer _idleTimer;

        public IdleStatusChecker(Func<IEnumerable<IOSession>> getSessionsFunc)
            : this(IdleCheckingInterval, getSessionsFunc)
        { }

        public IdleStatusChecker(int interval, Func<IEnumerable<IOSession>> getSessionsFunc)
        {
            Interval = interval;
            _idleTimer = new Timer(o =>
            {
                AbstractIOSession.NotifyIdleness(getSessionsFunc(), DateTime.Now);
            });
        }

        public int Interval { get; set; }

        public void Start()
        {
            _idleTimer.Change(0, Interval);
        }

        public void Stop()
        {
            _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _idleTimer.Dispose();
            }
        }
    }
}
