using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mina.Core.Session
{
    class DefaultIOSessionAttributeMap : IOSessionAttributeMap
    {
        private readonly ConcurrentDictionary<object, object> _attributes = new ConcurrentDictionary<object, object>();

        public object GetAttribute(IOSession session, object key, object defaultValue)
        {
            if (defaultValue == null)
            {
                object obj;
                _attributes.TryGetValue(key, out obj);
                return obj;
            }
            return _attributes.GetOrAdd(key, defaultValue);
        }

        public object SetAttribute(IOSession session, object key, object value)
        {
            object old = null;
            _attributes.AddOrUpdate(key, value, (k, v) => 
            {
                old = v;
                return value;
            });
            return old;
        }

        public object SetAttributeIfAbsent(IOSession session, object key, object value)
        {
            return _attributes.GetOrAdd(key, value);
        }

        public object RemoveAttribute(IOSession session, object key)
        {
            object obj;
            _attributes.TryRemove(key, out obj);
            return obj;
        }

        public bool ContainsAttribute(IOSession session, object key)
        {
            return _attributes.ContainsKey(key);
        }

        public IEnumerable<object> GetAttributeKeys(IOSession session)
        {
            return _attributes.Keys;
        }

        public void Dispose(IOSession session)
        {
            // Do nothing
        }
    }
}
