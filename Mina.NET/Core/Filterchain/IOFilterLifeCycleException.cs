using System;
using System.Runtime.Serialization;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An exception thrown when <see cref="IOFilter.Init()"/> or
    /// <see cref="IOFilter.OnPostAdd(IOFilterChain, string, INextFilter)"/> failed.
    /// </summary>
    [Serializable]
    public class IOFilterLifeCycleException : Exception
    {
        /// <summary>
        /// </summary>
        public IOFilterLifeCycleException()
        {
        }

        /// <summary>
        /// </summary>
        public IOFilterLifeCycleException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// </summary>
        public IOFilterLifeCycleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// </summary>
        protected IOFilterLifeCycleException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
