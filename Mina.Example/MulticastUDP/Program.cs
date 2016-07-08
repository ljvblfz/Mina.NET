using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Mina.Filter.Codec;
using Mina.Filter.Codec.TextLine;
using Mina.Transport.Socket;

namespace MulticastUDP
{
    /// <summary>
    /// UDP Multicast
    /// 
    /// See http://msdn.microsoft.com/en-us/library/system.net.sockets.multicastoption%28v=vs.110%29.aspx
    /// </summary>
    class Program
    {
        static IPAddress _mcastAddress;
        static int _mcastPort;

        static void Main(string[] args)
        {
            // Initialize the multicast address group and multicast port. 
            // Both address and port are selected from the allowed sets as 
            // defined in the related RFC documents. These are the same  
            // as the values used by the sender.
            _mcastAddress = IPAddress.Parse("224.168.100.2");
            _mcastPort = 11000;

            StartMulticastAcceptor();
            StartMulticastConnector();

            Console.ReadLine();
        }

        static void StartMulticastAcceptor()
        {
            var localIpAddr = IPAddress.Any;
            var acceptor = new AsyncDatagramAcceptor();

            acceptor.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

            // Define a MulticastOption object specifying the multicast group  
            // address and the local IPAddress. 
            // The multicast group address is the same as the address used by the client.
            var mcastOption = new MulticastOption(_mcastAddress, localIpAddr);
            acceptor.SessionConfig.MulticastOption = mcastOption;

            acceptor.SessionOpened += (s, e) =>
            {
                Console.WriteLine("Opened: {0}", e.Session.RemoteEndPoint);
            };
            acceptor.MessageReceived += (s, e) =>
            {
                Console.WriteLine("Received from {0}: {1}", e.Session.RemoteEndPoint, e.Message);
            };

            acceptor.Bind(new IPEndPoint(localIpAddr, _mcastPort));

            Console.WriteLine("Acceptor: current multicast group is: " + mcastOption.Group);
            Console.WriteLine("Acceptor: current multicast local address is: " + mcastOption.LocalAddress);
            Console.WriteLine("Waiting for multicast packets.......");
        }

        static void StartMulticastConnector()
        {
            var localIpAddr = IPAddress.Any;
            var mcastEp = new IPEndPoint(_mcastAddress, _mcastPort);
            var connector = new AsyncDatagramConnector();

            connector.FilterChain.AddLast("codec", new ProtocolCodecFilter(new TextLineCodecFactory(Encoding.UTF8)));

            // Set the local IP address used by the listener and the sender to 
            // exchange multicast messages. 
            connector.DefaultLocalEndPoint = new IPEndPoint(localIpAddr, 0);

            // Define a MulticastOption object specifying the multicast group  
            // address and the local IP address. 
            // The multicast group address is the same as the address used by the listener.
            var mcastOption = new MulticastOption(_mcastAddress, localIpAddr);
            connector.SessionConfig.MulticastOption = mcastOption;

            // Call Connect() to force binding to the local IP address,
            // and get the associated multicast session.
            var session = connector.Connect(mcastEp).Await().Session;

            // Send multicast packets to the multicast endpoint.
            session.Write("hello 1", mcastEp);
            session.Write("hello 2", mcastEp);
            session.Write("hello 3", mcastEp);
        }
    }
}
