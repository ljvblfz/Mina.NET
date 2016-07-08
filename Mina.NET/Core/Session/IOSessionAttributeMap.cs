using System.Collections.Generic;

namespace Mina.Core.Session
{
    /// <summary>
    /// Stores the user-defined attributes which is provided per <see cref="IOSession"/>.
    /// All user-defined attribute accesses in <see cref="IOSession"/> are forwarded to
    /// the instance of <see cref="IOSessionAttributeMap"/>.
    /// </summary>
    public interface IOSessionAttributeMap
    {
        /// <summary>
        /// Returns the value of user defined attribute associated with the
        /// specified key. If there's no such attribute, the specified default
        /// value is associated with the specified key, and the default value is
        /// returned.
        /// </summary>
        object GetAttribute(IOSession session, object key, object defaultValue);

        /// <summary>
        /// Sets a user-defined attribute.
        /// </summary>
        object SetAttribute(IOSession session, object key, object value);

        /// <summary>
        /// Sets a user defined attribute if the attribute with the specified key
        /// is not set yet.
        /// </summary>
        object SetAttributeIfAbsent(IOSession session, object key, object value);

        /// <summary>
        /// Removes a user-defined attribute with the specified key.
        /// </summary>
        object RemoveAttribute(IOSession session, object key);

        /// <summary>
        /// Returns <tt>true</tt> if this session contains the attribute with the specified <tt>key</tt>.
        /// </summary>
        bool ContainsAttribute(IOSession session, object key);

        /// <summary>
        /// Returns the keys of all user-defined attributes.
        /// </summary>
        IEnumerable<object> GetAttributeKeys(IOSession session);

        /// <summary>
        /// Disposes any releases associated with the specified session.
        /// This method is invoked on disconnection.
        /// </summary>
        void Dispose(IOSession session);
    }
}
