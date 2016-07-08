using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// Represents the completion of an asynchronous I/O operation on an <see cref="IOSession"/>.
    /// </summary>
    public interface IOFuture
    {
        /// <summary>
        /// Gets the <see cref="IOSession"/> which is associated with this future.
        /// </summary>
        IOSession Session { get; }

        /// <summary>
        /// Returns if the asynchronous operation is completed.
        /// </summary>
        bool Done { get; }

        /// <summary>
        /// Event that this future is completed.
        /// If the listener is added after the completion, the listener is directly notified.
        /// </summary>
        event EventHandler<IoFutureEventArgs> Complete;

        /// <summary>
        /// Wait for the asynchronous operation to complete.
        /// </summary>
        /// <returns>self</returns>
        IOFuture Await();

        /// <summary>
        /// Wait for the asynchronous operation to complete with the specified timeout.
        /// </summary>
        /// <returns><tt>true</tt> if the operation is completed</returns>
        bool Await(int millisecondsTimeout);
    }

    /// <summary>
    /// Contains data for events of <see cref="IOFuture"/>.
    /// </summary>
    public class IoFutureEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        public IoFutureEventArgs(IOFuture future)
        {
            Future = future;
        }

        /// <summary>
        /// Gets the associated future.
        /// </summary>
        public IOFuture Future { get; }
    }
}
