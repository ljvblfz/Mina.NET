using System;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An exception thrown when <see cref="IOFilter.Init()"/> or
    /// <see cref="IOFilter.OnPostAdd(IOFilterChain, string, INextFilter)"/> failed.
    /// </summary>
    [Serializable]
    public class IoFilterLifeCycleException : Exception
    {
        /// <summary>
        /// </summary>
        public IoFilterLifeCycleException()
        { }

        /// <summary>
        /// </summary>
        public IoFilterLifeCycleException(string message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        public IoFilterLifeCycleException(string message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// </summary>
        protected IoFilterLifeCycleException(
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
