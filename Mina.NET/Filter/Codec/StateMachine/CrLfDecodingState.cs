using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which decodes a single <code>CRLF</code>.
    /// If it is found, the bytes are consumed and <code>true</code>
    /// is provided as the product. Otherwise, read bytes are pushed back
    /// to the stream, and <code>false</code> is provided as the
    /// product.
    /// Note that if we find a CR but do not find a following LF, we raise
    /// an error.
    /// </summary>
    public abstract class CrLfDecodingState : IDecodingState
    {
        /// <summary>
        /// Carriage return character
        /// </summary>
        private static readonly byte Cr = 13;

        /// <summary>
        /// Line feed character
        /// </summary>
        private static readonly byte Lf = 10;

        private bool _hasCr;

        public IDecodingState Decode(IOBuffer input, IProtocolDecoderOutput output)
        {
            var found = false;
            var finished = false;
            while (input.HasRemaining)
            {
                var b = input.Get();
                if (!_hasCr)
                {
                    if (b == Cr)
                    {
                        _hasCr = true;
                    }
                    else
                    {
                        if (b == Lf)
                        {
                            found = true;
                        }
                        else
                        {
                            input.Position = input.Position - 1;
                            found = false;
                        }
                        finished = true;
                        break;
                    }
                }
                else
                {
                    if (b == Lf)
                    {
                        found = true;
                        finished = true;
                        break;
                    }

                    throw new ProtocolDecoderException("Expected LF after CR but was: " + (b & 0xff));
                }
            }

            if (finished)
            {
                _hasCr = false;
                return FinishDecode(found, output);
            }

            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            return FinishDecode(false, output);
        }

        /// <summary>
        /// Invoked when this state has found a <code>CRLF</code>.
        /// </summary>
        /// <param name="foundCrlf"><code>true</code> if <code>CRLF</code> was found</param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(bool foundCrlf, IProtocolDecoderOutput output);
    }
}
