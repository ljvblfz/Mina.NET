using System;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A default implementation of <see cref="IOFilterChain"/> that provides
    /// all operations for developers who want to implement their own
    /// transport layer once used with <see cref="AbstractIOSession"/>.
    /// </summary>
    public class VirtualDefaultIOFilterChain : Chain<VirtualDefaultIOFilterChain, IOFilter, INextFilter>, IOFilterChain
    {
        private readonly static ILog Log = LogManager.GetLogger(typeof(DefaultIOFilterChain));

        private readonly AbstractIOSession _session;

        /// <summary>
        /// </summary>
        public VirtualDefaultIOFilterChain(AbstractIOSession session)
            : base(
            e => new NextFilter(e),
            () => new HeadFilter(), () => new TailFilter()
            )
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            _session = session;
        }

        /// <summary>
        /// Gets the <see cref="IOSession"/> this chain belongs to.
        /// </summary>
        public IOSession Session => _session;

        /// <inheritdoc/>
        public virtual void FireSessionCreated()
        {
            CallNextSessionCreated(Head, _session);
        }

        /// <inheritdoc/>
        public virtual void FireSessionOpened()
        {
            CallNextSessionOpened(Head, _session);
        }

        /// <inheritdoc/>
        public virtual void FireSessionClosed()
        {
            // update future
            try
            {
                _session.CloseFuture.Closed = true;
            }
            catch (Exception e)
            {
                FireExceptionCaught(e);
            }

            // And start the chain.
            CallNextSessionClosed(Head, _session);
        }

        /// <inheritdoc/>
        public virtual void FireSessionIdle(IdleStatus status)
        {
            _session.IncreaseIdleCount(status, DateTime.Now);
            CallNextSessionIdle(Head, _session, status);
        }

        /// <inheritdoc/>
        public virtual void FireMessageReceived(object message)
        {
            var buf = message as IOBuffer;
            if (buf != null)
                _session.IncreaseReadBytes(buf.Remaining, DateTime.Now);

            CallNextMessageReceived(Head, _session, message);
        }

        /// <inheritdoc/>
        public virtual void FireMessageSent(IWriteRequest request)
        {
            _session.IncreaseWrittenMessages(request, DateTime.Now);

            try
            {
                request.Future.Written = true;
            }
            catch (Exception e)
            {
                FireExceptionCaught(e);
            }

            if (!request.Encoded)
            {
                CallNextMessageSent(Head, _session, request);
            }
        }

        /// <inheritdoc/>
        public virtual void FireExceptionCaught(Exception cause)
        {
            CallNextExceptionCaught(Head, _session, cause);
        }

        /// <inheritdoc/>
        public virtual void FireInputClosed()
        {
            CallNextInputClosed(Head, _session);
        }

        /// <inheritdoc/>
        public virtual void FireFilterWrite(IWriteRequest writeRequest)
        {
            CallPreviousFilterWrite(Tail, _session, writeRequest);
        }

        /// <inheritdoc/>
        public virtual void FireFilterClose()
        {
            CallPreviousFilterClose(Tail, _session);
        }

        private void CallNext(IEntry<IOFilter, INextFilter> entry, Action<IOFilter, INextFilter> act, Action<Exception> error = null)
        {
            try
            {
                var filter = entry.Filter;
                var nextFilter = entry.NextFilter;
                act(filter, nextFilter);
            }
            catch (Exception e)
            {
                if (error == null)
                    FireExceptionCaught(e);
                else
                    error(e);
            }
        }

        private void CallPrevious(IEntry<IOFilter, INextFilter> entry, Action<IOFilter, INextFilter> act, Action<Exception> error = null)
        {
            try
            {
                var filter = entry.Filter;
                var nextFilter = entry.NextFilter;
                act(filter, nextFilter);
            }
            catch (Exception e)
            {
                if (error == null)
                    FireExceptionCaught(e);
                else
                    error(e);
            }
        }

        private void CallNextSessionCreated(IEntry<IOFilter, INextFilter> entry, IOSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionCreated(next, session));
        }

        private void CallNextSessionOpened(IEntry<IOFilter, INextFilter> entry, IOSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionOpened(next, session));
        }

        private void CallNextSessionClosed(IEntry<IOFilter, INextFilter> entry, IOSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionClosed(next, session));
        }

        private void CallNextSessionIdle(IEntry<IOFilter, INextFilter> entry, IOSession session, IdleStatus status)
        {
            CallNext(entry, (filter, next) => filter.SessionIdle(next, session, status));
        }

        private void CallNextExceptionCaught(IEntry<IOFilter, INextFilter> entry, IOSession session, Exception cause)
        {
            // Notify the related future.
            var future = session.RemoveAttribute(DefaultIOFilterChain.SessionCreatedFuture) as IConnectFuture;
            if (future == null)
            {
                CallNext(entry, (filter, next) => filter.ExceptionCaught(next, session, cause),
                    e => Log.Warn("Unexpected exception from exceptionCaught handler.", e));
            }
            else
            {
                // Please note that this place is not the only place that
                // calls ConnectFuture.setException().
                session.Close(true);
                future.Exception = cause;
            }
        }

        private void CallNextInputClosed(IEntry<IOFilter, INextFilter> entry, IOSession session)
        {
            CallNext(entry, (filter, next) => filter.InputClosed(next, session));
        }

        private void CallNextMessageReceived(IEntry<IOFilter, INextFilter> entry, IOSession session, object message)
        {
            CallNext(entry, (filter, next) => filter.MessageReceived(next, session, message));
        }

        private void CallNextMessageSent(IEntry<IOFilter, INextFilter> entry, IOSession session, IWriteRequest writeRequest)
        {
            CallNext(entry, (filter, next) => filter.MessageSent(next, session, writeRequest));
        }

        private void CallPreviousFilterClose(IEntry<IOFilter, INextFilter> entry, IOSession session)
        {
            CallPrevious(entry, (filter, next) => filter.FilterClose(next, session));
        }

        private void CallPreviousFilterWrite(IEntry<IOFilter, INextFilter> entry, IOSession session, IWriteRequest writeRequest)
        {
            CallPrevious(entry, (filter, next) => filter.FilterWrite(next, session, writeRequest),
                e =>
                {
                    writeRequest.Future.Exception = e;
                    FireExceptionCaught(e);
                });
        }

        /// <inheritdoc/>
        public new void Clear()
        {
            try
            {
                base.Clear();
            }
            catch (Exception e)
            {
                throw new IOFilterLifeCycleException("Clear(): in " + Session, e);
                //throw new IoFilterLifeCycleException("Clear(): " + entry.Name + " in " + Session, e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPreAdd(Entry entry)
        {
            try
            {
                entry.Filter.OnPreAdd(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IOFilterLifeCycleException("OnPreAdd(): " + entry.Name + ':' + entry.Filter + " in " + Session, e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPostAdd(Entry entry)
        {
            try
            {
                entry.Filter.OnPostAdd(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                Deregister0(entry);
                throw new IOFilterLifeCycleException("OnPostAdd(): " + entry.Name + ':' + entry.Filter + " in " + Session, e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPreRemove(Entry entry)
        {
            try
            {
                entry.Filter.OnPreRemove(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IOFilterLifeCycleException("OnPreRemove(): " + entry.Name + ':' + entry.Filter + " in "
                        + Session, e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPostRemove(Entry entry)
        {
            try
            {
                entry.Filter.OnPostRemove(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IOFilterLifeCycleException("OnPostRemove(): " + entry.Name + ':' + entry.Filter + " in "
                        + Session, e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPreReplace(Entry entry, IOFilter newFilter)
        {
            // Call the preAdd method of the new filter
            try
            {
                newFilter.OnPreAdd(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IOFilterLifeCycleException("OnPreAdd(): " + entry.Name + ':' + newFilter + " in " + Session, e);
            }
        }

        /// <inheritdoc/>
        protected override void OnPostReplace(Entry entry, IOFilter newFilter)
        {
            // Call the postAdd method of the new filter
            try
            {
                newFilter.OnPostAdd(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IOFilterLifeCycleException("OnPostAdd(): " + entry.Name + ':' + newFilter + " in " + Session, e);
            }
        }

        class HeadFilter : IOFilterAdapter
        {
            public override void FilterWrite(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
            {
                var s = session as AbstractIOSession;
                if (s != null)
                {
                    // Maintain counters.
                    var buffer = writeRequest.Message as IOBuffer;
                    if (buffer != null)
                    {
                        // I/O processor implementation will call buffer.Reset()
                        // it after the write operation is finished, because
                        // the buffer will be specified with messageSent event.
                        buffer.Mark();
                        var remaining = buffer.Remaining;
                        if (remaining == 0)
                            // Zero-sized buffer means the internal message delimiter
                            s.IncreaseScheduledWriteMessages();
                        else
                            s.IncreaseScheduledWriteBytes(remaining);
                    }
                    else
                        s.IncreaseScheduledWriteMessages();
                }

                var writeRequestQueue = session.WriteRequestQueue;

                if (session.WriteSuspended)
                {
                    writeRequestQueue.Offer(session, writeRequest);
                }
                else if (writeRequestQueue.IsEmpty(session))
                {
                    // We can write directly the message
                    session.Processor.Write(session, writeRequest);
                }
                else
                {
                    writeRequestQueue.Offer(session, writeRequest);
                    session.Processor.Flush(session);
                }
            }

            public override void FilterClose(INextFilter nextFilter, IOSession session)
            {
                session.Processor.Remove(session);
            }
        }

        class TailFilter : IOFilterAdapter
        {
            public override void SessionCreated(INextFilter nextFilter, IOSession session)
            {
                try
                {
                    session.Handler.SessionCreated(session);
                }
                finally
                {
                    var future = session.RemoveAttribute(DefaultIOFilterChain.SessionCreatedFuture) as IConnectFuture;
                    if (future != null)
                        future.SetSession(session);
                }
            }

            public override void SessionOpened(INextFilter nextFilter, IOSession session)
            {
                session.Handler.SessionOpened(session);
            }

            public override void SessionClosed(INextFilter nextFilter, IOSession session)
            {
                var s = session as AbstractIOSession;
                try
                {
                    session.Handler.SessionClosed(session);
                }
                finally
                {
                    try { session.WriteRequestQueue.Dispose(session); }
                    finally
                    {
                        try { s.AttributeMap.Dispose(session); }
                        finally
                        {
                            session.FilterChain.Clear();
                            // TODO IsUseReadOperation
                        }
                    }
                }
            }

            public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
            {
                session.Handler.SessionIdle(session, status);
            }

            public override void ExceptionCaught(INextFilter nextFilter, IOSession session, Exception cause)
            {
                session.Handler.ExceptionCaught(session, cause);
                // TODO IsUseReadOperation
            }

            public override void InputClosed(INextFilter nextFilter, IOSession session)
            {
                session.Handler.InputClosed(session);
            }

            public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
            {
                var s = session as AbstractIOSession;
                if (s != null)
                {
                    var buf = message as IOBuffer;
                    if (buf == null || !buf.HasRemaining)
                        s.IncreaseReadMessages(DateTime.Now);
                }

                // Update the statistics
                session.Service.Statistics.UpdateThroughput(DateTime.Now);

                // Propagate the message
                session.Handler.MessageReceived(session, message);
                // TODO IsUseReadOperation
            }

            public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
            {
                var s = session as AbstractIOSession;
                if (s != null)
                {
                    s.IncreaseWrittenMessages(writeRequest, DateTime.Now);
                }

                // Update the statistics
                session.Service.Statistics.UpdateThroughput(DateTime.Now);

                // Propagate the message
                session.Handler.MessageSent(session, writeRequest.Message);
            }
        }

        class NextFilter : INextFilter
        {
            readonly VirtualDefaultIOFilterChain _chain;
            readonly Entry _entry;

            public NextFilter(Entry entry)
            {
                _chain = entry.Chain;
                _entry = entry;
            }

            public void SessionCreated(IOSession session)
            {
                _chain.CallNextSessionCreated(_entry.NextEntry, session);
            }

            public void SessionOpened(IOSession session)
            {
                _chain.CallNextSessionOpened(_entry.NextEntry, session);
            }

            public void SessionClosed(IOSession session)
            {
                _chain.CallNextSessionClosed(_entry.NextEntry, session);
            }

            public void SessionIdle(IOSession session, IdleStatus status)
            {
                _chain.CallNextSessionIdle(_entry.NextEntry, session, status);
            }

            public void ExceptionCaught(IOSession session, Exception cause)
            {
                _chain.CallNextExceptionCaught(_entry.NextEntry, session, cause);
            }

            public void InputClosed(IOSession session)
            {
                _chain.CallNextInputClosed(_entry.NextEntry, session);
            }

            public void MessageReceived(IOSession session, object message)
            {
                _chain.CallNextMessageReceived(_entry.NextEntry, session, message);
            }

            public void MessageSent(IOSession session, IWriteRequest writeRequest)
            {
                _chain.CallNextMessageSent(_entry.NextEntry, session, writeRequest);
            }

            public void FilterWrite(IOSession session, IWriteRequest writeRequest)
            {
                _chain.CallPreviousFilterWrite(_entry.PrevEntry, session, writeRequest);
            }

            public void FilterClose(IOSession session)
            {
                _chain.CallPreviousFilterClose(_entry.PrevEntry, session);
            }

            public override string ToString()
            {
                return _entry.NextEntry == null ? "null" : _entry.NextEntry.Name;
            }
        }
    }
}
