using Mina.Core.Buffer;

namespace Mina.Core.File
{
    /// <summary>
    /// Indicates the region of a file to be sent to the remote host.
    /// </summary>
    public interface IFileRegion
    {
        /// <summary>
        /// Gets the absolute filename for the underlying file.
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Gets the total length of the file.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Gets the current file position from which data will be read.
        /// </summary>
        long Position { get; }

        /// <summary>
        /// Gets the number of bytes remaining to be written from the file
        /// to the remote host.
        /// </summary>
        long RemainingBytes { get; }

        /// <summary>
        /// Gets the total number of bytes already written.
        /// </summary>
        long WrittenBytes { get; }

        /// <summary>
        /// Reads as much bytes in to a buffer as the remaining of it.
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>the actual number of read bytes</returns>
        int Read(IOBuffer buffer);

        /// <summary>
        /// Updates the current file position based on the specified amount.
        /// </summary>
        void Update(long amount);
    }
}
