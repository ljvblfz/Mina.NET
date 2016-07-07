using System.Net.Sockets;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    abstract class AbstractDatagramSessionConfig : AbstractIOSessionConfig, IDatagramSessionConfig
    {
        protected override void DoSetAll(IOSessionConfig config)
        {
            var cfg = config as IDatagramSessionConfig;
            if (cfg == null)
            {
                return;
            }

            if (cfg.EnableBroadcast.HasValue)
            {
                EnableBroadcast = cfg.EnableBroadcast;
            }
            if (cfg.ReceiveBufferSize.HasValue)
            {
                ReceiveBufferSize = cfg.ReceiveBufferSize;
            }
            if (cfg.SendBufferSize.HasValue)
            {
                SendBufferSize = cfg.SendBufferSize;
            }
            if (cfg.ReuseAddress.HasValue)
            {
                ReuseAddress = cfg.ReuseAddress;
            }
            if (cfg.TrafficClass.HasValue)
            {
                TrafficClass = cfg.TrafficClass;
            }
            if (cfg.ExclusiveAddressUse.HasValue)
            {
                ExclusiveAddressUse = cfg.ExclusiveAddressUse;
            }
            MulticastOption = cfg.MulticastOption;
        }

        public abstract bool? EnableBroadcast { get; set; }

        public abstract int? ReceiveBufferSize { get; set; }

        public abstract int? SendBufferSize { get; set; }

        public abstract bool? ReuseAddress { get; set; }

        public abstract int? TrafficClass { get; set; }

        public abstract bool? ExclusiveAddressUse { get; set; }

        public abstract MulticastOption MulticastOption { get; set; }
    }
}
