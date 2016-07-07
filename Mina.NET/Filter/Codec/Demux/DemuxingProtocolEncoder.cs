using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// A composite <see cref="IProtocolEncoder"/> that demultiplexes incoming message
    /// encoding requests into an appropriate <see cref="IMessageEncoder"/>.
    /// </summary>
    public class DemuxingProtocolEncoder : IProtocolEncoder
    {
        private readonly AttributeKey _state;

        private readonly Dictionary<Type, IMessageEncoderFactory> _type2EncoderFactory
            = new Dictionary<Type, IMessageEncoderFactory>();

        public DemuxingProtocolEncoder()
        {
            _state = new AttributeKey(GetType(), "state");
        }

        public void AddMessageEncoder<TMessage, TEncoder>() where TEncoder : IMessageEncoder
        {
            var encoderType = typeof(TEncoder);

            if (encoderType.GetConstructor(DemuxingProtocolCodecFactory.EmptyParams) == null)
            {
                throw new ArgumentException("The specified class doesn't have a public default constructor.");
            }

            AddMessageEncoder<TMessage>(new DefaultConstructorMessageEncoderFactory<TMessage>(encoderType));
        }

        public void AddMessageEncoder<TMessage>(IMessageEncoder<TMessage> encoder)
        {
            AddMessageEncoder<TMessage>(new SingletonMessageEncoderFactory<TMessage>(encoder));
        }

        public void AddMessageEncoder<TMessage>(IMessageEncoderFactory<TMessage> factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var messageType = typeof(TMessage);
            lock (_type2EncoderFactory)
            {
                if (_type2EncoderFactory.ContainsKey(messageType))
                {
                    throw new InvalidOperationException("The specified message type (" + messageType.Name
                                                        + ") is registered already.");
                }
                _type2EncoderFactory[messageType] = factory;
            }
        }

        public void Encode(IOSession session, object message, IProtocolEncoderOutput output)
        {
            var state = GetState(session);
            var encoder = FindEncoder(state, message.GetType());
            if (encoder == null)
            {
                throw new UnknownMessageTypeException("No message encoder found for message: " + message);
            }
            encoder.Encode(session, message, output);
        }

        public void Dispose(IOSession session)
        {
            session.RemoveAttribute(_state);
        }

        private State GetState(IOSession session)
        {
            var state = session.GetAttribute<State>(_state);
            if (state == null)
            {
                state = new State(_type2EncoderFactory);
                var oldState = (State) session.SetAttributeIfAbsent(_state, state);
                if (oldState != null)
                {
                    state = oldState;
                }
            }
            return state;
        }

        private IMessageEncoder FindEncoder(State state, Type type)
        {
            return FindEncoder(state, type, null);
        }

        private IMessageEncoder FindEncoder(State state, Type type, HashSet<Type> triedClasses)
        {
            IMessageEncoder encoder = null;

            if (triedClasses != null && triedClasses.Contains(type))
            {
                return null;
            }

            // Try the cache first.
            if (state.FindEncoderCache.TryGetValue(type, out encoder))
            {
                return encoder;
            }

            // Try the registered encoders for an immediate match.
            state.Type2Encoder.TryGetValue(type, out encoder);

            if (encoder == null)
            {
                // No immediate match could be found. Search the type's interfaces.
                if (triedClasses == null)
                {
                    triedClasses = new HashSet<Type>();
                }
                triedClasses.Add(type);

                foreach (var ifc in type.GetInterfaces())
                {
                    encoder = FindEncoder(state, ifc, triedClasses);
                    if (encoder != null)
                    {
                        break;
                    }
                }
            }

            if (encoder == null)
            {
                // No match in type's interfaces could be found. Search the superclass.
                var baseType = type.BaseType;
                if (baseType != null)
                {
                    encoder = FindEncoder(state, baseType);
                }
            }

            /*
             * Make sure the encoder is added to the cache. By updating the cache
             * here all the types (superclasses and interfaces) in the path which
             * led to a match will be cached along with the immediate message type.
             */
            if (encoder != null)
            {
                encoder = state.FindEncoderCache.GetOrAdd(type, encoder);
            }

            return encoder;
        }

        class State
        {
            public readonly ConcurrentDictionary<Type, IMessageEncoder> FindEncoderCache
                = new ConcurrentDictionary<Type, IMessageEncoder>();

            public ConcurrentDictionary<Type, IMessageEncoder> Type2Encoder
                = new ConcurrentDictionary<Type, IMessageEncoder>();

            public State(IDictionary<Type, IMessageEncoderFactory> type2EncoderFactory)
            {
                foreach (var pair in type2EncoderFactory)
                {
                    Type2Encoder[pair.Key] = pair.Value.GetEncoder();
                }
            }
        }

        class SingletonMessageEncoderFactory<T> : IMessageEncoderFactory<T>
        {
            private readonly IMessageEncoder<T> _encoder;

            public SingletonMessageEncoderFactory(IMessageEncoder<T> encoder)
            {
                if (encoder == null)
                {
                    throw new ArgumentNullException(nameof(encoder));
                }
                this._encoder = encoder;
            }

            public IMessageEncoder<T> GetEncoder()
            {
                return _encoder;
            }

            IMessageEncoder IMessageEncoderFactory.GetEncoder()
            {
                return _encoder;
            }
        }

        class DefaultConstructorMessageEncoderFactory<T> : IMessageEncoderFactory<T>
        {
            private readonly Type _encoderType;

            public DefaultConstructorMessageEncoderFactory(Type encoderType)
            {
                this._encoderType = encoderType;
            }

            public IMessageEncoder<T> GetEncoder()
            {
                return (IMessageEncoder<T>) Activator.CreateInstance(_encoderType);
            }

            IMessageEncoder IMessageEncoderFactory.GetEncoder()
            {
                return GetEncoder();
            }
        }
    }
}
