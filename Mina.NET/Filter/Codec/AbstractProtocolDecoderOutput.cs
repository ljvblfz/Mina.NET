using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A <see cref="IProtocolDecoderOutput"/> based on queue.
    /// </summary>
    public abstract class AbstractProtocolDecoderOutput : IProtocolDecoderOutput
    {
        public IQueue<object> MessageQueue { get; } = new Queue<object>();

        /// <inheritdoc/>
        public void Write(object message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            MessageQueue.Enqueue(message);
        }

        /// <inheritdoc/>
        public abstract void Flush(INextFilter nextFilter, IOSession session);
    }
}
