using System;
using System.Net;
using Mina.Core.Future;

namespace Mina.Core.Write
{
    /// <summary>
    /// A wrapper for an existing <see cref="IWriteRequest"/>.
    /// </summary>
    public class WriteRequestWrapper : IWriteRequest
    {
        /// <summary>
        /// </summary>
        public WriteRequestWrapper(IWriteRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            InnerRequest = request;
        }

        /// <inheritdoc/>
        public IWriteRequest OriginalRequest => InnerRequest.OriginalRequest;

        /// <inheritdoc/>
        public virtual object Message => InnerRequest.Message;

        /// <inheritdoc/>
        public EndPoint Destination => InnerRequest.Destination;

        /// <inheritdoc/>
        public bool Encoded => InnerRequest.Encoded;

        /// <inheritdoc/>
        public IWriteFuture Future => InnerRequest.Future;

        public IWriteRequest InnerRequest { get; }
    }
}
