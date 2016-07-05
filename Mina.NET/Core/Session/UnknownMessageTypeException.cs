
namespace Mina.Core.Session
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// An exception that is thrown when the type of the message cannot be determined.
    /// </summary>
    [Serializable]
    public class UnknownMessageTypeException : Exception
    {
        /// <summary>
        /// </summary>
        public UnknownMessageTypeException() { }

        /// <summary>
        /// </summary>
        public UnknownMessageTypeException(string message)
            : base(message) { }

        /// <summary>
        /// </summary>
        public UnknownMessageTypeException(string message, Exception inner)
            : base(message, inner) { }

        /// <summary>
        /// </summary>
        protected UnknownMessageTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context) { }
    }
}
