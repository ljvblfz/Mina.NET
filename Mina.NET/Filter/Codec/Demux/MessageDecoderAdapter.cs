using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// An abstract <see cref="IMessageDecoder"/> implementation for those who don't need to
    /// implement <code>FinishDecode(IoSession, IProtocolDecoderOutput)</code> method.
    /// </summary>
    public abstract class MessageDecoderAdapter : IMessageDecoder
    {
        public abstract MessageDecoderResult Decodable(IOSession session, IOBuffer input);

        public abstract MessageDecoderResult Decode(IOSession session, IOBuffer input, IProtocolDecoderOutput output);

        public virtual void FinishDecode(IOSession session, IProtocolDecoderOutput output)
        {
            // Do nothing
        }
    }
}
