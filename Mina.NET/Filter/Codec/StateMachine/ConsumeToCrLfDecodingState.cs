using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which consumes all bytes until a <code>CRLF</code> 
    /// has been encountered.
    /// </summary>
    public abstract class ConsumeToCrLfDecodingState : IDecodingState
    {
        /// <summary>
        /// Carriage return character
        /// </summary>
        private static readonly byte Cr = 13;
        /// <summary>
        /// Line feed character
        /// </summary>
        private static readonly byte Lf = 10;
        private bool _lastIsCr;
        private IOBuffer _buffer;

        public IDecodingState Decode(IOBuffer input, IProtocolDecoderOutput output)
        {
            var beginPos = input.Position;
            var limit = input.Limit;
            var terminatorPos = -1;

            for (var i = beginPos; i < limit; i++)
            {
                var b = input.Get(i);
                if (b == Cr)
                {
                    _lastIsCr = true;
                }
                else
                {
                    if (b == Lf && _lastIsCr)
                    {
                        terminatorPos = i;
                        break;
                    }
                    _lastIsCr = false;
                }
            }

            if (terminatorPos >= 0)
            {
                IOBuffer product;

                var endPos = terminatorPos - 1;

                if (beginPos < endPos)
                {
                    input.Limit = endPos;

                    if (_buffer == null)
                    {
                        product = input.Slice();
                    }
                    else
                    {
                        _buffer.Put(input);
                        product = _buffer.Flip();
                        _buffer = null;
                    }

                    input.Limit = limit;
                }
                else
                {
                    // When input contained only CR or LF rather than actual data...
                    if (_buffer == null)
                    {
                        product = IOBuffer.Allocate(0);
                    }
                    else
                    {
                        product = _buffer.Flip();
                        _buffer = null;
                    }
                }
                input.Position = terminatorPos + 1;
                return FinishDecode(product, output);
            }

            input.Position = beginPos;

            if (_buffer == null)
            {
                _buffer = IOBuffer.Allocate(input.Remaining);
                _buffer.AutoExpand = true;
            }

            _buffer.Put(input);

            if (_lastIsCr)
            {
                _buffer.Position = _buffer.Position - 1;
            }

            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            IOBuffer product;
            // When input contained only CR or LF rather than actual data...
            if (_buffer == null)
            {
                product = IOBuffer.Allocate(0);
            }
            else
            {
                product = _buffer.Flip();
                _buffer = null;
            }
            return FinishDecode(product, output);
        }

        /// <summary>
        /// Invoked when this state has consumed all bytes until the session is closed.
        /// </summary>
        /// <param name="product">the read bytes including the <code>CRLF</code></param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(IOBuffer product, IProtocolDecoderOutput output);
    }
}
