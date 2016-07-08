using System;
using System.Collections;
using System.Collections.Generic;
using Mina.Core.Session;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// The default implementation of <see cref="IOFilterChainBuilder"/> which is useful
    /// in most cases.  <see cref="DefaultIOFilterChainBuilder"/> has an identical interface
    /// with <see cref="IOFilter"/>; it contains a list of <see cref="IOFilter"/>s that you can
    /// modify. The <see cref="IOFilter"/>s which are added to this builder will be appended
    /// to the <see cref="IOFilterChain"/> when BuildFilterChain(IoFilterChain) is
    /// invoked.
    /// However, the identical interface doesn't mean that it behaves in an exactly
    /// same way with <see cref="IOFilterChain"/>.  <see cref="DefaultIOFilterChainBuilder"/>
    /// doesn't manage the life cycle of the <see cref="IOFilter"/>s at all, and the
    /// existing <see cref="IOSession"/>s won't get affected by the changes in this builder.
    /// <see cref="IOFilterChainBuilder"/>s affect only newly created <see cref="IOSession"/>s.
    /// </summary>
    public class DefaultIOFilterChainBuilder : IOFilterChainBuilder
    {
        private readonly List<EntryImpl> _entries;
        private readonly object _syncRoot;

        /// <summary>
        /// </summary>
        public DefaultIOFilterChainBuilder()
        {
            _entries = new List<EntryImpl>();
            _syncRoot = ((ICollection) _entries).SyncRoot;
        }

        /// <summary>
        /// </summary>
        public DefaultIOFilterChainBuilder(DefaultIOFilterChainBuilder filterChain)
        {
            if (filterChain == null)
            {
                throw new ArgumentNullException(nameof(filterChain));
            }
            _entries = new List<EntryImpl>(filterChain._entries);
            _syncRoot = ((ICollection) _entries).SyncRoot;
        }

        /// <summary>
        /// Gets the <see cref="IEntry&lt;IoFilter, INextFilter&gt;"/> with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name we are looking for</param>
        /// <returns>the <see cref="IEntry&lt;IoFilter, INextFilter&gt;"/> with the given name, or null if not found</returns>
        public IEntry<IOFilter, INextFilter> GetEntry(string name)
        {
            return _entries.Find(e => e.Name.Equals(name));
        }

        /// <summary>
        /// Gets the <see cref="IOFilter"/> with the specified <paramref name="name"/> in this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>the <see cref="IOFilter"/>, or null if not found</returns>
        public IOFilter Get(string name)
        {
            var entry = GetEntry(name);
            return entry == null ? null : entry.Filter;
        }

        /// <summary>
        /// Gets all <see cref="IEntry&lt;IoFilter, INextFilter&gt;"/>s in this chain.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEntry<IOFilter, INextFilter>> GetAll()
        {
            foreach (var item in _entries)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Checks if this chain contains a filter with the specified <paramref name="name"/>.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <returns>true if this chain contains a filter with the specified <paramref name="name"/></returns>
        public bool Contains(string name)
        {
            return GetEntry(name) != null;
        }

        /// <summary>
        /// Adds the specified filter with the specified name at the beginning of this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddFirst(string name, IOFilter filter)
        {
            lock (_syncRoot)
            {
                Register(0, new EntryImpl(this, name, filter));
            }
        }

        /// <summary>
        /// Adds the specified filter with the specified name at the end of this chain.
        /// </summary>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddLast(string name, IOFilter filter)
        {
            lock (_syncRoot)
            {
                Register(_entries.Count, new EntryImpl(this, name, filter));
            }
        }

        /// <summary>
        /// Adds the specified filter with the specified name just before the filter whose name is
        /// <paramref name="baseName"/> in this chain.
        /// </summary>
        /// <param name="baseName">the targeted filter's name</param>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddBefore(string baseName, string name, IOFilter filter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(baseName);

                var i = _entries.FindIndex(e => e.Name.Equals(baseName));
                if (i >= 0)
                {
                    Register(i, new EntryImpl(this, name, filter));
                }
            }
        }

        /// <summary>
        /// Adds the specified filter with the specified name just after the filter whose name is
        /// <paramref name="baseName"/> in this chain.
        /// </summary>
        /// <param name="baseName">the targeted filter's name</param>
        /// <param name="name">the filter's name</param>
        /// <param name="filter">the filter to add</param>
        public void AddAfter(string baseName, string name, IOFilter filter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(baseName);
                var i = _entries.FindIndex(e => e.Name.Equals(baseName));
                if (i >= 0)
                {
                    Register(i + 1, new EntryImpl(this, name, filter));
                }
            }
        }

        /// <summary>
        /// Removes the filter with the specified name from this chain.
        /// </summary>
        /// <param name="name">the name of the filter to remove</param>
        /// <returns>the removed filter</returns>
        public IOFilter Remove(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            lock (_syncRoot)
            {
                var entry = _entries.Find(e => e.Name.Equals(name));
                if (entry != null)
                {
                    _entries.Remove(entry);
                    return entry.Filter;
                }
            }

            throw new ArgumentException("Unknown filter name: " + name);
        }

        /// <summary>
        /// Replace the filter with the specified name with the specified new filter.
        /// </summary>
        /// <param name="name">the name of the filter to replace</param>
        /// <param name="newFilter">the new filter</param>
        /// <returns>the old filter</returns>
        public IOFilter Replace(string name, IOFilter newFilter)
        {
            lock (_syncRoot)
            {
                CheckBaseName(name);
                var e = (EntryImpl) GetEntry(name);
                var oldFilter = e.Filter;
                e.Filter = newFilter;
                return oldFilter;
            }
        }

        /// <summary>
        /// Removes all filters added to this chain.
        /// </summary>
        public void Clear()
        {
            lock (_syncRoot)
            {
                _entries.Clear();
            }
        }

        /// <inheritdoc/>
        public void BuildFilterChain(IOFilterChain chain)
        {
            foreach (var entry in _entries)
            {
                chain.AddLast(entry.Name, entry.Filter);
            }
        }

        private void CheckBaseName(string baseName)
        {
            if (baseName == null)
            {
                throw new ArgumentNullException(nameof(baseName));
            }
            if (!Contains(baseName))
            {
                throw new ArgumentException("Unknown filter name: " + baseName);
            }
        }

        private void Register(int index, EntryImpl e)
        {
            if (Contains(e.Name))
            {
                throw new ArgumentException("Other filter is using the same name: " + e.Name);
            }
            _entries.Insert(index, e);
        }

        class EntryImpl : IEntry<IOFilter, INextFilter>
        {
            private readonly DefaultIOFilterChainBuilder _chain;

            public EntryImpl(DefaultIOFilterChainBuilder chain, string name, IOFilter filter)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                if (filter == null)
                {
                    throw new ArgumentNullException(nameof(filter));
                }

                _chain = chain;
                Name = name;
                Filter = filter;
            }

            public string Name { get; }

            public IOFilter Filter { get; set; }

            public INextFilter NextFilter
            {
                get { throw new InvalidOperationException(); }
            }

            public override string ToString()
            {
                return "(" + Name + ':' + Filter + ')';
            }

            public void AddAfter(string name, IOFilter filter)
            {
                _chain.AddAfter(Name, name, filter);
            }

            public void AddBefore(string name, IOFilter filter)
            {
                _chain.AddBefore(Name, name, filter);
            }

            public void Remove()
            {
                _chain.Remove(Name);
            }

            public void Replace(IOFilter newFilter)
            {
                _chain.Replace(Name, newFilter);
            }
        }
    }
}
