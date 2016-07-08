using System;
using System.Collections.Concurrent;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IProtocolDecoder"/> which uses a <see cref="IDecodingState"/> to decode data.
    /// Use a <see cref="DecodingStateMachine"/> as <see cref="IDecodingState"/> to create
    /// a state machine which can decode your protocol.
    /// </summary>
    public class DecodingStateProtocolDecoder : IProtocolDecoder
    {
        private readonly IDecodingState _state;
        private readonly ConcurrentQueue<IOBuffer> _undecodedBuffers = new ConcurrentQueue<IOBuffer>();
        private IOSession _session;

        /// <summary>
        /// Creates a new instance using the specified <see cref="IDecodingState"/>.
        /// </summary>
        /// <param name="state">the <see cref="IDecodingState"/></param>
        /// <exception cref="ArgumentNullException">if the specified state is <code>null</code></exception>
        public DecodingStateProtocolDecoder(IDecodingState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            _state = state;
        }

        public void Decode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            if (_session == null)
            {
                _session = session;
            }
            else if (_session != session)
            {
                throw new InvalidOperationException(GetType().Name + " is a stateful decoder.  "
                                                    + "You have to create one per session.");
            }

            _undecodedBuffers.Enqueue(input);
            while (true)
            {
                IOBuffer b;
                if (!_undecodedBuffers.TryPeek(out b))
                {
                    break;
                }

                var oldRemaining = b.Remaining;
                _state.Decode(b, output);
                var newRemaining = b.Remaining;
                if (newRemaining != 0)
                {
                    if (oldRemaining == newRemaining)
                    {
                        throw new InvalidOperationException(_state.GetType().Name
                                                            + " must consume at least one byte per decode().");
                    }
                }
                else
                {
                    _undecodedBuffers.TryDequeue(out b);
                }
            }
        }

        public void FinishDecode(IOSession session, IProtocolDecoderOutput output)
        {
            _state.FinishDecode(output);
        }

        public void Dispose(IOSession session)
        {
            // Do nothing
        }
    }
}
