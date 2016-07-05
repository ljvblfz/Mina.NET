using System;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// A handler interface that <see cref="DemuxingIoHandler"/> forwards
    /// <code>ExceptionCaught</code> events to.
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// Invoked when the specific type of exception is caught from the
        /// specified <code>session</code>.
        /// </summary>
        void ExceptionCaught(IOSession session, Exception cause);
    }

    /// <summary>
    /// A handler interface that <see cref="DemuxingIoHandler"/> forwards
    /// <code>ExceptionCaught</code> events to.
    /// </summary>
    /// <typeparam name="TE"></typeparam>
    public interface IExceptionHandler<in TE> : IExceptionHandler where TE : Exception
    {
        /// <summary>
        /// Invoked when the specific type of exception is caught from the
        /// specified <code>session</code>.
        /// </summary>
        void ExceptionCaught(IOSession session, TE cause);
    }
}
