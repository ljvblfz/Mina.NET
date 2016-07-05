using System;
using System.Net;
using Mina.Core.Session;
using Mina.Example.SumUp.Codec;
using Mina.Example.SumUp.Message;
using Mina.Filter.Codec;
using Mina.Filter.Codec.Serialization;
using Mina.Filter.Logging;
using Mina.Transport.Socket;

namespace Mina.Example.SumUp
{
    class Server
    {
        private static readonly int ServerPort = 8080;
        private static readonly string SumKey = "sum";

        // Set this to false to use object serialization instead of custom codec.
        private static readonly bool UseCustomCodec = false;

        static void Main(string[] args)
        {
            var acceptor = new AsyncSocketAcceptor();

            if (UseCustomCodec)
            {
                acceptor.FilterChain.AddLast("codec",
                    new ProtocolCodecFilter(new SumUpProtocolCodecFactory(true)));
            }
            else
            {
                acceptor.FilterChain.AddLast("codec",
                    new ProtocolCodecFilter(new ObjectSerializationCodecFactory()));
            }

            acceptor.FilterChain.AddLast("logger", new LoggingFilter());

            acceptor.SessionOpened += (s, e) =>
            {
                e.Session.Config.SetIdleTime(IdleStatus.BothIdle, 60);
                e.Session.SetAttribute(SumKey, 0);
            };

            acceptor.SessionIdle += (s, e) =>
            {
                e.Session.Close(true);
            };

            acceptor.ExceptionCaught += (s, e) =>
            {
                Console.WriteLine(e.Exception);
                e.Session.Close(true);
            };

            acceptor.MessageReceived += (s, e) =>
            {
                // client only sends AddMessage. otherwise, we will have to identify
                // its type using instanceof operator.
                var am = (AddMessage)e.Message;

                // add the value to the current sum.
                var sum = e.Session.GetAttribute<int>(SumKey);
                var value = am.Value;
                var expectedSum = (long)sum + value;
                if (expectedSum > int.MaxValue || expectedSum < int.MinValue)
                {
                    // if the sum overflows or underflows, return error message
                    var rm = new ResultMessage();
                    rm.Sequence = am.Sequence; // copy sequence
                    rm.Ok = false;
                    e.Session.Write(rm);
                }
                else
                {
                    // sum up
                    sum = (int)expectedSum;
                    e.Session.SetAttribute(SumKey, sum);

                    // return the result message
                    var rm = new ResultMessage();
                    rm.Sequence = am.Sequence; // copy sequence
                    rm.Ok = true;
                    rm.Value = sum;
                    e.Session.Write(rm);
                }
            };

            acceptor.Bind(new IPEndPoint(IPAddress.Any, ServerPort));

            Console.WriteLine("Listening on port " + ServerPort);
            Console.ReadLine();
        }
    }
}
