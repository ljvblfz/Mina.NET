using System;
using System.IO;
using Mina.Core.Buffer;

namespace Mina.Core.File
{
    /// <summary>
    /// <see cref="IFileRegion"/> based on a <see cref="FileInfo"/>.
    /// </summary>
    public class FileInfoFileRegion : IFileRegion
    {
        private readonly FileInfo _file;
        private readonly long _originalPosition;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="fileInfo">the file info</param>
        public FileInfoFileRegion(FileInfo fileInfo)
            : this(fileInfo, 0, fileInfo.Length)
        {
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="fileInfo">the file info</param>
        /// <param name="position">the start position</param>
        /// <param name="remainingBytes">the count of remaining bytes</param>
        public FileInfoFileRegion(FileInfo fileInfo, long position, long remainingBytes)
        {
            if (fileInfo == null)
            {
                throw new ArgumentNullException(nameof(fileInfo));
            }
            if (position < 0L)
            {
                throw new ArgumentException("position may not be less than 0", nameof(position));
            }
            if (remainingBytes < 0L)
            {
                throw new ArgumentException("remainingBytes may not be less than 0", nameof(remainingBytes));
            }

            _file = fileInfo;
            _originalPosition = position;
            Position = position;
            RemainingBytes = remainingBytes;
        }

        /// <inheritdoc/>
        public string FullName => _file.FullName;

        /// <inheritdoc/>
        public long Length => _file.Length;

        /// <inheritdoc/>
        public long Position { get; private set; }

        /// <inheritdoc/>
        public long RemainingBytes { get; private set; }

        /// <inheritdoc/>
        public long WrittenBytes => Position - _originalPosition;

        /// <inheritdoc/>
        public int Read(IOBuffer buffer)
        {
            using (var fs = _file.OpenRead())
            {
                fs.Position = Position;
                var bytes = new byte[buffer.Remaining];
                var read = fs.Read(bytes, 0, bytes.Length);
                buffer.Put(bytes, 0, read);
                Update(read);
                return read;
            }
        }

        /// <inheritdoc/>
        public void Update(long amount)
        {
            Position += amount;
            RemainingBytes -= amount;
        }
    }
}
