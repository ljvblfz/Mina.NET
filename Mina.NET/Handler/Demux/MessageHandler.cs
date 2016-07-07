﻿using System;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// Default implementation of <see cref="IMessageHandler"/>.
    /// </summary>
    public class MessageHandler<T> : IMessageHandler<T>
    {
        public static readonly IMessageHandler<object> Noop = new NoopMessageHandler();

        private readonly Action<IOSession, T> _act;

        /// <summary>
        /// </summary>
        public MessageHandler()
        {
        }

        /// <summary>
        /// </summary>
        public MessageHandler(Action<IOSession, T> act)
        {
            if (act == null)
            {
                throw new ArgumentNullException(nameof(act));
            }
            _act = act;
        }

        /// <inheritdoc/>
        public virtual void HandleMessage(IOSession session, T message)
        {
            if (_act != null)
            {
                _act(session, message);
            }
        }

        void IMessageHandler.HandleMessage(IOSession session, object message)
        {
            HandleMessage(session, (T) message);
        }
    }

    class NoopMessageHandler : IMessageHandler<object>
    {
        internal NoopMessageHandler()
        {
        }

        public void HandleMessage(IOSession session, object message)
        {
            // Do nothing
        }
    }
}
