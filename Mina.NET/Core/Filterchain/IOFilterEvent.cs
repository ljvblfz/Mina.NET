using System;
using Common.Logging;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An I/O event or an I/O request that MINA provides for <see cref="IOFilter"/>s.
    /// It is usually used by internal components to store I/O events.
    /// </summary>
    public class IOFilterEvent : IOEvent
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(IOFilterEvent));

        /// <summary>
        /// </summary>
        public IOFilterEvent(INextFilter nextFilter, IoEventType eventType, IOSession session, object parameter)
            : base(eventType, session, parameter)
        {
            if (nextFilter == null)
                throw new ArgumentNullException(nameof(nextFilter));
            NextFilter = nextFilter;
        }

        /// <summary>
        /// Gets the next filter.
        /// </summary>
        public INextFilter NextFilter { get; }

        /// <inheritdoc/>
        public override void Fire()
        {
            if (Log.IsDebugEnabled)
                Log.DebugFormat("Firing a {0} event for session {1}", EventType, Session.Id);

            switch (EventType)
            {
                case IoEventType.MessageReceived:
                    NextFilter.MessageReceived(Session, Parameter);
                    break;
                case IoEventType.MessageSent:
                    NextFilter.MessageSent(Session, (IWriteRequest)Parameter);
                    break;
                case IoEventType.Write:
                    NextFilter.FilterWrite(Session, (IWriteRequest)Parameter);
                    break;
                case IoEventType.Close:
                    NextFilter.FilterClose(Session);
                    break;
                case IoEventType.ExceptionCaught:
                    NextFilter.ExceptionCaught(Session, (Exception)Parameter);
                    break;
                case IoEventType.SessionIdle:
                    NextFilter.SessionIdle(Session, (IdleStatus)Parameter);
                    break;
                case IoEventType.SessionCreated:
                    NextFilter.SessionCreated(Session);
                    break;
                case IoEventType.SessionOpened:
                    NextFilter.SessionOpened(Session);
                    break;
                case IoEventType.SessionClosed:
                    NextFilter.SessionClosed(Session);
                    break;
                default:
                    throw new InvalidOperationException("Unknown event type: " + EventType);
            }

            if (Log.IsDebugEnabled)
                Log.DebugFormat("Event {0} has been fired for session {1}", EventType, Session.Id);
        }
    }
}
