using System;
using System.Net;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// An endpoint which represents loopback port number.
    /// </summary>
    public class LoopbackEndPoint : EndPoint, IComparable<LoopbackEndPoint>
    {
        /// <summary>
        /// Creates a new instance with the specified port number.
        /// </summary>
        public LoopbackEndPoint(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Gets the port number.
        /// </summary>
        public int Port { get; }

        /// <inheritdoc/>
        public int CompareTo(LoopbackEndPoint other)
        {
            return Port.CompareTo(other.Port);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Port.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            var other = obj as LoopbackEndPoint;
            return obj != null && Port == other.Port;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Port >= 0 ? ("vm:server:" + Port) : ("vm:client:" + -Port);
        }
    }
}
