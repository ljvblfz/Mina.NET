using System;
using System.Threading;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Statistic
{
    /// <summary>
    /// This class will measure the time it takes for a
    /// method in the <see cref="IOFilter"/> class to execute.
    /// </summary>
    public class ProfilerTimerFilter : IOFilterAdapter
    {
        private volatile TimeUnit _timeUnit;

        private bool _profileMessageReceived;
        private TimerWorker _messageReceivedTimerWorker;
        private bool _profileMessageSent;
        private TimerWorker _messageSentTimerWorker;
        private bool _profileSessionCreated;
        private TimerWorker _sessionCreatedTimerWorker;
        private bool _profileSessionOpened;
        private TimerWorker _sessionOpenedTimerWorker;
        private bool _profileSessionIdle;
        private TimerWorker _sessionIdleTimerWorker;
        private bool _profileSessionClosed;
        private TimerWorker _sessionClosedTimerWorker;

        /// <summary>
        /// Creates a profiler on event <see cref="IOEventType.MessageReceived"/>
        /// and <see cref="IOEventType.MessageSent"/> in milliseconds.
        /// </summary>
        public ProfilerTimerFilter()
            : this(TimeUnit.Milliseconds, IOEventType.MessageReceived | IOEventType.MessageSent)
        {
        }

        /// <summary>
        /// Creates a profiler on event <see cref="IOEventType.MessageReceived"/>
        /// and <see cref="IOEventType.MessageSent"/>.
        /// </summary>
        /// <param name="timeUnit">the time unit being used</param>
        public ProfilerTimerFilter(TimeUnit timeUnit)
            : this(timeUnit, IOEventType.MessageReceived | IOEventType.MessageSent)
        {
        }

        /// <summary>
        /// Creates a profiler.
        /// </summary>
        /// <param name="timeUnit">the time unit being used</param>
        /// <param name="eventTypes">the event types to profile</param>
        public ProfilerTimerFilter(TimeUnit timeUnit, IOEventType eventTypes)
        {
            _timeUnit = timeUnit;
            SetProfilers(eventTypes);
        }

        /// <summary>
        /// Gets or sets the <see cref="TimeUnit"/> being used.
        /// </summary>
        public TimeUnit TimeUnit
        {
            get { return _timeUnit; }
            set { _timeUnit = value; }
        }

        /// <summary>
        /// Gets or sets the <see cref="IOEventType"/>s to profile.
        /// </summary>
        public IOEventType EventsToProfile
        {
            get
            {
                var et = default(IOEventType);

                if (_profileMessageReceived)
                {
                    et |= IOEventType.MessageReceived;
                }
                if (_profileMessageSent)
                {
                    et |= IOEventType.MessageSent;
                }
                if (_profileSessionCreated)
                {
                    et |= IOEventType.SessionCreated;
                }
                if (_profileSessionOpened)
                {
                    et |= IOEventType.SessionOpened;
                }
                if (_profileSessionIdle)
                {
                    et |= IOEventType.SessionIdle;
                }
                if (_profileSessionClosed)
                {
                    et |= IOEventType.SessionClosed;
                }

                return et;
            }
            set { SetProfilers(value); }
        }

        /// <summary>
        /// Get the average time for the specified method represented by the <see cref="IOEventType"/>.
        /// </summary>
        public double GetAverageTime(IOEventType type)
        {
            return GetTimerWorker(type).Average;
        }

        /// <summary>
        /// Gets the total number of times the method has been called that is represented
        /// by the <see cref="IOEventType"/>.
        /// </summary>
        public long GetTotalCalls(IOEventType type)
        {
            return GetTimerWorker(type).CallsNumber;
        }

        /// <summary>
        /// Gets the total time this method has been executing.
        /// </summary>
        public long GetTotalTime(IOEventType type)
        {
            return GetTimerWorker(type).Total;
        }

        /// <summary>
        /// Gets minimum time the method represented by <see cref="IOEventType"/> has executed.
        /// </summary>
        public long GetMinimumTime(IOEventType type)
        {
            return GetTimerWorker(type).Minimum;
        }

        /// <summary>
        /// Gets maximum time the method represented by <see cref="IOEventType"/> has executed.
        /// </summary>
        public long GetMaximumTime(IOEventType type)
        {
            return GetTimerWorker(type).Maximum;
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            Profile(_profileMessageReceived, _messageReceivedTimerWorker,
                () => nextFilter.MessageReceived(session, message));
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            Profile(_profileMessageSent, _messageSentTimerWorker, () => nextFilter.MessageSent(session, writeRequest));
        }

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IOSession session)
        {
            Profile(_profileSessionCreated, _sessionCreatedTimerWorker, () => nextFilter.SessionCreated(session));
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IOSession session)
        {
            Profile(_profileSessionOpened, _sessionOpenedTimerWorker, () => nextFilter.SessionOpened(session));
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            Profile(_profileSessionIdle, _sessionIdleTimerWorker, () => nextFilter.SessionIdle(session, status));
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            Profile(_profileSessionClosed, _sessionClosedTimerWorker, () => nextFilter.SessionClosed(session));
        }

        private void Profile(bool profile, TimerWorker worker, Action action)
        {
            if (profile)
            {
                var start = TimeNow();
                action();
                var end = TimeNow();
                worker.AddNewDuration(end - start);
            }
            else
            {
                action();
            }
        }

        private TimerWorker GetTimerWorker(IOEventType type)
        {
            switch (type)
            {
                case IOEventType.MessageReceived:
                    if (_profileMessageReceived)
                        return _messageReceivedTimerWorker;
                    break;
                case IOEventType.MessageSent:
                    if (_profileMessageSent)
                        return _messageSentTimerWorker;
                    break;
                case IOEventType.SessionCreated:
                    if (_profileSessionCreated)
                        return _sessionCreatedTimerWorker;
                    break;
                case IOEventType.SessionOpened:
                    if (_profileSessionOpened)
                        return _sessionOpenedTimerWorker;
                    break;
                case IOEventType.SessionIdle:
                    if (_profileSessionIdle)
                        return _sessionIdleTimerWorker;
                    break;
                case IOEventType.SessionClosed:
                    if (_profileSessionClosed)
                        return _sessionClosedTimerWorker;
                    break;
                default:
                    break;
            }

            throw new ArgumentException("You are not monitoring this event. Please add this event first.");
        }

        private void SetProfilers(IOEventType eventTypes)
        {
            if ((eventTypes & IOEventType.MessageReceived) == IOEventType.MessageReceived)
            {
                _messageReceivedTimerWorker = new TimerWorker();
                _profileMessageReceived = true;
            }
            if ((eventTypes & IOEventType.MessageSent) == IOEventType.MessageSent)
            {
                _messageSentTimerWorker = new TimerWorker();
                _profileMessageSent = true;
            }
            if ((eventTypes & IOEventType.SessionCreated) == IOEventType.SessionCreated)
            {
                _sessionCreatedTimerWorker = new TimerWorker();
                _profileSessionCreated = true;
            }
            if ((eventTypes & IOEventType.SessionOpened) == IOEventType.SessionOpened)
            {
                _sessionOpenedTimerWorker = new TimerWorker();
                _profileSessionOpened = true;
            }
            if ((eventTypes & IOEventType.SessionIdle) == IOEventType.SessionIdle)
            {
                _sessionIdleTimerWorker = new TimerWorker();
                _profileSessionIdle = true;
            }
            if ((eventTypes & IOEventType.SessionClosed) == IOEventType.SessionClosed)
            {
                _sessionClosedTimerWorker = new TimerWorker();
                _profileSessionClosed = true;
            }
        }

        private long TimeNow()
        {
            switch (_timeUnit)
            {
                case TimeUnit.Seconds:
                    return DateTime.Now.Ticks/TimeSpan.TicksPerSecond;
                case TimeUnit.Ticks:
                    return DateTime.Now.Ticks;
                default:
                    return DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
            }
        }

        class TimerWorker
        {
            /// <summary>
            /// The sum of all operation durations
            /// </summary>
            public long Total;

            /// <summary>
            /// The number of calls
            /// </summary>
            public long CallsNumber;

            /// <summary>
            /// The fastest operation
            /// </summary>
            public long Minimum = long.MaxValue;

            /// <summary>
            /// The slowest operation
            /// </summary>
            public long Maximum;

            private object _syncRoot = new byte[0];

            public void AddNewDuration(long duration)
            {
                Interlocked.Increment(ref CallsNumber);
                Interlocked.Add(ref Total, duration);
                lock (_syncRoot)
                {
                    if (duration < Minimum)
                    {
                        Minimum = duration;
                    }
                    if (duration > Maximum)
                    {
                        Maximum = duration;
                    }
                }
            }

            public double Average => Total/CallsNumber;
        }
    }

    /// <summary>
    /// The unit of time
    /// </summary>
    public enum TimeUnit
    {
        /// <summary>
        /// Seconds
        /// </summary>
        Seconds,

        /// <summary>
        /// Milliseconds
        /// </summary>
        Milliseconds,

        /// <summary>
        /// Ticks
        /// </summary>
        Ticks
    }
}
