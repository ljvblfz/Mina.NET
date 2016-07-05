using System;

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
        { }

        /// <summary>
        /// </summary>
        public IOFilterLifeCycleException(string message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        public IOFilterLifeCycleException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// </summary>
        protected IOFilterLifeCycleException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        /// <summary>
        /// </summary>
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
