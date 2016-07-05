using System;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// A handle which represents connection between two end-points regardless of transport types.
    /// </summary>
    public interface IOSession
    {
        /// <summary>
        /// Gets a unique identifier for this session.
        /// </summary>
        long Id { get; }
        /// <summary>
        /// Gets the configuration of this session.
        /// </summary>
        IOSessionConfig Config { get; }
        /// <summary>
        /// Gets the <see cref="IOService"/> which provides I/O service to this session.
        /// </summary>
        IOService Service { get; }
        /// <summary>
        /// Gets the associated <see cref="IOProcessor"/> for this session.
        /// </summary>
        IOProcessor Processor { get; }
        /// <summary>
        /// Gets the <see cref="IOHandler"/> which handles this session.
        /// </summary>
        IOHandler Handler { get; }
        /// <summary>
        /// Gets the filter chain that only affects this session.
        /// </summary>
        IOFilterChain FilterChain { get; }
        IWriteRequestQueue WriteRequestQueue { get; }
        /// <summary>
        /// Gets the <see cref="ITransportMetadata"/> that this session runs on.
        /// </summary>
        ITransportMetadata TransportMetadata { get; }
        /// <summary>
        /// Returns <code>true</code> if this session is connected with remote peer.
        /// </summary>
        bool Connected { get; }
        /// <summary>
        /// Returns <code>true</code> if and only if this session is being closed.
        /// </summary>
        bool Closing { get; }
        /// <summary>
        /// Returns <code>true</code> if the session has started with SSL,
        /// <code>false</code> if the session is not yet secured (the handshake is not completed)
        /// or if SSL is not set for this session, or if SSL is not even an option.
        /// </summary>
        bool Secured { get; }
        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        EndPoint LocalEndPoint { get; }
        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        EndPoint RemoteEndPoint { get; }
        /// <summary>
        /// Gets the <see cref="ICloseFuture"/> of this session.
        /// This method returns the same instance whenever user calls it.
        /// </summary>
        ICloseFuture CloseFuture { get; }
        /// <summary>
        /// Writes the specified <code>message</code> to remote peer.
        /// This operation is asynchronous.
        /// </summary>
        IWriteFuture Write(object message);
        /// <summary>
        /// Writes the specified <code>message</code> to the specified destination.
        /// This operation is asynchronous.
        /// </summary>
        IWriteFuture Write(object message, EndPoint remoteEp);
        /// <summary>
        /// Closes this session immediately or after all queued write requests
        /// are flushed. This operation is asynchronous.
        /// </summary>
        /// <param name="rightNow">true to close this session immediately,
        /// discarding the pending write requests; false to close this session
        /// after all queued write requests are flushed.</param>
        /// <returns></returns>
        ICloseFuture Close(bool rightNow);
        /// <summary>
        /// Gets the value of the user-defined attribute of this session.
        /// </summary>
        /// <typeparam name="T">the type of the attribute</typeparam>
        /// <param name="key">the key of the attribute</param>
        /// <returns><code>null</code> if there is no attribute with the specified key</returns>
        T GetAttribute<T>(object key);
        /// <summary>
        /// Gets the value of the user-defined attribute of this session.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns><code>null</code> if there is no attribute with the specified key</returns>
        object GetAttribute(object key);
        /// <summary>
        ///  Sets a user-defined attribute.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <param name="value">the value of the attribute</param>
        /// <returns>the old value of the attribute, or <code>null</code> if it is new</returns>
        object SetAttribute(object key, object value);
        /// <summary>
        /// Sets a user defined attribute without a value.
        /// This is useful when you just want to put a 'mark' attribute.
        /// Its value is set to <code>true</code>.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns>the old value of the attribute, or <code>null</code> if it is new</returns>
        object SetAttribute(object key);
        /// <summary>
        /// Sets a user defined attribute if the attribute with the specified key
        /// is not set yet.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <param name="value">the value of the attribute</param>
        /// <returns>the old value of the attribute, or <code>null</code> if it is new</returns>
        object SetAttributeIfAbsent(object key, object value);
        /// <summary>
        /// Removes a user-defined attribute with the specified key.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns>the old value of the attribute, or <code>null</code> if not found</returns>
        object RemoveAttribute(object key);
        /// <summary>
        /// Returns <code>true</code> if this session contains the attribute with
        /// the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns>true or false</returns>
        bool ContainsAttribute(object key);

        /// <summary>
        /// Checks if write operation is suspended for this session. 
        /// </summary>
        bool WriteSuspended { get; }
        /// <summary>
        /// Checks if read operation is suspended for this session. 
        /// </summary>
        bool ReadSuspended { get; }
        /// <summary>
        /// Suspends read operations for this session.
        /// </summary>
        void SuspendRead();
        /// <summary>
        /// Suspends write operations for this session.
        /// </summary>
        void SuspendWrite();
        /// <summary>
        /// Resumes read operations for this session.
        /// </summary>
        void ResumeRead();
        /// <summary>
        /// Resumes write operations for this session.
        /// </summary>
        void ResumeWrite();

        /// <summary>
        /// Gets or sets the <see cref="IWriteRequest"/> which is being processed by <see cref="IOService"/>.
        /// </summary>
        IWriteRequest CurrentWriteRequest { get; set; }

        /// <summary>
        /// Gets the total number of bytes which were read from this session.
        /// </summary>
        long ReadBytes { get; }
        /// <summary>
        /// Gets the total number of bytes which were written to this session.
        /// </summary>
        long WrittenBytes { get; }
        /// <summary>
        /// Gets the total number of messages which were read and decoded from this session.
        /// </summary>
        long ReadMessages { get; }
        /// <summary>
        /// Gets the total number of messages which were written and encoded by this session.
        /// </summary>
        long WrittenMessages { get; }
        /// <summary>
        /// Gets the number of read bytes per second.
        /// </summary>
        double ReadBytesThroughput { get; }
        /// <summary>
        /// Gets the number of written bytes per second.
        /// </summary>
        double WrittenBytesThroughput { get; }
        /// <summary>
        /// Gets the number of read messages per second.
        /// </summary>
        double ReadMessagesThroughput { get; }
        /// <summary>
        /// Gets the number of written messages per second.
        /// </summary>
        double WrittenMessagesThroughput { get; }
        /// <summary>
        /// Gets the session's creation time.
        /// </summary>
        DateTime CreationTime { get; }
        /// <summary>
        /// Gets the time when I/O occurred lastly.
        /// </summary>
        DateTime LastIoTime { get; }
        /// <summary>
        /// Gets the time when read operation occurred lastly.
        /// </summary>
        DateTime LastReadTime { get; }
        /// <summary>
        /// Gets the time when write operation occurred lastly.
        /// </summary>
        DateTime LastWriteTime { get; }
        /// <summary>
        /// Returns <code>true</code> if this session is idle for the
        /// specified <see cref="IdleStatus"/>.
        /// </summary>
        bool IsIdle(IdleStatus status);
        /// <summary>
        /// Checks if this session is <see cref="IdleStatus.ReaderIdle"/>.
        /// </summary>
        bool IsReaderIdle { get; }
        /// <summary>
        /// Checks if this session is <see cref="IdleStatus.WriterIdle"/>.
        /// </summary>
        bool IsWriterIdle { get; }
        /// <summary>
        /// Checks if this session is <see cref="IdleStatus.BothIdle"/>.
        /// </summary>
        bool IsBothIdle { get; }
        /// <summary>
        /// Gets the number of the fired continuous <code>SessionIdle</code> events
        /// for the specified <see cref="IdleStatus"/>.
        /// </summary>
        int GetIdleCount(IdleStatus status);
        /// <summary>
        /// Gets the number of the fired continuous <code>SessionIdle</code> events
        /// for <see cref="IdleStatus.ReaderIdle"/>.
        /// </summary>
        int ReaderIdleCount { get; }
        /// <summary>
        /// Gets the number of the fired continuous <code>SessionIdle</code> events
        /// for <see cref="IdleStatus.WriterIdle"/>.
        /// </summary>
        int WriterIdleCount { get; }
        /// <summary>
        /// Gets the number of the fired continuous <code>SessionIdle</code> events
        /// for <see cref="IdleStatus.BothIdle"/>.
        /// </summary>
        int BothIdleCount { get; }
        /// <summary>
        ///  Returns the time when the last <code>SessionIdle</code> event
        ///  is fired for the specified <see cref="IdleStatus"/>.
        /// </summary>
        DateTime GetLastIdleTime(IdleStatus status);
        /// <summary>
        ///  Gets the time when the last <code>SessionIdle</code> event
        ///  is fired for <see cref="IdleStatus.ReaderIdle"/>.
        /// </summary>
        DateTime LastReaderIdleTime { get; }
        /// <summary>
        ///  Gets the time when the last <code>SessionIdle</code> event
        ///  is fired for <see cref="IdleStatus.WriterIdle"/>.
        /// </summary>
        DateTime LastWriterIdleTime { get; }
        /// <summary>
        ///  Gets the time when the last <code>SessionIdle</code> event
        ///  is fired for <see cref="IdleStatus.BothIdle"/>.
        /// </summary>
        DateTime LastBothIdleTime { get; }

        /// <summary>
        /// Update all statistical properties related with throughput assuming
        /// the specified time is the current time.
        /// </summary>
        void UpdateThroughput(DateTime currentTime, bool force);
    }

    /// <summary>
    /// Provides data for <see cref="IOSession"/>'s events.
    /// </summary>
    public class IoSessionEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        public IoSessionEventArgs(IOSession session)
        {
            Session = session;
        }

        /// <summary>
        /// Gets the associated session.
        /// </summary>
        public IOSession Session { get; }
    }

    /// <summary>
    /// Provides data for <see cref="IOSession"/>'s idle event.
    /// </summary>
    public class IoSessionIdleEventArgs : IoSessionEventArgs
    {
        /// <summary>
        /// </summary>
        public IoSessionIdleEventArgs(IOSession session, IdleStatus idleStatus)
            : base(session)
        {
            IdleStatus = idleStatus;
        }

        /// <summary>
        /// Gets the <see cref="IdleStatus"/>.
        /// </summary>
        public IdleStatus IdleStatus { get; }
    }

    /// <summary>
    /// Provides data for <see cref="IOSession"/>'s exception event.
    /// </summary>
    public class IoSessionExceptionEventArgs : IoSessionEventArgs
    {
        /// <summary>
        /// </summary>
        public IoSessionExceptionEventArgs(IOSession session, Exception exception)
            : base(session)
        {
            Exception = exception;
        }

        /// <summary>
        /// Gets the associated exception.
        /// </summary>
        public Exception Exception { get; }
    }

    /// <summary>
    /// Provides data for <see cref="IOSession"/>'s message receive/sent event.
    /// </summary>
    public class IoSessionMessageEventArgs : IoSessionEventArgs
    {
        /// <summary>
        /// </summary>
        public IoSessionMessageEventArgs(IOSession session, object message)
            : base(session)
        {
            Message = message;
        }

        /// <summary>
        /// Gets the associated message.
        /// </summary>
        public object Message { get; }
    }
}
