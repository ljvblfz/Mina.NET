using System;
using System.Text;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A <see cref="IProtocolDecoder"/> which decodes a text line into a string.
    /// </summary>
    public class TextLineDecoder : IProtocolDecoder
    {
        private readonly AttributeKey _context;
        private readonly Encoding _encoding;
        private readonly LineDelimiter _delimiter;
        private int _maxLineLength = 1024;
        private int _bufferLength = 128;
        private byte[] _delimBuf;

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and <see cref="LineDelimiter.Auto"/>.
        /// </summary>
        public TextLineDecoder()
            : this(LineDelimiter.Auto)
        { }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineDecoder(string delimiter)
            : this(new LineDelimiter(delimiter))
        { }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineDecoder(LineDelimiter delimiter)
            : this(Encoding.Default, delimiter)
        { }

        /// <summary>
        /// Instantiates with given encoding,
        /// and default <see cref="LineDelimiter.Auto"/>.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        public TextLineDecoder(Encoding encoding)
            : this(encoding, LineDelimiter.Auto)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineDecoder(Encoding encoding, string delimiter)
            : this(encoding, new LineDelimiter(delimiter))
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineDecoder(Encoding encoding, LineDelimiter delimiter)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (delimiter == null)
                throw new ArgumentNullException(nameof(delimiter));

            _context = new AttributeKey(GetType(), "context");
            _encoding = encoding;
            _delimiter = delimiter;

            _delimBuf = encoding.GetBytes(delimiter.Value);
        }

        /// <summary>
        /// Gets or sets the max length allowed for a line.
        /// </summary>
        public int MaxLineLength
        {
            get { return _maxLineLength; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("maxLineLength (" + value + ") should be a positive value");
                _maxLineLength = value;
            }
        }

        /// <summary>
        /// Gets or sets the lenght of inner buffer.
        /// </summary>
        public int BufferLength
        {
            get { return _bufferLength; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("bufferLength (" + value + ") should be a positive value");
                _bufferLength = value;
            }
        }

        /// <inheritdoc/>
        public void Decode(IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            var ctx = GetContext(session);

            if (LineDelimiter.Auto.Equals(_delimiter))
                DecodeAuto(ctx, session, input, output);
            else
                DecodeNormal(ctx, session, input, output);
        }

        /// <inheritdoc/>
        public void FinishDecode(IOSession session, IProtocolDecoderOutput output)
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public void Dispose(IOSession session)
        {
            session.RemoveAttribute(_context);
        }

        /// <summary>
        /// By default, this method propagates the decoded line of text to <see cref="IProtocolDecoderOutput"/>.
        /// You may override this method to modify the default behavior.
        /// </summary>
        protected virtual void WriteText(IOSession session, string text, IProtocolDecoderOutput output)
        {
            output.Write(text);
        }

        private void DecodeAuto(Context ctx, IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            var matchCount = ctx.MatchCount;

            // Try to find a match
            int oldPos = input.Position, oldLimit = input.Limit;

            while (input.HasRemaining)
            {
                var b = input.Get();
                var matched = false;
                
                switch (b)
                {
                    case 0x0d: // \r
                        // Might be Mac, but we don't auto-detect Mac EOL
                        // to avoid confusion.
                        matchCount++;
                        break;
                    case 0x0a: // \n
                        // UNIX
                        matchCount++;
                        matched = true;
                        break;
                    default:
                        matchCount = 0;
                        break;
                }

                if (matched)
                {
                    // Found a match.
                    var pos = input.Position;
                    input.Limit = pos;
                    input.Position = oldPos;

                    ctx.Append(input);

                    input.Limit = oldLimit;
                    input.Position = pos;

                    if (ctx.OverflowPosition == 0)
                    {
                        var buf = ctx.Buffer;
                        buf.Flip();
                        buf.Limit -= matchCount;
                        var bytes = buf.GetRemaining();
                        try
                        {
                            var str = _encoding.GetString(bytes.Array, bytes.Offset, bytes.Count);
                            WriteText(session, str, output);
                        }
                        finally
                        {
                            buf.Clear();
                        }
                    }
                    else
                    {
                        var overflowPosition = ctx.OverflowPosition;
                        ctx.Reset();
                        throw new RecoverableProtocolDecoderException("Line is too long: " + overflowPosition);
                    }

                    oldPos = pos;
                    matchCount = 0;
                }
            }

            // Put remainder to buf.
            input.Position = oldPos;
            ctx.Append(input);
            ctx.MatchCount = matchCount;
        }

        private void DecodeNormal(Context ctx, IOSession session, IOBuffer input, IProtocolDecoderOutput output)
        {
            var matchCount = ctx.MatchCount;

            // Try to find a match
            int oldPos = input.Position, oldLimit = input.Limit;

            while (input.HasRemaining)
            {
                var b = input.Get();

                if (_delimBuf[matchCount] == b)
                {
                    matchCount++;

                    if (matchCount == _delimBuf.Length)
                    {
                        // Found a match.
                        var pos = input.Position;
                        input.Limit = pos;
                        input.Position = oldPos;

                        ctx.Append(input);

                        input.Limit = oldLimit;
                        input.Position = pos;

                        if (ctx.OverflowPosition == 0)
                        {
                            var buf = ctx.Buffer;
                            buf.Flip();
                            buf.Limit -= matchCount;
                            var bytes = buf.GetRemaining();
                            try
                            {
                                var str = _encoding.GetString(bytes.Array, bytes.Offset, bytes.Count);
                                WriteText(session, str, output);
                            }
                            finally
                            {
                                buf.Clear();
                            }
                        }
                        else
                        {
                            var overflowPosition = ctx.OverflowPosition;
                            ctx.Reset();
                            throw new RecoverableProtocolDecoderException("Line is too long: " + overflowPosition);
                        }

                        oldPos = pos;
                        matchCount = 0;
                    }
                }
                else
                {
                    input.Position = Math.Max(0, input.Position - matchCount);
                    matchCount = 0;
                }
            }

            // Put remainder to buf.
            input.Position = oldPos;
            ctx.Append(input);
            ctx.MatchCount = matchCount;
        }

        private Context GetContext(IOSession session)
        {
            var ctx = session.GetAttribute<Context>(_context);
            if (ctx == null)
            {
                ctx = new Context(this);
                session.SetAttribute(_context, ctx);
            }
            return ctx;
        }

        class Context
        {
            private readonly TextLineDecoder _textLineDecoder;

            public Context(TextLineDecoder textLineDecoder)
            {
                _textLineDecoder = textLineDecoder;
                Buffer = IOBuffer.Allocate(_textLineDecoder.BufferLength);
                Buffer.AutoExpand = true;
            }

            public int MatchCount { get; set; }

            public IOBuffer Buffer { get; }

            public int OverflowPosition { get; private set; }

            public void Reset()
            {
                OverflowPosition = 0;
                MatchCount = 0;
            }

            public void Append(IOBuffer input)
            {
                if (OverflowPosition != 0)
                {
                    Discard(input);
                }
                else if (Buffer.Position > _textLineDecoder.MaxLineLength - input.Remaining)
                {
                    OverflowPosition = Buffer.Position;
                    Buffer.Clear();
                    Discard(input);
                }
                else
                {
                    Buffer.Put(input);
                }
            }

            private void Discard(IOBuffer input)
            {
                if (int.MaxValue - input.Remaining < OverflowPosition)
                    OverflowPosition = int.MaxValue;
                else
                    OverflowPosition += input.Remaining;
                input.Position = input.Limit;
            }
        }
    }
}
