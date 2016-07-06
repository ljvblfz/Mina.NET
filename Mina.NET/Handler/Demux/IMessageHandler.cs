using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// A handler interface that <see cref="DemuxingIOHandler"/> forwards
    /// <tt>MessageReceived</tt> or <tt>MessageSent</tt> events to.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Invoked when the specific type of message is received from or sent to
        /// the specified <code>session</code>.
        /// </summary>
        /// <param name="session">the associated <see cref="IOSession"/></param>
        /// <param name="message">the message to decode</param>
        void HandleMessage(IOSession session, object message);
    }

    /// <summary>
    /// A handler interface that <see cref="DemuxingIOHandler"/> forwards
    /// <tt>MessageReceived</tt> or <tt>MessageSent</tt> events to.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageHandler<in T> : IMessageHandler
    {
        /// <summary>
        /// Invoked when the specific type of message is received from or sent to
        /// the specified <code>session</code>.
        /// </summary>
        /// <param name="session">the associated <see cref="IOSession"/></param>
        /// <param name="message">the message to decode. Its type is set by the implementation</param>
        void HandleMessage(IOSession session, T message);
    }
}
