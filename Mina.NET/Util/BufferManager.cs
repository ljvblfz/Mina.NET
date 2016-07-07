using System.Collections.Generic;
using System.Net.Sockets;

namespace Mina.Util
{
    /// <summary>
    /// This class creates a single large buffer which can be divided up and assigned to SocketAsyncEventArgs objects for use
    /// with each socket I/O operation.  This enables buffers to be easily reused and gaurds against fragmenting heap memory.
    /// 
    /// The operations exposed on the BufferManager class are not thread safe.
    /// </summary>
    class BufferManager
    {
        int _numBytes; // the total number of bytes controlled by the buffer pool
        byte[] _buffer; // the underlying byte array maintained by the Buffer Manager
        Stack<int> _freeIndexPool;
        int _currentIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferManager"/> class.
        /// </summary>
        /// <param name="totalBytes">the total bytes</param>
        /// <param name="bufferSize">the size of one buffer</param>
        public BufferManager(int totalBytes, int bufferSize)
        {
            _numBytes = totalBytes;
            _currentIndex = 0;
            BufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();
        }

        public int BufferSize { get; }

        /// <summary>
        /// Allocates buffer space used by the buffer pool
        /// </summary>
        public void InitBuffer()
        {
            // create one big large buffer and divide that out to each SocketAsyncEventArg object
            _buffer = new byte[_numBytes];
        }

        /// <summary>
        /// Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        /// </summary>
        /// <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), BufferSize);
            }
            else
            {
                if ((_numBytes - BufferSize) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, BufferSize);
                _currentIndex += BufferSize;
            }
            return true;
        }

        /// <summary>
        /// Removes the buffer from a SocketAsyncEventArg object.  This frees the buffer back to the 
        /// buffer pool
        /// </summary>
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
