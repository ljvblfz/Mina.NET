using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Example.Chat.Server
{
    class ChatProtocolHandler : IOHandlerAdapter
    {
#if NET20
        IDictionary<IoSession, Boolean> sessions = new Dictionary<IoSession, Boolean>();
        IDictionary<String, Boolean> users = new Dictionary<String, Boolean>();
#else
        IDictionary<IOSession, bool> _sessions = new ConcurrentDictionary<IOSession, bool>();
        IDictionary<string, bool> _users = new ConcurrentDictionary<string, bool>();
#endif

        public void Broadcast(string message)
        {
            foreach (var session in _sessions.Keys)
            {
                if (session.Connected)
                    session.Write("BROADCAST OK " + message);
            }
        }

        public override void ExceptionCaught(IOSession session, Exception cause)
        {
            Console.WriteLine("Unexpected exception." + cause);
            session.Close(true);
        }

        public override void SessionClosed(IOSession session)
        {
            var user = session.GetAttribute<string>("user");
            _sessions.Remove(session);
            if (user != null)
            {
                _users.Remove(user);
                Broadcast("The user " + user + " has left the chat session.");
            }
        }

        public override void MessageReceived(IOSession session, object message)
        {
            var theMessage = (string)message;
            var result = theMessage.Split(new char[] { ' ' }, 2);
            var theCommand = result[0];

            var user = session.GetAttribute<string>("user");

            if (string.Equals("QUIT", theCommand, StringComparison.OrdinalIgnoreCase))
            {
                session.Write("QUIT OK");
                session.Close(true);
            }
            else if (string.Equals("LOGIN", theCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (user != null)
                {
                    session.Write("LOGIN ERROR user " + user + " already logged in.");
                    return;
                }

                if (result.Length == 2)
                {
                    user = result[1];
                }
                else
                {
                    session.Write("LOGIN ERROR invalid login command.");
                    return;
                }

                // check if the username is already used
                if (_users.ContainsKey(user))
                {
                    session.Write("LOGIN ERROR the name " + user + " is already used.");
                    return;
                }

                _sessions[session] = true;
                session.SetAttribute("user", user);

                // Allow all users
                _users[user] = true;
                session.Write("LOGIN OK");
                Broadcast("The user " + user + " has joined the chat session.");
            }
            else if (string.Equals("BROADCAST", theCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (result.Length == 2)
                {
                    Broadcast(user + ": " + result[1]);
                }
            }
            else
            {
                Console.WriteLine("Unhandled command: " + theCommand);
            }
        }
    }
}
