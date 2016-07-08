using System;
using Mina.Core.Future;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Callback for <see cref="IProtocolEncoder"/> to generate encoded messages such as
    /// <see cref="Core.Buffer.IOBuffer"/>s.  <see cref="IProtocolEncoder"/> must call #write(Object)
    /// for each encoded message.
    /// </summary>
    public interface IProtocolEncoderOutput
    {
        /// <summary>
        /// Callback for <see cref="IProtocolEncoder"/> to generate encoded messages such as
        /// <see cref="Core.Buffer.IOBuffer"/>s.  <see cref="IProtocolEncoder"/> must call #write(Object)
        /// for each encoded message.
        /// </summary>
        void Write(object encodedMessage);

        /// <summary>
        /// Merges all buffers you wrote via <see cref="Write(object)"/> into one <see cref="Core.Buffer.IOBuffer"/>
        /// and replaces the old fragmented ones with it.
        /// This method is useful when you want to control the way Mina.NET generates network packets.
        /// <remarks>
        /// Please note that this method only works when you called <see cref="Write(object)"/> method
        /// with only <see cref="Core.Buffer.IOBuffer"/>s.
        /// </remarks>
        /// </summary>
        /// <exception cref="InvalidOperationException">if you wrote something else than <see cref="Core.Buffer.IOBuffer"/></exception>
        void MergeAll();

        /// <summary>
        /// Flushes all buffers you wrote via <see cref="Write(object)"/> to the session.
        /// This operation is asynchronous; please wait for the returned <see cref="IWriteFuture"/>
        /// if you want to wait for the buffers flushed.
        /// </summary>
        /// <returns><code>null</code> if there is nothing to flush at all</returns>
        IWriteFuture Flush();
    }
}
