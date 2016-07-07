using Mina.Core.Session;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// Provides keep-alive messages to <see cref="KeepAliveFilter"/>.
    /// </summary>
    public interface IKeepAliveMessageFactory
    {
        /// <summary>
        /// Returns <tt>true</tt> if and only if the specified message is a
        /// keep-alive request message.
        /// </summary>
        bool IsRequest(IOSession session, object message);

        /// <summary>
        /// Returns <tt>true</tt> if and only if the specified message is a 
        /// keep-alive response message;
        /// </summary>
        bool IsResponse(IOSession session, object message);

        /// <summary>
        /// Returns a (new) keep-alive request message.
        /// Returns <tt>null</tt> if no request is required.
        /// </summary>
        object GetRequest(IOSession session);

        /// <summary>
        /// Returns a (new) response message for the specified keep-alive request.
        /// Returns <tt>null</tt> if no response is required.
        /// </summary>
        object GetResponse(IOSession session, object request);
    }
}
