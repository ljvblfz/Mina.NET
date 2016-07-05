using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which decodes <code>int</code> values
    /// in big-endian order (high bytes come first).
    /// </summary>
    public abstract class IntegerDecodingState : IDecodingState
    {
        private int _firstByte;
        private int _secondByte;
        private int _thirdByte;
        private int _counter;

        public IDecodingState Decode(IOBuffer input, IProtocolDecoderOutput output)
        {
            while (input.HasRemaining)
            {
                switch (_counter)
                {
                    case 0:
                        _firstByte = input.Get() & 0xff;
                        break;
                    case 1:
                        _secondByte = input.Get() & 0xff;
                        break;
                    case 2:
                        _thirdByte = input.Get() & 0xff;
                        break;
                    case 3:
                        _counter = 0;
                        return FinishDecode((_firstByte << 24) | (_secondByte << 16) | (_thirdByte << 8) | (input.Get() & 0xff), output);
                }

                _counter++;
            }
            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            throw new ProtocolDecoderException("Unexpected end of session while waiting for a integer.");
        }

        /// <summary>
        /// Invoked when this state has consumed a complete <code>int</code>.
        /// </summary>
        /// <param name="value">the integer</param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(int value, IProtocolDecoderOutput output);
    }
}
