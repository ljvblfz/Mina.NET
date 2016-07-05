#if !UNITY
using System.IO.Ports;
using System.Net;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// An endpoint for a serial port communication.
    /// </summary>
    public class SerialEndPoint : EndPoint
    {
        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="portName">the port name</param>
        /// <param name="baudRate">the baud rate</param>
        public SerialEndPoint(string portName, int baudRate)
            : this(portName, baudRate, Parity.None, 8, StopBits.One)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="portName">the port name</param>
        /// <param name="baudRate">the baud rate</param>
        /// <param name="parity">the <see cref="Parity"/></param>
        /// <param name="dataBits">the data bits</param>
        /// <param name="stopBits">the <see cref="StopBits"/></param>
        public SerialEndPoint(string portName, int baudRate,
            Parity parity, int dataBits, StopBits stopBits)
        {
            PortName = portName;
            BaudRate = baudRate;
            Parity = parity;
            DataBits = dataBits;
            StopBits = stopBits;
        }

        /// <summary>
        /// Gets the serial port name.
        /// </summary>
        public string PortName { get; }

        /// <summary>
        /// Gets the baud rate.
        /// </summary>
        public int BaudRate { get; }

        /// <summary>
        /// Gets the parity.
        /// </summary>
        public Parity Parity { get; }

        /// <summary>
        /// Gets the data bits.
        /// </summary>
        public int DataBits { get; }

        /// <summary>
        /// Gets the stop bits.
        /// </summary>
        public StopBits StopBits { get; }
    }
}
#endif