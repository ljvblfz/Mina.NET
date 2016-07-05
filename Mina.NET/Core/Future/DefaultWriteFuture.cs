using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IWriteFuture"/>.
    /// </summary>
    public class DefaultWriteFuture : DefaultIOFuture, IWriteFuture
    {
        /// <summary>
        /// Returns a new <see cref="DefaultWriteFuture"/> which is already marked as 'written'.
        /// </summary>
        public static IWriteFuture NewWrittenFuture(IOSession session)
        {
            var writtenFuture = new DefaultWriteFuture(session);
            writtenFuture.Written = true;
            return writtenFuture;
        }

        /// <summary>
        /// Returns a new <see cref="DefaultWriteFuture"/> which is already marked as 'not written'.
        /// </summary>
        public static IWriteFuture NewNotWrittenFuture(IOSession session, Exception cause)
        {
            var unwrittenFuture = new DefaultWriteFuture(session);
            unwrittenFuture.Exception = cause;
            return unwrittenFuture;
        }

        /// <summary>
        /// </summary>
        public DefaultWriteFuture(IOSession session)
            : base(session)
        { }

        /// <inheritdoc/>
        public bool Written
        {
            get
            {
                if (Done)
                {
                    var v = Value;
                    if (v is bool)
                        return (bool)v;
                }
                return false;
            }
            set { Value = true; }
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
        public new IWriteFuture Await()
        {
            return (IWriteFuture)base.Await();
        }
    }
}
