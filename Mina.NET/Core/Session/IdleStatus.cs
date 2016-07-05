using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// Represents the type of idleness of <see cref="IOSession"/>.
    /// </summary>
    public enum IdleStatus
    {
        /// <summary>
        /// Represents the session status that no data is coming from the remote peer.
        /// </summary>
        ReaderIdle,
        /// <summary>
        /// Represents the session status that the session is not writing any data.
        /// </summary>
        WriterIdle,
        /// <summary>
        /// Represents both ReaderIdle and WriterIdle.
        /// </summary>
        BothIdle
    }

    /// <summary>
    /// Provides data for idle events.
    /// </summary>
    public class IdleEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        public IdleEventArgs(IdleStatus idleStatus)
        {
            IdleStatus = idleStatus;
        }

        /// <summary>
        /// Gets the idle status.
        /// </summary>
        public IdleStatus IdleStatus { get; }
    }
}
