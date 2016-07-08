using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Service
{
    /// <summary>
    /// An internal interface to represent an 'I/O processor' that performs
    /// actual I/O operations for <see cref="IOSession"/>s.
    /// </summary>
    public interface IOProcessor
    {
        /// <summary>
        /// Adds the specified <see cref="IOSession"/> to the I/O processor so that
        /// the I/O processor starts to perform any I/O operations related
        /// with the <see cref="IOSession"/>.
        /// </summary>
        /// <param name="session">the session to add</param>
        void Add(IOSession session);

        /// <summary>
        /// Writes the <see cref="IWriteRequest"/> for the specified <see cref="IOSession"/>.
        /// </summary>
        /// <param name="session">the session where the message to be written</param>
        /// <param name="writeRequest">thr message to write</param>
        void Write(IOSession session, IWriteRequest writeRequest);

        /// <summary>
        /// Flushes the internal write request queue of the specified <see cref="IOSession"/>.
        /// </summary>
        /// <param name="session">the session to flush</param>
        void Flush(IOSession session);

        /// <summary>
        /// Removes and closes the specified <see cref="IOSession"/> from the I/O
        ///  processor so that the I/O processor closes the connection
        ///  associated with the <see cref="IOSession"/> and releases any other
        ///  related resources.
        /// </summary>
        /// <param name="session">the session to remove</param>
        void Remove(IOSession session);

        /// <summary>
        /// Controls the traffic of the specified <paramref name="session"/>
        /// depending of the <see cref="IOSession.ReadSuspended"/>
        /// and <see cref="IOSession.WriteSuspended"/> flags.
        /// </summary>
        /// <param name="session">the session to control</param>
        void UpdateTrafficControl(IOSession session);
    }

    /// <summary>
    /// An internal interface to represent an 'I/O processor' that performs
    /// actual I/O operations for <typeparamref name="TS"/>s.
    /// </summary>
    /// <typeparam name="TS">the type of sessions</typeparam>
    public interface IIOProcessor<in TS> : IOProcessor
        where TS : IOSession
    {
        /// <summary>
        /// Adds the specified <typeparamref name="TS"/> to the I/O processor so that
        /// the I/O processor starts to perform any I/O operations related
        /// with the <typeparamref name="TS"/>.
        /// </summary>
        /// <param name="session">the session to add</param>
        void Add(TS session);

        /// <summary>
        /// Writes the <see cref="IWriteRequest"/> for the specified <typeparamref name="TS"/>.
        /// </summary>
        /// <param name="session">the session we want the message to be written</param>
        /// <param name="writeRequest">the message to write</param>
        void Write(TS session, IWriteRequest writeRequest);

        /// <summary>
        /// Flushes the internal write request queue of the specified <typeparamref name="TS"/>.
        /// </summary>
        /// <param name="session">the session to flush</param>
        void Flush(TS session);

        /// <summary>
        /// Removes and closes the specified <typeparamref name="TS"/> from the I/O
        ///  processor so that the I/O processor closes the connection
        ///  associated with the <typeparamref name="TS"/> and releases any other
        ///  related resources.
        /// </summary>
        /// <param name="session">the session to remove</param>
        void Remove(TS session);

        /// <summary>
        /// Controls the traffic of the specified <paramref name="session"/>
        /// depending of the <see cref="IOSession.ReadSuspended"/>
        /// and <see cref="IOSession.WriteSuspended"/> flags.
        /// </summary>
        /// <param name="session">the session to control</param>
        void UpdateTrafficControl(TS session);
    }
}
