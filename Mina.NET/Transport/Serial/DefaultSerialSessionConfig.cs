#if !UNITY
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// The default configuration for a serial session 
    /// </summary>
    class DefaultSerialSessionConfig : AbstractIoSessionConfig, ISerialSessionConfig
    {
        public DefaultSerialSessionConfig()
        {
            // reset configs
            ReadBufferSize = 0;
            WriteTimeout = 0;
        }

        protected override void DoSetAll(IOSessionConfig config)
        {
            var cfg = config as ISerialSessionConfig;
            if (cfg != null)
            {
                ReadTimeout = cfg.ReadTimeout;
                WriteBufferSize = cfg.WriteBufferSize;
                ReceivedBytesThreshold = cfg.ReceivedBytesThreshold;
            }
        }

        public int ReadTimeout { get; set; }

        public int WriteBufferSize { get; set; }

        public int ReceivedBytesThreshold { get; set; }
    }
}
#endif