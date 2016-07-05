using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="ICloseFuture"/>.
    /// </summary>
    public class DefaultCloseFuture : DefaultIoFuture, ICloseFuture
    {
        /// <summary>
        /// </summary>
        public DefaultCloseFuture(IOSession session)
            : base(session)
        { }

        /// <inheritdoc/>
        public bool Closed
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
        public new ICloseFuture Await()
        {
            return (ICloseFuture)base.Await();
        }
    }
}
