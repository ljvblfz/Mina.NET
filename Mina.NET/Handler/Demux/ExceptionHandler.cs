using System;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// Default implementation of <see cref="IExceptionHandler"/>.
    /// </summary>
    public class ExceptionHandler<TE> : IExceptionHandler<TE> where TE : Exception
    {
        public static readonly IExceptionHandler<Exception> Noop = new NoopExceptionHandler();
        public static readonly IExceptionHandler<Exception> Close = new CloseExceptionHandler();

        private readonly Action<IOSession, TE> _act;

        /// <summary>
        /// </summary>
        public ExceptionHandler()
        {
        }

        /// <summary>
        /// </summary>
        public ExceptionHandler(Action<IOSession, TE> act)
        {
            if (act == null)
            {
                throw new ArgumentNullException(nameof(act));
            }
            _act = act;
        }

        /// <inheritdoc/>
        public virtual void ExceptionCaught(IOSession session, TE cause)
        {
            if (_act != null)
            {
                _act(session, cause);
            }
        }

        void IExceptionHandler.ExceptionCaught(IOSession session, Exception cause)
        {
            ExceptionCaught(session, (TE) cause);
        }
    }

    class NoopExceptionHandler : IExceptionHandler<Exception>
    {
        internal NoopExceptionHandler()
        {
        }

        public void ExceptionCaught(IOSession session, Exception cause)
        {
            // Do nothing
        }
    }

    class CloseExceptionHandler : IExceptionHandler<Exception>
    {
        internal CloseExceptionHandler()
        {
        }

        public void ExceptionCaught(IOSession session, Exception cause)
        {
            session.Close(true);
        }
    }
}
