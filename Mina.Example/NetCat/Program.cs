using System;
using System.Net;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Transport.Socket;

namespace Mina.Example.NetCat
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(typeof(Program).FullName + " <hostname> <port>");
                return;
            }

            // Create TCP/IP connector.
            var connector = new AsyncSocketConnector();

            // Set connect timeout.
            connector.ConnectTimeoutInMillis = 30 * 1000L;

            // Set reader idle time to 10 seconds.
            // sessionIdle(...) method will be invoked when no data is read
            // for 10 seconds.
            connector.SessionOpened += (s, e) => e.Session.Config.SetIdleTime(IdleStatus.ReaderIdle, 10);

            // Print out total number of bytes read from the remote peer.
            connector.SessionClosed += (s, e) => Console.WriteLine("Total " + e.Session.ReadBytes + " byte(s)");

            connector.SessionIdle += (s, e) => 
            {
                if (e.IdleStatus == IdleStatus.ReaderIdle)
                    e.Session.Close(true);
            };

            connector.MessageReceived += (s, e) =>
            {
                var buf = (IOBuffer)e.Message;
                while (buf.HasRemaining)
                {
                    Console.Write((char)buf.Get());
                }
            };

            // Start communication.
            var cf = connector.Connect(new IPEndPoint(Dns.GetHostEntry(args[0]).AddressList[3], int.Parse(args[1])));

            // Wait for the connection attempt to be finished.
            cf.Await();
            cf.Session.CloseFuture.Await();

            connector.Dispose();
        }
    }
}
