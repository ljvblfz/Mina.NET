using System.Collections.Generic;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Filter.Util
{
    /// <summary>
    /// An <see cref="IOFilter"/> that sets initial attributes when a new
    /// <see cref="IOSession"/> is created.  By default, the attribute map is empty when
    /// an <see cref="IOSession"/> is newly created.  Inserting this filter will make
    /// the pre-configured attributes available after this filter executes the
    /// <tt>SessionCreated</tt> event.
    /// </summary>
    public class SessionAttributeInitializingFilter : IoFilterAdapter
    {
        private readonly Dictionary<string, object> _attributes = new Dictionary<string, object>();

        /// <summary>
        /// Creates a new instance with no default attributes.
        /// </summary>
        public SessionAttributeInitializingFilter()
        { }

        /// <summary>
        /// Creates a new instance with the specified default attributes.
        /// </summary>
        public SessionAttributeInitializingFilter(IDictionary<string, object> attributes)
        {
            Attributes = attributes;
        }

        /// <summary>
        /// Gets or sets the attribute map.
        /// </summary>
        public IDictionary<string, object> Attributes
        {
            get { return _attributes; }
            set
            {
                _attributes.Clear();
                if (value != null)
                {
                    foreach (var pair in value)
                    {
                        _attributes[pair.Key] = pair.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the value of user-defined attribute.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns><tt>null</tt> if there is no attribute with the specified key</returns>
        public object GetAttribute(string key)
        {
            object value;
            return _attributes.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>
        /// Sets a user-defined attribute.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <param name="value">the value of the attribute</param>
        /// <returns>the old value of the attribute, or <tt>null</tt> if it is new</returns>
        public object SetAttribute(string key, object value)
        {
            if (value == null)
                return RemoveAttribute(key);

            object old;
            _attributes.TryGetValue(key, out old);
            _attributes[key] = value;
            return old;
        }

        /// <summary>
        /// Sets a user defined attribute without a value.
        /// This is useful when you just want to put a 'mark' attribute.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns>the old value of the attribute, or <tt>null</tt> if it is new</returns>
        public object SetAttribute(string key)
        {
            return SetAttribute(key, true);
        }

        /// <summary>
        /// Removes a user-defined attribute with the specified key.
        /// </summary>
        /// <param name="key">the key of the attribute</param>
        /// <returns>the old value of the attribute, or <tt>null</tt> if not found</returns>
        public object RemoveAttribute(string key)
        {
            object old;
            _attributes.TryGetValue(key, out old);
            _attributes.Remove(key);
            return old;
        }

        /// <summary>
        /// Returns <tt>true</tt> if this session contains the attribute with
        /// the specified <tt>key</tt>.
        /// </summary>
        public bool ContainsAttribute(string key)
        {
            return _attributes.ContainsKey(key);
        }

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IOSession session)
        {
            foreach (var pair in _attributes)
            {
                session.SetAttribute(pair.Key, pair.Value);
            }

            base.SessionCreated(nextFilter, session);
        }
    }
}
