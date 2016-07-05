using System;
using System.Net;
using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Filter.Ssl;
using Mina.Transport.Socket;

namespace Mina.Example.Chat.Server
{
    class Program
    {
        private static readonly int Port = 1234;
        private static readonly bool Ssl = true;

        static void Main(string[] args)
        {
            IOAcceptor acceptor = new AsyncSocketAcceptor();

            if (Ssl)
            {
                acceptor.FilterChain.AddLast("ssl", new SslFilter(AppDomain.CurrentDomain.BaseDirectory + "\\TempCert.cer"));
                Console.WriteLine("SSL ON");
            }

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());
            acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory()));

            acceptor.Handler = new ChatProtocolHandler();

            acceptor.Bind(new IPEndPoint(IPAddress.Any, Port));

            Console.WriteLine("Listening on " + acceptor.LocalEndPoint);

            Console.ReadLine();
        }
    }
}
