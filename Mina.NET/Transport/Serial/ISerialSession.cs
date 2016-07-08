#if !UNITY
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// An <see cref="IOSession"/> for serial communication transport.
    /// </summary>
    public interface ISerialSession : IOSession
    {
        /// <inheritdoc/>
        new ISerialSessionConfig Config { get; }

        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.RtsEnable"/>
        /// </summary>
        bool RtsEnable { get; set; }

        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.DtrEnable"/>
        /// </summary>
        bool DtrEnable { get; set; }
    }
}
#endif