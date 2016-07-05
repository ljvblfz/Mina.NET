using System;
using System.Text;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// Provides utility methods to dump an <see cref="IOBuffer"/> into a hex formatted string.
    /// </summary>
    public static class IOBufferHexDumper
    {
        private static readonly char[] HighDigits;
        private static readonly char[] LowDigits;

        static IOBufferHexDumper()
        {
            char[] digits = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

            var high = new char[256];
            var low = new char[256];

            for (var i = 0; i < 256; i++)
            {
                high[i] = digits[i >> 4];
                low[i] = digits[i & 0x0F];
            }

            HighDigits = high;
            LowDigits = low;
        }

        public static string GetHexdump(IOBuffer buf, int lengthLimit)
        {
            if (lengthLimit <= 0)
            {
                throw new ArgumentException("lengthLimit: " + lengthLimit + " (expected: 1+)");
            }
            var truncate = buf.Remaining > lengthLimit;
            var size = truncate ? lengthLimit : buf.Remaining;

            if (size == 0)
            {
                return "empty";
            }

            var sb = new StringBuilder(size*3 + 3);
            var oldPos = buf.Position;

            // fill the first
            var byteValue = buf.Get() & 0xFF;
            sb.Append(HighDigits[byteValue]);
            sb.Append(LowDigits[byteValue]);
            size--;

            // and the others, too
            for (; size > 0; size--)
            {
                sb.Append(' ');
                byteValue = buf.Get() & 0xFF;
                sb.Append(HighDigits[byteValue]);
                sb.Append(LowDigits[byteValue]);
            }

            buf.Position = oldPos;

            if (truncate)
            {
                sb.Append("...");
            }

            return sb.ToString();
        }
    }
}
