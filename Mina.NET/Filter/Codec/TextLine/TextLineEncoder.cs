using System;
using System.Text;
using Mina.Core.Session;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A <see cref="IProtocolEncoder"/> which encodes a string into a text line
    /// which ends with the delimiter.
    /// </summary>
    public class TextLineEncoder : IProtocolEncoder
    {
        private readonly Encoding _encoding;
        private readonly LineDelimiter _delimiter;
        private int _maxLineLength = int.MaxValue;

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and <see cref="LineDelimiter.Unix"/>.
        /// </summary>
        public TextLineEncoder()
            : this(LineDelimiter.Unix)
        {
        }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineEncoder(string delimiter)
            : this(new LineDelimiter(delimiter))
        {
        }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineEncoder(LineDelimiter delimiter)
            : this(Encoding.Default, delimiter)
        {
        }

        /// <summary>
        /// Instantiates with given encoding,
        /// and default <see cref="LineDelimiter.Unix"/>.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        public TextLineEncoder(Encoding encoding)
            : this(encoding, LineDelimiter.Unix)
        {
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineEncoder(Encoding encoding, string delimiter)
            : this(encoding, new LineDelimiter(delimiter))
        {
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineEncoder(Encoding encoding, LineDelimiter delimiter)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }
            if (delimiter == null)
            {
                throw new ArgumentNullException(nameof(delimiter));
            }
            if (LineDelimiter.Auto.Equals(delimiter))
            {
                throw new ArgumentException("AUTO delimiter is not allowed for encoder.");
            }

            _encoding = encoding;
            _delimiter = delimiter;
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of the encoded line.
        /// </summary>
        public int MaxLineLength
        {
            get { return _maxLineLength; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("maxLineLength (" + value + ") should be a positive value");
                }
                _maxLineLength = value;
            }
        }

        /// <inheritdoc/>
        public void Encode(IOSession session, object message, IProtocolEncoderOutput output)
        {
            var value = message == null ? string.Empty : message.ToString();
            value += _delimiter.Value;
            var bytes = _encoding.GetBytes(value);
            if (bytes.Length > _maxLineLength)
            {
                throw new ArgumentException("Line too long: " + bytes.Length);
            }

            // TODO BufferAllocator
            var buf = IOBuffer.Wrap(bytes);
            output.Write(buf);
        }

        /// <inheritdoc/>
        public void Dispose(IOSession session)
        {
            // Do nothing
        }
    }
}
