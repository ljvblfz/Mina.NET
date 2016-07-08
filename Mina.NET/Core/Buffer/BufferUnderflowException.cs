using System;
using System.Runtime.Serialization;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// An exception thrown when a <code>Get</code> operation reaches the source buffer's limit.
    /// </summary>
    [Serializable]
    public class BufferUnderflowException : Exception
    {
        /// <summary>
        /// </summary>
        public BufferUnderflowException()
        {
        }

        /// <summary>
        /// </summary>
        public BufferUnderflowException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public BufferUnderflowException(string message, Exception inner)
            : base(message, inner)
        {
        }

        /// <summary>
        /// </summary>
        protected BufferUnderflowException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
