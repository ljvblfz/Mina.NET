namespace Mina.Transport.Socket
{
    class DefaultDatagramSessionConfig : AbstractDatagramSessionConfig
    {
        public override bool? EnableBroadcast { get; set; }

        public override int? ReceiveBufferSize { get; set; }

        public override int? SendBufferSize { get; set; }

        public override bool? ExclusiveAddressUse { get; set; }

        public override bool? ReuseAddress { get; set; }

        public override int? TrafficClass { get; set; }

        public override System.Net.Sockets.MulticastOption MulticastOption { get; set; }
    }
}
