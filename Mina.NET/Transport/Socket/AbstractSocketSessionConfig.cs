using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    abstract class AbstractSocketSessionConfig : AbstractIoSessionConfig, ISocketSessionConfig
    {
        protected override void DoSetAll(IOSessionConfig config)
        {
            var cfg = config as ISocketSessionConfig;
            if (cfg == null)
                return;

            if (cfg.ReceiveBufferSize.HasValue)
                ReceiveBufferSize = cfg.ReceiveBufferSize;
            if (cfg.SendBufferSize.HasValue)
                SendBufferSize = cfg.SendBufferSize;
            if (cfg.ReuseAddress.HasValue)
                ReuseAddress = cfg.ReuseAddress;
            if (cfg.TrafficClass.HasValue)
                TrafficClass = cfg.TrafficClass;
            if (cfg.ExclusiveAddressUse.HasValue)
                ExclusiveAddressUse = cfg.ExclusiveAddressUse;
            if (cfg.KeepAlive.HasValue)
                KeepAlive = cfg.KeepAlive;
            if (cfg.OobInline.HasValue)
                OobInline = cfg.OobInline;
            if (cfg.NoDelay.HasValue)
                NoDelay = cfg.NoDelay;
            if (cfg.SoLinger.HasValue)
                SoLinger = cfg.SoLinger;
        }

        public abstract int? ReceiveBufferSize { get; set; }

        public abstract int? SendBufferSize { get; set; }

        public abstract bool? NoDelay { get; set; }

        public abstract int? SoLinger { get; set; }

        public abstract bool? ExclusiveAddressUse { get; set; }

        public abstract bool? ReuseAddress { get; set; }

        public abstract int? TrafficClass { get; set; }

        public abstract bool? KeepAlive { get; set; }

        public abstract bool? OobInline { get; set; }
    }
}
