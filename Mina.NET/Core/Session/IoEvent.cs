using System;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// An I/O event or an I/O request that MINA provides.
    /// It is usually used by internal components to store I/O events.
    /// </summary>
    public class IOEvent
    {
        /// <summary>
        /// </summary>
        public IOEvent(IoEventType eventType, IOSession session, object parameter)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            EventType = eventType;
            Session = session;
            Parameter = parameter;
        }

        /// <summary>
        /// Gets the <see cref="IoEventType"/> of this event.
        /// </summary>
        public IoEventType EventType { get; }

        /// <summary>
        /// Gets the <see cref="IOSession"/> of this event.
        /// </summary>
        public IOSession Session { get; }

        /// <summary>
        /// Gets the parameter of this event.
        /// </summary>
        public object Parameter { get; }

        /// <summary>
        /// Fires this event.
        /// </summary>
        public virtual void Fire()
        {
            switch (EventType)
            {
                case IoEventType.MessageReceived:
                    Session.FilterChain.FireMessageReceived(Parameter);
                    break;
                case IoEventType.MessageSent:
                    Session.FilterChain.FireMessageSent((IWriteRequest)Parameter);
                    break;
                case IoEventType.Write:
                    Session.FilterChain.FireFilterWrite((IWriteRequest)Parameter);
                    break;
                case IoEventType.Close:
                    Session.FilterChain.FireFilterClose();
                    break;
                case IoEventType.ExceptionCaught:
                    Session.FilterChain.FireExceptionCaught((Exception)Parameter);
                    break;
                case IoEventType.SessionIdle:
                    Session.FilterChain.FireSessionIdle((IdleStatus)Parameter);
                    break;
                case IoEventType.SessionCreated:
                    Session.FilterChain.FireSessionCreated();
                    break;
                case IoEventType.SessionOpened:
                    Session.FilterChain.FireSessionOpened();
                    break;
                case IoEventType.SessionClosed:
                    Session.FilterChain.FireSessionClosed();
                    break;
                default:
                    throw new InvalidOperationException("Unknown event type: " + EventType);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            if (Parameter == null)
                return "[" + Session + "] " + EventType;
            return "[" + Session + "] " + EventType + ": " + Parameter;
        }
    }
}
