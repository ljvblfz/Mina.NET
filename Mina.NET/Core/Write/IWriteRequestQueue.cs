using Mina.Core.Session;

namespace Mina.Core.Write
{
    /// <summary>
    /// Stores <see cref="IWriteRequest"/>s which are queued to an <see cref="IOSession"/>.
    /// </summary>
    public interface IWriteRequestQueue
    {
        /// <summary>
        /// Gets the first request available in the queue for a session.
        /// </summary>
        /// <param name="session">the associated session</param>
        /// <returns>the first available request, if any.</returns>
        IWriteRequest Poll(IOSession session);
        /// <summary>
        /// Adds a new WriteRequest to the session write's queue
        /// </summary>
        /// <param name="session">the associated session</param>
        /// <param name="writeRequest">the writeRequest to add</param>
        void Offer(IOSession session, IWriteRequest writeRequest);
        /// <summary>
        /// Tells if the WriteRequest queue is empty or not for a session.
        /// </summary>
        /// <param name="session">the associated session</param>
        /// <returns><code>true</code> if the writeRequest queue is empty</returns>
        bool IsEmpty(IOSession session);
        /// <summary>
        /// Removes all the requests from this session's queue.
        /// </summary>
        /// <param name="session">the associated session</param>
        void Clear(IOSession session);
        /// <summary>
        /// Disposes any releases associated with the specified session.
        /// This method is invoked on disconnection.
        /// </summary>
        /// <param name="session">the associated session</param>
        void Dispose(IOSession session);
        /// <summary>
        /// Gets number of objects currently stored in the queue.
        /// </summary>
        int Size { get; }
    }
}
