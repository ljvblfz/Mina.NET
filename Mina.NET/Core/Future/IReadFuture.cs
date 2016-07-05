using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IOFuture"/> for asynchronous read requests.
    /// </summary>
    public interface IReadFuture : IOFuture
    {
        /// <summary>
        /// Gets or sets the received message.
        /// </summary>
        /// <remarks>
        /// Returns null if this future is not ready or the associated
        /// <see cref="IOSession"/> has been closed.
        /// All threads waiting for will be notified while being set.
        /// </remarks>
        object Message { get; set; }
        /// <summary>
        /// Returns <code>true</code> if a message was received successfully.
        /// </summary>
        bool Read { get; }
        /// <summary>
        /// Gets or sets a value indicating if the <see cref="IOSession"/>
        /// associated with this future has been closed.
        /// </summary>
        bool Closed { get; set; }
        /// <summary>
        /// Gets or sets the cause of the read failure if and only if the read
        /// operation has failed due to an <see cref="Exception"/>.
        /// Otherwise null is returned.
        /// </summary>
        Exception Exception { get; set; }
        /// <inheritdoc/>
        new IReadFuture Await();
    }
}
