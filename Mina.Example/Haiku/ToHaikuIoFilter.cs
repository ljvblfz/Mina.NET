using System.Collections.Generic;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Example.Haiku
{
    class ToHaikuIoFilter : IOFilterAdapter
    {
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            var phrases = session.GetAttribute<List<string>>("phrases");

            if (null == phrases)
            {
                phrases = new List<string>();
                session.SetAttribute("phrases", phrases);
            }

            phrases.Add((string)message);

            if (phrases.Count == 3)
            {
                session.RemoveAttribute("phrases");

                base.MessageReceived(nextFilter, session, new Haiku(phrases
                        .ToArray()));
            }
        }
    }
}
