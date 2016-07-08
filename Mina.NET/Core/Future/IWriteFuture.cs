using System;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IOFuture"/> for asynchronous write requests.
    /// </summary>
    public interface IWriteFuture : IOFuture
    {
        /// <summary>
        /// Gets or sets a value indicating if this write operation is finished successfully.
        /// </summary>
        bool Written { get; set; }

        /// <summary>
        /// Gets or sets the cause of the write failure if and only if the write
        /// operation has failed due to an <see cref="Exception"/>.
        /// Otherwise null is returned.
        /// </summary>
        Exception Exception { get; set; }

        /// <inheritdoc/>
        new IWriteFuture Await();
    }
}
