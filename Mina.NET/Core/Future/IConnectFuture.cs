using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IOFuture"/> for asynchronous connect requests.
    /// </summary>
    public interface IConnectFuture : IOFuture
    {
        /// <summary>
        /// Returns <code>true</code> if the connect operation is finished successfully.
        /// </summary>
        bool Connected { get; }
        /// <summary>
        /// Returns <code>true</code> if the connect operation has been
        /// canceled by <see cref="Cancel()"/>.
        /// </summary>
        bool Canceled { get; }
        /// <summary>
        /// Gets or sets the cause of the connection failure.
        /// </summary>
        /// <remarks>
        /// Returns null if the connect operation is not finished yet,
        /// or if the connection attempt is successful.
        /// </remarks>
        Exception Exception { get; set; }
        /// <summary>
        /// Sets the newly connected session and notifies all threads waiting for this future.
        /// </summary>
        /// <param name="session"></param>
        void SetSession(IOSession session);
        /// <summary>
        /// Cancels the connection attempt and notifies all threads waiting for this future.
        /// <returns>
        /// <code>true</code> if the future has been cancelled by this call,
        /// <code>false</code>if the future was already cancelled.
        /// </returns>
        /// </summary>
        bool Cancel();
        /// <inheritdoc/>
        new IConnectFuture Await();
    }
}
