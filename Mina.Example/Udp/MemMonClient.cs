using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Transport.Socket;

namespace Mina.Example.Udp
{
    /// <summary>
    /// Sends its memory usage to the MemoryMonitor server.
    /// </summary>
    class MemMonClient
    {
        static void Main(string[] args)
        {
            IOConnector connector = new AsyncDatagramConnector();

            connector.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(e.Exception);
            };
            connector.MessageReceived += (s, e) =>
            {
                Console.WriteLine("Session recv...");
            };
            connector.MessageSent += (s, e) =>
            {
                Console.WriteLine("Session sent...");
            };
            connector.SessionCreated += (s, e) =>
            {
                Console.WriteLine("Session created...");
            };
            connector.SessionOpened += (s, e) =>
            {
                Console.WriteLine("Session opened...");
            };
            connector.SessionClosed += (s, e) =>
            {
                Console.WriteLine("Session closed...");
            };
            connector.SessionIdle += (s, e) =>
            {
                Console.WriteLine("Session idle...");
            };

            var connFuture = connector.Connect(new IPEndPoint(IPAddress.Loopback, MemoryMonitor.Port));
            connFuture.Await();

            connFuture.Complete += (s, e) =>
            {
                var f = (IConnectFuture)e.Future;
                if (f.Connected)
                {
                    Console.WriteLine("...connected");
                    var session = f.Session;

                    for (var i = 0; i < 30; i++)
                    {
                        var memory = GC.GetTotalMemory(false);
                        var buffer = IOBuffer.Allocate(8);
                        buffer.PutInt64(memory);
                        buffer.Flip();
                        session.Write(buffer);

                        try
                        {
                            Thread.Sleep(1000);
                        }
                        catch (ThreadInterruptedException)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Not connected...exiting");
                }
            };

            Console.ReadLine();
        }
    }
}
