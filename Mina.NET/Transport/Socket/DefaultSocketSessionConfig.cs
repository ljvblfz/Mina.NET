namespace Mina.Transport.Socket
{
    class DefaultSocketSessionConfig : AbstractSocketSessionConfig
    {
        public override int? ReceiveBufferSize { get; set; }

        public override int? SendBufferSize { get; set; }

        public override bool? NoDelay { get; set; }

        public override int? SoLinger { get; set; }

        public override bool? ExclusiveAddressUse { get; set; }

        public override bool? ReuseAddress { get; set; }

        public override int? TrafficClass { get; set; }

        public override bool? KeepAlive { get; set; }

        public override bool? OobInline { get; set; }
    }
}
