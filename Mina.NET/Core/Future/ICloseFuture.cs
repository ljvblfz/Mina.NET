using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IOFuture"/> for asynchronous close requests.
    /// </summary>
    public interface ICloseFuture : IOFuture
    {
        /// <summary>
        /// Gets or sets a value indicating if the close request is finished and
        /// the associated <see cref="IOSession"/> been closed.
        /// </summary>
        bool Closed { get; set; }
        /// <inheritdoc/>
        new ICloseFuture Await();
    }
}
