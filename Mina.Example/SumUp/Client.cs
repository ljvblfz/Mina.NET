using System;
using System.Net;
using System.Threading;
using Mina.Core.Session;
using Mina.Example.SumUp.Codec;
using Mina.Example.SumUp.Message;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Serialization;
using Mina.Filter.Logging;
using Mina.Transport.Socket;

namespace Mina.Example.SumUp
{
    class Client
    {
        private static readonly int Port = 8080;
        private static readonly long ConnectTimeout = 30 * 1000L; // 30 seconds

        // Set this to false to use object serialization instead of custom codec.
        private static readonly bool UseCustomCodec = false;

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please specify the list of any integers");
                return;
            }

            // prepare values to sum up
            var values = new int[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                values[i] = int.Parse(args[i]);
            }

            var connector = new AsyncSocketConnector();

            // Configure the service.
            connector.ConnectTimeoutInMillis = ConnectTimeout;

            if (UseCustomCodec)
            {
                connector.FilterChain.AddLast("codec",
                    new ProtocolCodecFilter(new SumUpProtocolCodecFactory(false)));
            }
            else
            {
                connector.FilterChain.AddLast("codec",
                    new ProtocolCodecFilter(new ObjectSerializationCodecFactory()));
            }

            connector.FilterChain.AddLast("logger", new LoggingFilter());

            connector.SessionOpened += (s, e) =>
            {
                // send summation requests
                for (var i = 0; i < values.Length; i++)
                {
                    var m = new AddMessage();
                    m.Sequence = i;
                    m.Value = values[i];
                    e.Session.Write(m);
                }
            };

            connector.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(e.Exception);
                e.Session.Close(true);
            };

            connector.MessageReceived += (s, e) =>
            {
                // server only sends ResultMessage. otherwise, we will have to identify
                // its type using instanceof operator.
                var rm = (ResultMessage)e.Message;
                if (rm.Ok)
                {
                    // server returned OK code.
                    // if received the result message which has the last sequence
                    // number,
                    // it is time to disconnect.
                    if (rm.Sequence == values.Length - 1)
                    {
                        // print the sum and disconnect.
                        Console.WriteLine("The sum: " + rm.Value);
                        e.Session.Close(true);
                    }
                }
                else
                {
                    // seever returned error code because of overflow, etc.
                    Console.WriteLine("Server error, disconnecting...");
                    e.Session.Close(true);
                }
            };

            IOSession session;
            while (true)
            {
                try
                {
                    var future = connector.Connect(new IPEndPoint(IPAddress.Loopback, Port));
                    future.Await();
                    session = future.Session;
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    Thread.Sleep(3000);
                }
            }

            // wait until the summation is done
            session.CloseFuture.Await();
            Console.WriteLine("Press any key to exit");
            Console.Read();
        }
    }
}
