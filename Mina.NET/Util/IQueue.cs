﻿using System.Collections.Generic;

namespace Mina.Util
{
    /// <summary>
    /// Represents a FIFO queue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQueue<T> : IEnumerable<T>
    {
        /// <summary>
        /// Checks if this queue is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Enqueue an item.
        /// </summary>
        void Enqueue(T item);

        /// <summary>
        /// Dequeue an item.
        /// </summary>
        T Dequeue();

        /// <summary>
        /// Gets the count of items in this queue.
        /// </summary>
        int Count { get; }
    }

    class Queue<T> : System.Collections.Generic.Queue<T>, IQueue<T>
    {
        public bool IsEmpty => Count == 0;

        T IQueue<T>.Dequeue()
        {
            return IsEmpty ? default(T) : base.Dequeue();
        }
    }

    class ConcurrentQueue<T> : System.Collections.Concurrent.ConcurrentQueue<T>, IQueue<T>
    {
        public T Dequeue()
        {
            var e = default(T);
            TryDequeue(out e);
            return e;
        }
    }
}
