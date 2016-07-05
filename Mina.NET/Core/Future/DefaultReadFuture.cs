using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IReadFuture"/>.
    /// </summary>
    public class DefaultReadFuture : DefaultIOFuture, IReadFuture
    {
        private static readonly object CLOSED = new object();

        /// <summary>
        /// </summary>
        public DefaultReadFuture(IOSession session)
            : base(session)
        { }

        /// <inheritdoc/>
        public object Message
        {
            get
            {
                if (Done)
                {
                    var val = Value;
                    if (ReferenceEquals(val, CLOSED))
                        return null;
                    var ex = val as Exception;
                    if (ex != null)
                        throw ex;
                    return val;
                }

                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                Value = value;
            }
        }

        /// <inheritdoc/>
        public bool Read
        {
            get
            {
                if (Done)
                {
                    var val = Value;
                    return !ReferenceEquals(val, CLOSED) && !(val is Exception);
                }
                return false;
            }
        }

        /// <inheritdoc/>
        public bool Closed
        {
            get { return Done && ReferenceEquals(Value, CLOSED); }
            set { Value = CLOSED; }
        }

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
                    throw new ArgumentNullException(nameof(value));
                Value = value;
            }
        }

        /// <inheritdoc/>
        public new IReadFuture Await()
        {
            return (IReadFuture)base.Await();
        }
    }
}
