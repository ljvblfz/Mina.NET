using System;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// A composite <see cref="IProtocolDecoder"/> that demultiplexes incoming <see cref="IOBuffer"/>
    /// decoding requests into an appropriate <see cref="IMessageDecoder"/>.
    /// </summary>
    public class DemuxingProtocolDecoder : CumulativeProtocolDecoder
    {
        private readonly AttributeKey _state;
        private IMessageDecoderFactory[] _decoderFactories = new IMessageDecoderFactory[0];

        public DemuxingProtocolDecoder()
        {
            _state = new AttributeKey(GetType(), "state");
        }

        public void AddMessageDecoder<TDecoder>() where TDecoder : IMessageDecoder
        {
            var decoderType = typeof(TDecoder);

            if (decoderType.GetConstructor(DemuxingProtocolCodecFactory.EmptyParams) == null)
                throw new ArgumentException("The specified class doesn't have a public default constructor.");

            AddMessageDecoder(new DefaultConstructorMessageDecoderFactory(decoderType));
        }

        public void AddMessageDecoder(IMessageDecoder decoder)
        {
            AddMessageDecoder(new SingletonMessageDecoderFactory(decoder));
        }

        public void AddMessageDecoder(IMessageDecoderFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var decoderFactories = _decoderFactories;
            var newDecoderFactories = new IMessageDecoderFactory[decoderFactories.Length + 1];
            Array.Copy(decoderFactories, 0, newDecoderFactories, 0, decoderFactories.Length);
            newDecoderFactories[decoderFactories.Length] = factory;
            _decoderFactories = newDecoderFactories;
        }

        /// <inheritdoc/>
        public override void Dispose(IOSession session)
        {
            base.Dispose(session);
            session.RemoveAttribute(_state);
        }

        /// <inheritdoc/>
        public override void FinishDecode(IOSession session, IProtocolDecoderOutput output)
        {
            base.FinishDecode(session, output);
            var state = GetState(session);
            var currentDecoder = state.CurrentDecoder;
            if (currentDecoder == null)
                return;
            currentDecoder.FinishDecode(session, output);
        }

        /// <inheritdoc/>
        protected override bool DoDecode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            var state = GetState(session);

            if (state.CurrentDecoder == null)
            {
                var decoders = state.Decoders;
                var undecodables = 0;

                for (var i = decoders.Length - 1; i >= 0; i--)
                {
                    var decoder = decoders[i];
                    var limit = input.Limit;
                    var pos = input.Position;

                    MessageDecoderResult result;

                    try
                    {
                        result = decoder.Decodable(session, input);
                    }
                    finally
                    {
                        input.Position = pos;
                        input.Limit = limit;
                    }

                    if (result == MessageDecoderResult.Ok)
                    {
                        state.CurrentDecoder = decoder;
                        break;
                    }
                    if (result == MessageDecoderResult.NotOk)
                    {
                        undecodables++;
                    }
                    else if (result != MessageDecoderResult.NeedData)
                    {
                        throw new InvalidOperationException("Unexpected decode result (see your decodable()): " + result);
                    }
                }

                if (undecodables == decoders.Length)
                {
                    // Throw an exception if all decoders cannot decode data.
                    var dump = input.GetHexDump();
                    input.Position = input.Limit; // Skip data
                    var e = new ProtocolDecoderException("No appropriate message decoder: " + dump);
                    e.Hexdump = dump;
                    throw e;
                }

                if (state.CurrentDecoder == null)
                {
                    // Decoder is not determined yet (i.e. we need more data)
                    return false;
                }
            }

            try
            {
                var result = state.CurrentDecoder.Decode(session, input, output);
                if (result == MessageDecoderResult.Ok)
                {
                    state.CurrentDecoder = null;
                    return true;
                }
                if (result == MessageDecoderResult.NeedData)
                {
                    return false;
                }
                if (result == MessageDecoderResult.NotOk)
                {
                    state.CurrentDecoder = null;
                    throw new ProtocolDecoderException("Message decoder returned NOT_OK.");
                }
                state.CurrentDecoder = null;
                throw new InvalidOperationException("Unexpected decode result (see your decode()): " + result);
            }
            catch (Exception)
            {
                state.CurrentDecoder = null;
                throw;
            }
        }

        private State GetState(IOSession session)
        {
            var state = session.GetAttribute<State>(_state);

            if (state == null)
            {
                state = new State(_decoderFactories);
                var oldState = (State)session.SetAttributeIfAbsent(_state, state);

                if (oldState != null)
                {
                    state = oldState;
                }
            }

            return state;
        }

        class State
        {
            public readonly IMessageDecoder[] Decoders;
            public IMessageDecoder CurrentDecoder;

            public State(IMessageDecoderFactory[] decoderFactories)
            {
                Decoders = new IMessageDecoder[decoderFactories.Length];
                for (var i = decoderFactories.Length - 1; i >= 0; i--)
                {
                    Decoders[i] = decoderFactories[i].GetDecoder();
                }
            }
        }

        class SingletonMessageDecoderFactory : IMessageDecoderFactory
        {
            private readonly IMessageDecoder _decoder;

            public SingletonMessageDecoderFactory(IMessageDecoder decoder)
            {
                if (decoder == null)
                    throw new ArgumentNullException(nameof(decoder));
                this._decoder = decoder;
            }

            public IMessageDecoder GetDecoder()
            {
                return _decoder;
            }
        }

        class DefaultConstructorMessageDecoderFactory : IMessageDecoderFactory
        {
            private readonly Type _decoderType;

            public DefaultConstructorMessageDecoderFactory(Type decoderType)
            {
                this._decoderType = decoderType;
            }

            public IMessageDecoder GetDecoder()
            {
                return (IMessageDecoder)Activator.CreateInstance(_decoderType);
            }
        }
    }
}
