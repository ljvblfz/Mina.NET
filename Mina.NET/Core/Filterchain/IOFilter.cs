using System;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A filter which intercepts <see cref="IOHandler"/> events like Servlet filters.
    /// </summary>
    public interface IOFilter
    {
        /// <summary>
        /// Invoked by <see cref="Filter.Util.ReferenceCountingFilter"/> when this filter
        /// is added to a <see cref="IOFilterChain"/> at the first time, so you can
        /// initialize shared resources.  Please note that this method is never
        /// called if you don't wrap a filter with <see cref="Filter.Util.ReferenceCountingFilter"/>.
        /// </summary>
        void Init();
        /// <summary>
        /// Invoked by <see cref="Filter.Util.ReferenceCountingFilter"/> when this filter
        /// is not used by any <see cref="IOFilterChain"/> anymore, so you can destroy
        /// shared resources.  Please note that this method is never called if
        /// you don't wrap a filter with <see cref="Filter.Util.ReferenceCountingFilter"/>.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Invoked before this filter is added to the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is added to more than one parents. This method is not
        /// invoked before <see cref="Init()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPreAdd(IOFilterChain parent, string name, INextFilter nextFilter);
        /// <summary>
        /// Invoked after this filter is added to the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is added to more than one parents. This method is not
        /// invoked before <see cref="Init()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPostAdd(IOFilterChain parent, string name, INextFilter nextFilter);
        /// <summary>
        /// Invoked before this filter is removed from the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is removed from more than one parents.
        /// This method is always invoked before <see cref="Destroy()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPreRemove(IOFilterChain parent, string name, INextFilter nextFilter);
        /// <summary>
        /// Invoked after this filter is removed from the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is removed from more than one parents.
        /// This method is always invoked before <see cref="Destroy()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPostRemove(IOFilterChain parent, string name, INextFilter nextFilter);

        /// <summary>
        /// Filters <see cref="IOHandler.SeIOSessionted(IoSession)"/> event.
        /// </summary>
        void SessionCreated(INextFilter nextFilter, IOSession session);
        /// <summary>
        /// Filters <see cref="IOHandler.SIOSessionned(IoSession)"/> event.
        /// </summary>
        void SessionOpened(INextFilter nextFilter, IOSession session);
        /// <summary>
        /// Filters <see cref="IOHandler.SIOSessionsed(IoSession)"/> event.
        /// </summary>
        void SessionClosed(INextFilter nextFilter, IOSession session);
        /// <summary>
        /// Filters <see cref="IOHandlerIOSessiondle(IoSession, IdleStatus)"/> event.
        /// </summary>
        void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status);
        /// <summary>
        /// Filters <see cref="IOHandler.ExcIOSessionght(IoSession, Exception)"/> event.
        /// </summary>
        void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause);
        /// <summary>
        /// Filters <see cref="IOHandlerIOSessionsed(IoSession)"/> event.
        /// </summary>
        /// <param name="nextFilter">
        /// The <see cref="INextFilter"/> for this filter.
        /// You can reuse this object until this filter is removed from the chain.
        /// </param>
        /// <param name="session">The <see cref="IOSession"/> which has received this event.</param>
        void InputClosed(INextFilter nextFilter, IOSession session);
        /// <summary>
        /// Filters <see cref="IOHandler.MesIOSessionved(IoSession, object)"/> event.
        /// </summary>
        void MessageReceived(INextFilter nextFilter, IOSession session, object message);
        /// <summary>
        /// Filters <see cref="IOHandlerIOSessionent(IoSession, object)"/> event.
        /// </summary>
        void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest);
        /// <summary>
        /// Filters <see cref="IOSession.Close(bool)"/> event.
        /// </summary>
        void FilterClose(INextFilter nextFilter, IOSession session);
        /// <summary>
        /// Filters <see cref="IOSession.Write(object)"/> event.
        /// </summary>
        void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest);
    }
}
