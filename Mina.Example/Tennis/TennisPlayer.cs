using System;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Example.Tennis
{
    class TennisPlayer : IOHandlerAdapter
    {
        private static int _nextId;
        /// <summary>
        /// Player ID
        /// </summary>
        private readonly int _id = _nextId++;

        public override void SessionOpened(IOSession session)
        {
            Console.WriteLine("Player-" + _id + ": READY");
        }

        public override void SessionClosed(IOSession session)
        {
            Console.WriteLine("Player-" + _id + ": QUIT");
        }

        public override void MessageReceived(IOSession session, object message)
        {
            Console.WriteLine("Player-" + _id + ": RCVD " + message);

            var ball = (TennisBall)message;

            // Stroke: TTL decreases and PING/PONG state changes.
            ball = ball.Stroke();

            if (ball.Ttl > 0)
            {
                // If the ball is still alive, pass it back to peer.
                session.Write(ball);
            }
            else
            {
                // If the ball is dead, this player loses.
                Console.WriteLine("Player-" + _id + ": LOSE");
                session.Close(true);
            }
        }

        public override void MessageSent(IOSession session, object message)
        {
            Console.WriteLine("Player-" + _id + ": SENT " + message);
        }

        public override void ExceptionCaught(IOSession session, Exception cause)
        {
            Console.WriteLine(cause);
            session.Close(true);
        }
    }
}
