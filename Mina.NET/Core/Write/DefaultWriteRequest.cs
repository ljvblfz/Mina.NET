using System;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Write
{
    public class DefaultWriteRequest : IWriteRequest
    {
        /// <summary>
        /// An empty message.
        /// </summary>
        public static readonly byte[] EmptyMessage = new byte[0];

        public DefaultWriteRequest(object message)
            : this(message, null, null)
        { }

        public DefaultWriteRequest(object message, IWriteFuture future)
            : this(message, future, null)
        { }

        public DefaultWriteRequest(object message, IWriteFuture future, EndPoint destination)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            Message = message;
            Future = future ?? UnusedFuture.Instance;
            Destination = destination;
        }

        /// <inheritdoc/>
        public IWriteRequest OriginalRequest => this;

        /// <inheritdoc/>
        public object Message { get; }

        /// <inheritdoc/>
        public EndPoint Destination { get; }

        /// <inheritdoc/>
        public IWriteFuture Future { get; }

        /// <inheritdoc/>
        public virtual bool Encoded => false;

        class UnusedFuture : IWriteFuture
        {
            public static readonly UnusedFuture Instance = new UnusedFuture();

            public event EventHandler<IoFutureEventArgs> Complete
            {
                add { throw new NotSupportedException(); }
                remove { throw new NotSupportedException(); }
            }

            public bool Written
            {
                get { return false; }
                set { }
            }

            public Exception Exception
            {
                get { return null; }
                set { }
            }

            public IOSession Session => null;

            public bool Done => true;

            public IWriteFuture Await()
            {
                return this;
            }

            public bool Await(int timeoutMillis)
            {
                return true;
            }

            IOFuture IOFuture.Await()
            {
                return Await();
            }
        }
    }
}
