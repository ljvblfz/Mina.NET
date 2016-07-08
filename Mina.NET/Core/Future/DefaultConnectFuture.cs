using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IConnectFuture"/>.
    /// </summary>
    public class DefaultConnectFuture : DefaultIOFuture, IConnectFuture
    {
        private static readonly object CANCELED = new object();

        /// <summary>
        /// Returns a new <see cref="IConnectFuture"/> which is already marked as 'failed to connect'.
        /// </summary>
        public static IConnectFuture NewFailedFuture(Exception exception)
        {
            var failedFuture = new DefaultConnectFuture();
            failedFuture.Exception = exception;
            return failedFuture;
        }

        /// <summary>
        /// </summary>
        public DefaultConnectFuture()
            : base(null)
        {
        }

        /// <inheritdoc/>
        public bool Connected => Value is IOSession;

        /// <inheritdoc/>
        public bool Canceled => ReferenceEquals(Value, CANCELED);

        /// <inheritdoc/>
        public Exception Exception
        {
            get
            {
                if (Done)
                {
                    return Value as Exception;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                Value = value;
            }
        }

        /// <inheritdoc/>
        public override IOSession Session
        {
            get
            {
                var val = Value;
                var ex = val as Exception;
                if (ex != null)
                {
                    throw ex;
                }
                return val as IOSession;
            }
        }

        /// <inheritdoc/>
        public void SetSession(IOSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }
            Value = session;
        }

        /// <inheritdoc/>
        public virtual bool Cancel()
        {
            return SetValue(CANCELED);
        }

        /// <inheritdoc/>
        public new IConnectFuture Await()
        {
            return (IConnectFuture) base.Await();
        }
    }
}
