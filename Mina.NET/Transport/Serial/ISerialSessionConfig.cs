#if !UNITY
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// An <see cref="IOSessionConfig"/> for serial transport type.
    /// </summary>
    public interface ISerialSessionConfig : IOSessionConfig
    {
        /// <summary>
        /// Gets or set read timeout in seconds.
        /// </summary>
        int ReadTimeout { get; set; }
        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.WriteBufferSize"/>
        /// </summary>
        int WriteBufferSize { get; set; }
        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.ReceivedBytesThreshold"/>
        /// </summary>
        int ReceivedBytesThreshold { get; set; }
    }
}
#endif