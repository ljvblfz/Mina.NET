using System.Net;
using Mina.Core.Service;

namespace Mina.Core.Session
{
    /// <summary>
    ///  A connectionless transport can recycle existing sessions by assigning an
    ///  <see cref="IOSessionRecycler"/> to an <see cref="IOService"/>.
    /// </summary>
    public interface IOSessionRecycler
    {
        /// <summary>
        /// Called when the underlying transport creates or writes a new <see cref="IOSession"/>.
        /// </summary>
        /// <param name="session"></param>
        void Put(IOSession session);
        /// <summary>
        /// Attempts to retrieve a recycled <see cref="IOSession"/>.
        /// </summary>
        /// <param name="remoteEp">the remote endpoint of the <see cref="IOSession"/> the transport wants to recycle</param>
        /// <returns>a recycled <see cref="IOSession"/>, or null if one cannot be found</returns>
        IOSession Recycle(EndPoint remoteEp);
        /// <summary>
        /// Called when an <see cref="IOSession"/> is explicitly closed.
        /// </summary>
        /// <param name="session"></param>
        void Remove(IOSession session);
    }

    /// <summary>
    /// A dummy recycler that doesn't recycle any sessions.
    /// Using this recycler will make all session lifecycle events
    /// to be fired for every I/O for all connectionless sessions.
    /// </summary>
    public class NoopRecycler : IOSessionRecycler
    {
        /// <summary>
        /// A dummy recycler that doesn't recycle any sessions.
        /// </summary>
        public static readonly NoopRecycler Instance = new NoopRecycler();

        private NoopRecycler()
        { }

        /// <inheritdoc/>
        public void Put(IOSession session)
        {
            // do nothing
        }

        /// <inheritdoc/>
        public IOSession Recycle(EndPoint remoteEp)
        {
            return null;
        }

        /// <inheritdoc/>
        public void Remove(IOSession session)
        {
            // do nothing
        }
    }
}
