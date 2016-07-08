using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Write
{
    /// <summary>
    /// Represents write request fired by <see cref="IOSession.Write(object)"/>.
    /// </summary>
    public interface IWriteRequest
    {
        /// <summary>
        /// Gets the <see cref="IWriteRequest"/> which was requested originally,
        /// which is not transformed by any <see cref="IOFilter"/>.
        /// </summary>
        IWriteRequest OriginalRequest { get; }

        /// <summary>
        /// Gets the message object to be written.
        /// </summary>
        object Message { get; }

        /// <summary>
        /// Gets the destination of this write request.
        /// </summary>
        EndPoint Destination { get; }

        /// <summary>
        /// Tells if the current message has been encoded.
        /// </summary>
        bool Encoded { get; }

        /// <summary>
        /// Gets <see cref="IWriteFuture"/> that is associated with this write request.
        /// </summary>
        IWriteFuture Future { get; }
    }
}
