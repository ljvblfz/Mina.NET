using System;
using System.Text;
using Mina.Core.Service;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Filter.Logging;
using Mina.Transport.Serial;

namespace Mina.Example.GettingStarted.SerialShell
{
    class SerialShell
    {
        static void Main(string[] args)
        {
            IOConnector serial = new SerialConnector();

            // Add two filters : a logger and a codec
            serial.FilterChain.AddLast("logger", new LoggingFilter());
            serial.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

            serial.ExceptionCaught += (s, e) => e.Session.Close(true);
            serial.MessageReceived += (s, e) =>
            {
                Console.WriteLine(e.Message);
            };

            var serialEp = new SerialEndPoint("COM3", 38400);
            var future = serial.Connect(serialEp);
            future.Await();

            if (future.Connected)
            {
                while (true)
                {
                    var line = Console.ReadLine();
                    if (line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
                    {
                        future.Session.Close(true);
                        break;
                    }
                    future.Session.Write(line);
                }
            }
            else if (future.Exception != null)
            {
                Console.WriteLine(future.Exception);
            }

            Console.WriteLine("Press ENTER to exit");
            Console.ReadLine();
        }
    }
}
