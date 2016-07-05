using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// An <see cref="IOFilter"/> that sends a keep-alive request on <see cref="IOEventType.SessionIdle"/>
    /// and sends back the response for the sent keep-alive request. 
    /// </summary>
    public class KeepAliveFilter : IOFilterAdapter
    {
        private readonly AttributeKey _waitingForResponse;
        private readonly AttributeKey _ignoreReaderIdleOnce;

        private readonly IKeepAliveMessageFactory _messageFactory;
        private readonly IdleStatus _interestedIdleStatus;
        private volatile IKeepAliveRequestTimeoutHandler _requestTimeoutHandler;
        private volatile int _requestInterval;
        private volatile int _requestTimeout;
        private volatile bool _forwardEvent;

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>interestedIdleStatus - <see cref="IdleStatus.ReaderIdle"/></li>
        ///   <li>strategy - <see cref="KeepAliveRequestTimeoutHandler.Close"/></li>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory)
            : this(messageFactory, IdleStatus.ReaderIdle, KeepAliveRequestTimeoutHandler.Close)
        { }

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>strategy - <see cref="KeepAliveRequestTimeoutHandler.Close"/></li>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="interestedIdleStatus"></param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus)
            : this(messageFactory, interestedIdleStatus, KeepAliveRequestTimeoutHandler.Close)
        { }

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>interestedIdleStatus - <see cref="IdleStatus.ReaderIdle"/></li>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="strategy"></param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IKeepAliveRequestTimeoutHandler strategy)
            : this(messageFactory, IdleStatus.ReaderIdle, strategy)
        { }

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="interestedIdleStatus"></param>
        /// <param name="strategy"></param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus,
            IKeepAliveRequestTimeoutHandler strategy)
            : this(messageFactory, interestedIdleStatus, strategy, 60, 30)
        { }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="interestedIdleStatus"></param>
        /// <param name="strategy"></param>
        /// <param name="keepAliveRequestInterval">the interval to send a keep-alive request</param>
        /// <param name="keepAliveRequestTimeout">the time to wait for a keep-alive response before timed out</param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus,
            IKeepAliveRequestTimeoutHandler strategy, int keepAliveRequestInterval, int keepAliveRequestTimeout)
        {
            if (messageFactory == null)
                throw new ArgumentNullException(nameof(messageFactory));
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            _waitingForResponse = new AttributeKey(GetType(), "waitingForResponse");
            _ignoreReaderIdleOnce = new AttributeKey(GetType(), "ignoreReaderIdleOnce");
            _messageFactory = messageFactory;
            _interestedIdleStatus = interestedIdleStatus;
            _requestTimeoutHandler = strategy;
            RequestInterval = keepAliveRequestInterval;
            RequestTimeout = keepAliveRequestTimeout;
        }

        /// <summary>
        /// Gets or sets the interval to send a keep-alive request.
        /// </summary>
        public int RequestInterval
        {
            get { return _requestInterval; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("RequestInterval must be a positive integer: " + value);
                _requestInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the time to wait for a keep-alive response before timed out.
        /// </summary>
        public int RequestTimeout
        {
            get { return _requestTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("RequestTimeout must be a positive integer: " + value);
                _requestTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this filter forwards
        /// an <see cref="IOEventType.SessionIdle"/> event to the next filter.
        /// The default value is <code>false</code>.
        /// </summary>
        public bool ForwardEvent
        {
            get { return _forwardEvent; }
            set { _forwardEvent = value; }
        }

        public IKeepAliveRequestTimeoutHandler RequestTimeoutHandler
        {
            get { return _requestTimeoutHandler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                _requestTimeoutHandler = value;
            }
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once. "
                    + "Create another instance and add it.");
        }

        /// <inheritdoc/>
        public override void OnPostAdd(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            ResetStatus(parent.Session);
        }

        /// <inheritdoc/>
        public override void OnPostRemove(IOFilterChain parent, string name, INextFilter nextFilter)
        {
            ResetStatus(parent.Session);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            try
            {
                if (_messageFactory.IsRequest(session, message))
                {
                    var pongMessage = _messageFactory.GetResponse(session, message);

                    if (pongMessage != null)
                        nextFilter.FilterWrite(session, new DefaultWriteRequest(pongMessage));
                }

                if (_messageFactory.IsResponse(session, message))
                    ResetStatus(session);
            }
            finally
            {
                if (!IsKeepAliveMessage(session, message))
                    nextFilter.MessageReceived(session, message);
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            var message = writeRequest.Message;
            if (!IsKeepAliveMessage(session, message))
                nextFilter.MessageSent(session, writeRequest);
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            if (status == _interestedIdleStatus)
            {
                if (!session.ContainsAttribute(_waitingForResponse))
                {
                    var pingMessage = _messageFactory.GetRequest(session);
                    if (pingMessage != null)
                    {
                        nextFilter.FilterWrite(session, new DefaultWriteRequest(pingMessage));

                        // If policy is OFF, there's no need to wait for
                        // the response.
                        if (_requestTimeoutHandler != KeepAliveRequestTimeoutHandler.DeafSpeaker)
                        {
                            MarkStatus(session);
                            if (_interestedIdleStatus == IdleStatus.BothIdle)
                            {
                                session.SetAttribute(_ignoreReaderIdleOnce);
                            }
                        }
                        else
                        {
                            ResetStatus(session);
                        }
                    }
                }
                else
                {
                    HandlePingTimeout(session);
                }
            }
            else if (status == IdleStatus.ReaderIdle)
            {
                if (session.RemoveAttribute(_ignoreReaderIdleOnce) == null)
                {
                    if (session.ContainsAttribute(_waitingForResponse))
                    {
                        HandlePingTimeout(session);
                    }
                }
            }

            if (_forwardEvent)
                nextFilter.SessionIdle(session, status);
        }

        private void ResetStatus(IOSession session)
        {
            session.Config.ReaderIdleTime = 0;
            session.Config.WriterIdleTime = 0;
            session.Config.SetIdleTime(_interestedIdleStatus, RequestInterval);
            session.RemoveAttribute(_waitingForResponse);
        }

        private bool IsKeepAliveMessage(IOSession session, object message)
        {
            return _messageFactory.IsRequest(session, message) || _messageFactory.IsResponse(session, message);
        }

        private void HandlePingTimeout(IOSession session)
        {
            ResetStatus(session);
            var handler = _requestTimeoutHandler;
            if (handler == KeepAliveRequestTimeoutHandler.DeafSpeaker)
                return;
            handler.KeepAliveRequestTimedOut(this, session);
        }

        private void MarkStatus(IOSession session)
        {
            session.Config.SetIdleTime(_interestedIdleStatus, 0);
            session.Config.ReaderIdleTime = RequestTimeout;
            session.SetAttribute(_waitingForResponse);
        }
    }
}
