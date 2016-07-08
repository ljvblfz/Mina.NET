using System;
using System.Runtime.Serialization;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A special exception that tells the <see cref="IProtocolDecoder"/> can keep
    /// decoding even after this exception is thrown.
    /// </summary>
    [Serializable]
    public class RecoverableProtocolDecoderException : ProtocolDecoderException
    {
        public RecoverableProtocolDecoderException()
        {
        }

        public RecoverableProtocolDecoderException(string message)
            : base(message)
        {
        }

        public RecoverableProtocolDecoderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected RecoverableProtocolDecoderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
