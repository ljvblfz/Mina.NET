using System;
using System.Text;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A delimiter which is appended to the end of a text line, such as
    /// <tt>CR/LF</tt>. This class defines default delimiters for various OS.
    /// </summary>
    public class LineDelimiter
    {
        /// <summary>
        /// The line delimiter constant of the current O/S.
        /// </summary>
        public static readonly LineDelimiter Default = new LineDelimiter(Environment.NewLine);

        /// <summary>
        /// A special line delimiter which is used for auto-detection of
        /// EOL in <see cref="TextLineDecoder"/>.  If this delimiter is used,
        /// <see cref="TextLineDecoder"/> will consider both  <tt>'\r'</tt> and
        /// <tt>'\n'</tt> as a delimiter.
        /// </summary>
        public static readonly LineDelimiter Auto = new LineDelimiter(string.Empty);

        /// <summary>
        /// The CRLF line delimiter constant (<tt>"\r\n"</tt>)
        /// </summary>
        public static readonly LineDelimiter Crlf = new LineDelimiter("\r\n");

        /// <summary>
        /// The line delimiter constant of UNIX (<tt>"\n"</tt>)
        /// </summary>
        public static readonly LineDelimiter Unix = new LineDelimiter("\n");

        /// <summary>
        /// The line delimiter constant of MS Windows/DOS (<tt>"\r\n"</tt>)
        /// </summary>
        public static readonly LineDelimiter Windows = Crlf;

        /// <summary>
        /// The line delimiter constant of Mac OS (<tt>"\r"</tt>)
        /// </summary>
        public static readonly LineDelimiter Mac = new LineDelimiter("\r");

        /// <summary>
        /// The line delimiter constant for NUL-terminated text protocols
        /// such as Flash XML socket (<tt>"\0"</tt>)
        /// </summary>
        public static readonly LineDelimiter Nul = new LineDelimiter("\0");

        /// <summary>
        /// Creates a new line delimiter with the specified <tt>delimiter</tt>.
        /// </summary>
        public LineDelimiter(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            Value = value;
        }

        /// <summary>
        /// Gets the delimiter string.
        /// </summary>
        public string Value { get; }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var that = obj as LineDelimiter;
            if (that == null)
                return false;
            return Value.Equals(that.Value);
        }

        public override string ToString()
        {
            if (Value.Length == 0)
                return "delimiter: auto";
            var buf = new StringBuilder();
            buf.Append("delimiter:");

            for (var i = 0; i < Value.Length; i++)
            {
                buf.Append(" 0x");
                buf.AppendFormat("{0:X}", Value[i]);
            }

            return buf.ToString();
        }
    }
}
