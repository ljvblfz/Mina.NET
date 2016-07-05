using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Encodes higher-level message objects into binary or protocol-specific data.
    /// </summary>
    public interface IProtocolEncoder
    {
        /// <summary>
        /// Encodes higher-level message objects into binary or protocol-specific data.
        /// </summary>
        void Encode(IOSession session, object message, IProtocolEncoderOutput output);
        /// <summary>
        /// Releases all resources related with this encoder.
        /// </summary>
        void Dispose(IOSession session);
    }
}
