using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A container for data of a specific primitive type.
    /// </summary>
    public abstract class Buffer
    {
        private int _position;
        private int _limit;

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, and capacity,
        /// after checking invariants.
        /// </summary>
        protected Buffer(int mark, int position, int limit, int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentException("Capacity should be >= 0", nameof(capacity));
            }
            Capacity = capacity;
            Limit = limit;
            Position = position;
            if (mark >= 0)
            {
                if (mark > position)
                {
                    throw new ArgumentException("Invalid mark position", nameof(mark));
                }
                MarkValue = mark;
            }
        }

        /// <summary>
        /// Gets this buffer's capacity.
        /// </summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// Gets or sets this buffer's position.
        /// If the mark is defined and larger than the new position then it is discarded.
        /// </summary>
        public int Position
        {
            get { return _position; }
            set
            {
                if ((value > _limit) || (value < 0))
                {
                    throw new ArgumentException("Invalid position", nameof(value));
                }
                _position = value;
                if (MarkValue > _position)
                {
                    MarkValue = -1;
                }
            }
        }

        /// <summary>
        /// Gets or sets this buffer's limit.
        /// If the position is larger than the new limit then it is set to the new limit.
        /// If the mark is defined and larger than the new limit then it is discarded.
        /// </summary>
        public int Limit
        {
            get { return _limit; }
            set
            {
                if ((value > Capacity) || (value < 0))
                {
                    throw new ArgumentException("Invalid limit", nameof(value));
                }
                _limit = value;
                if (_position > _limit)
                {
                    _position = _limit;
                }
                if (MarkValue > _limit)
                {
                    MarkValue = -1;
                }
            }
        }

        /// <summary>
        /// Gets the number of elements between the current position and the limit.
        /// </summary>
        public int Remaining => _limit - _position;

        /// <summary>
        /// Tells whether there are any elements between the current position and the limit.
        /// </summary>
        public bool HasRemaining => _position < _limit;

        /// <summary>
        /// Tells whether or not this buffer is read-only.
        /// </summary>
        public abstract bool ReadOnly { get; }

        /// <summary>
        /// Sets this buffer's mark at its position.
        /// </summary>
        public Buffer Mark()
        {
            MarkValue = _position;
            return this;
        }

        /// <summary>
        /// Resets this buffer's position to the previously-marked position.
        /// </summary>
        public Buffer Reset()
        {
            var m = MarkValue;
            if (m < 0)
            {
                throw new InvalidOperationException();
            }
            _position = m;
            return this;
        }

        /// <summary>
        /// Clears this buffer.
        /// The position is set to zero, the limit is set to the capacity, and the mark is discarded.
        /// </summary>
        public Buffer Clear()
        {
            _position = 0;
            _limit = Capacity;
            MarkValue = -1;
            return this;
        }

        /// <summary>
        /// Flips this buffer.
        /// The limit is set to the current position and then the position is set to zero.
        /// If the mark is defined then it is discarded.
        /// </summary>
        public Buffer Flip()
        {
            _limit = _position;
            _position = 0;
            MarkValue = -1;
            return this;
        }

        /// <summary>
        /// Rewinds this buffer.
        /// The position is set to zero and the mark is discarded.
        /// </summary>
        public Buffer Rewind()
        {
            _position = 0;
            MarkValue = -1;
            return this;
        }

        /// <summary>
        /// Gets current mark.
        /// </summary>
        protected int MarkValue { get; set; } = -1;

        /// <summary>
        /// Sets capacity.
        /// </summary>
        /// <param name="capacity">the new capacity</param>
        protected void Recapacity(int capacity)
        {
            Capacity = capacity;
        }

        /// <summary>
        /// Checks the given index against the limit.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// the index is not smaller than the limit or is smaller than zero
        /// </exception>
        protected int CheckIndex(int i)
        {
            if ((i < 0) || (i >= _limit))
            {
                throw new IndexOutOfRangeException();
            }
            return i;
        }

        /// <summary>
        /// Checks the given index against the limit.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// the index + number of bytes is not smaller than the limit or is smaller than zero
        /// </exception>
        protected int CheckIndex(int i, int nb)
        {
            if ((i < 0) || (nb > _limit - i))
            {
                throw new IndexOutOfRangeException();
            }
            return i;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="BufferUnderflowException">
        /// the current position is not smaller than the limit
        /// </exception>
        protected int NextGetIndex()
        {
            if (_position >= _limit)
            {
                throw new BufferUnderflowException();
            }
            return _position++;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position with given number of bytes.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="BufferUnderflowException">
        /// the current position is not enough for the given number of bytes
        /// </exception>
        protected int NextGetIndex(int nb)
        {
            if (_limit - _position < nb)
            {
                throw new BufferUnderflowException();
            }
            var p = _position;
            _position += nb;
            return p;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="OverflowException">
        /// the current position is not smaller than the limit
        /// </exception>
        protected int NextPutIndex()
        {
            if (_position >= _limit)
            {
                throw new OverflowException();
            }
            return _position++;
        }

        /// <summary>
        /// Checks the current position against the limit,
        /// and then increments the position with given number of bytes.
        /// </summary>
        /// <returns>the current position value, before it is incremented</returns>
        /// <exception cref="OverflowException">
        /// the current position is not enough for the given number of bytes
        /// </exception>
        protected int NextPutIndex(int nb)
        {
            if (_limit - _position < nb)
            {
                throw new OverflowException();
            }
            var p = _position;
            _position += nb;
            return p;
        }

        /// <summary>
        /// Checks the bounds.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException"></exception>
        protected static void CheckBounds(int off, int len, int size)
        {
            if ((off | len | (off + len) | (size - (off + len))) < 0)
            {
                throw new IndexOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Byte order
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Big-endian
        /// </summary>
        BigEndian,
        /// <summary>
        /// Little-endian
        /// </summary>
        LittleEndian
    }
}
