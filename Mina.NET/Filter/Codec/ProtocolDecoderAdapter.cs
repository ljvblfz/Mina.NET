using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An abstract <see cref="IProtocolDecoder"/> implementation for those who don't need
    /// FinishDecode(IoSession, ProtocolDecoderOutput) nor
    /// Dispose(IoSession) method.
    /// </summary>
    public abstract class ProtocolDecoderAdapter : IProtocolDecoder
    {
        /// <inheritdoc/>
        public abstract void Decode(IOSession session, IOBuffer input, IProtocolDecoderOutput output);

        /// <inheritdoc/>
        public virtual void FinishDecode(IOSession session, IProtocolDecoderOutput output)
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public virtual void Dispose(IOSession session)
        {
            // Do nothing
        }
    }
}
