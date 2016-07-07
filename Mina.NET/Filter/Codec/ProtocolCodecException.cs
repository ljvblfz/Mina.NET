using System;
using System.Runtime.Serialization;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolEncoder"/> or
    /// <see cref="IProtocolDecoder"/> cannot understand or failed to validate
    /// data to process.
    /// </summary>
    [Serializable]
    public class ProtocolCodecException : Exception
    {
        /// <summary>
        /// </summary>
        public ProtocolCodecException()
        {
        }

        /// <summary>
        /// </summary>
        public ProtocolCodecException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public ProtocolCodecException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// </summary>
        protected ProtocolCodecException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolEncoder"/>
    /// cannot understand or failed to validate the specified message object.
    /// </summary>
    [Serializable]
    public class ProtocolEncoderException : ProtocolCodecException
    {
        /// <summary>
        /// </summary>
        public ProtocolEncoderException()
        {
        }

        /// <summary>
        /// </summary>
        public ProtocolEncoderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public ProtocolEncoderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// </summary>
        protected ProtocolEncoderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolDecoder"/>
    /// cannot understand or failed to validate the specified <see cref="Core.Buffer.IOBuffer"/>
    /// content.
    /// </summary>
    [Serializable]
    public class ProtocolDecoderException : ProtocolCodecException
    {
        private string _hexdump;

        /// <summary>
        /// </summary>
        public ProtocolDecoderException()
        {
        }

        /// <summary>
        /// </summary>
        public ProtocolDecoderException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public ProtocolDecoderException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// </summary>
        protected ProtocolDecoderException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the current data in hex.
        /// </summary>
        public string Hexdump
        {
            get { return _hexdump; }
            set
            {
                if (_hexdump != null)
                {
                    throw new InvalidOperationException("Hexdump cannot be set more than once.");
                }
                _hexdump = value;
            }
        }
    }
}
