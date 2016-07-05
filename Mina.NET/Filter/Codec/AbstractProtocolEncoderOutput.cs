using System;
using Mina.Util;
using Mina.Core.Future;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A <see cref="IProtocolEncoderOutput"/> based on queue.
    /// </summary>
    public abstract class AbstractProtocolEncoderOutput : IProtocolEncoderOutput
    {
        private bool _buffersOnly = true;

        public IQueue<object> MessageQueue { get; } = new ConcurrentQueue<object>();

        /// <inheritdoc/>
        public void Write(object encodedMessage)
        {
            var buf = encodedMessage as IOBuffer;
            if (buf == null)
            {
                _buffersOnly = false;
            }
            else if (!buf.HasRemaining)
            {
                throw new ArgumentException("buf is empty. Forgot to call flip()?");
            }
            MessageQueue.Enqueue(encodedMessage);
        }

        /// <inheritdoc/>
        public void MergeAll()
        {
            if (!_buffersOnly)
                throw new InvalidOperationException("The encoded messages contains a non-buffer.");

            if (MessageQueue.Count < 2)
                // no need to merge!
                return;

            var sum = 0;
            foreach (var item in MessageQueue)
            {
                sum += ((IOBuffer)item).Remaining;
            }

            var newBuf = IOBuffer.Allocate(sum);
            for (; ; )
            {
                var obj = MessageQueue.Dequeue();
                if (obj == null)
                    break;
                newBuf.Put((IOBuffer)obj);
            }

            newBuf.Flip();
            MessageQueue.Enqueue(newBuf);
        }

        /// <inheritdoc/>
        public abstract IWriteFuture Flush();
    }
}
