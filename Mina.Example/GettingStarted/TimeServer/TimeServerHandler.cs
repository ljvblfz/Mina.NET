using System;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Example.GettingStarted.TimeServer
{
    /// <summary>
    /// The Time Server handler : it return the current date when a message is received,
    /// or close the session if the "quit" message is received.
    /// </summary>
    class TimeServerHandler : IOHandlerAdapter
    {
        /// <summary>
        /// Trap exceptions.
        /// </summary>
        public override void ExceptionCaught(IOSession session, Exception cause)
        {
            Console.WriteLine(cause);
        }

        /// <summary>
        /// If the message is 'quit', we exit by closing the session. Otherwise,
        /// we return the current date.
        /// </summary>
        public override void MessageReceived(IOSession session, object message)
        {
            var str = message.ToString();

            // "Quit" ? let's get out ...
            if (str.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                session.Close(true);
                return;
            }

            // Send the current date back to the client
            session.Write(DateTime.Now.ToString());
            Console.WriteLine("Message written...");
        }

        /// <summary>
        /// On idle, we just write a message on the console
        /// </summary>
        public override void SessionIdle(IOSession session, IdleStatus status)
        {
            Console.WriteLine("IDLE " + session.GetIdleCount(status));
        }
    }
}
