using Mina.Core.Service;

namespace Mina.Transport.Loopback
{
    class LoopbackPipe
    {
        public LoopbackPipe(LoopbackAcceptor acceptor, LoopbackEndPoint endpoint, IOHandler handler)
        {
            Acceptor = acceptor;
            Endpoint = endpoint;
            Handler = handler;
        }

        public LoopbackAcceptor Acceptor { get; }

        public LoopbackEndPoint Endpoint { get; }

        public IOHandler Handler { get; }
    }
}
