using Mina.Core.Service;
using Mina.Transport.Loopback;

namespace Mina.Example.Tennis
{
    class Program
    {
        static void Main(string[] args)
        {
            IOAcceptor acceptor = new LoopbackAcceptor();
            var lep = new LoopbackEndPoint(8080);

            // Set up server
            acceptor.Handler = new TennisPlayer();
            acceptor.Bind(lep);

            // Connect to the server.
            var connector = new LoopbackConnector();
            connector.Handler = new TennisPlayer();
            var future = connector.Connect(lep);
            future.Await();
            var session = future.Session;

            // Send the first ping message
            session.Write(new TennisBall(10));

            // Wait until the match ends.
            session.CloseFuture.Await();

            acceptor.Unbind();
        }
    }
}
