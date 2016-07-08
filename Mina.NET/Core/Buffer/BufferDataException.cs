using System;
using System.Runtime.Serialization;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// An exception which is thrown when the data the <see cref="IOBuffer"/> contains is corrupt.
    /// </summary>
    [Serializable]
    public class BufferDataException : Exception
    {
        /// <summary>
        /// </summary>
        public BufferDataException()
        {
        }

        /// <summary>
        /// </summary>
        public BufferDataException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public BufferDataException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// </summary>
        protected BufferDataException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
