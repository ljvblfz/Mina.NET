using System;
using System.Net;
using Mina.Core.Buffer;
using Mina.Filter.Logging;
using Mina.Transport.Socket;

namespace Mina.Example.Udp
{
    /// <summary>
    /// The class that will accept and process clients in order to properly
    /// track the memory usage.
    /// </summary>
    class MemoryMonitor
    {
        public const int Port = 18567;

        static void Main(string[] args)
        {
            var acceptor = new AsyncDatagramAcceptor();

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());
            acceptor.SessionConfig.ReuseAddress = true;

            acceptor.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(e.Exception);
                e.Session.Close(true);
            };
            acceptor.MessageReceived += (s, e) =>
            {
                var buf = e.Message as IOBuffer;
                if (buf != null)
                {
                    Console.WriteLine("New value for {0}: {1}", e.Session.RemoteEndPoint, buf.GetInt64());
                }
            };
            acceptor.SessionCreated += (s, e) =>
            {
                Console.WriteLine("Session created...");
            };
            acceptor.SessionOpened += (s, e) =>
            {
                Console.WriteLine("Session opened...");
            };
            acceptor.SessionClosed += (s, e) =>
            {
                Console.WriteLine("Session closed...");
            };
            acceptor.SessionIdle += (s, e) =>
            {
                Console.WriteLine("Session idle...");
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, Port));
            Console.WriteLine("UDPServer listening on port " + Port);
            Console.ReadLine();
        }
    }
}
