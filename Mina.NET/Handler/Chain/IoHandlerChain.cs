using System;
using System.Text;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// A chain of <see cref="IOHandlerCommand"/>s.
    /// </summary>
    public class IoHandlerChain : Chain<IoHandlerChain, IOHandlerCommand, INextCommand>, IOHandlerCommand
    {
        private static volatile int _nextId;

        private readonly int _id = _nextId++;
        private readonly string _nextCommandKey;

        /// <summary>
        /// </summary>
        public IoHandlerChain()
            : base(
            e => new NextCommand(e),
            () => new HeadCommand(),
            () => new TailCommand(typeof(IoHandlerChain).Name + "." + Guid.NewGuid() + ".nextCommand")
            )
        {
            _nextCommandKey = ((TailCommand)Tail.Filter).NextCommandKey;
        }

        /// <inheritdoc/>
        public void Execute(INextCommand next, IOSession session, object message)
        {
            if (next != null)
                session.SetAttribute(_nextCommandKey, next);

            try
            {
                CallNextCommand(Head, session, message);
            }
            finally
            {
                session.RemoveAttribute(_nextCommandKey);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var buf = new StringBuilder();
            buf.Append("{ ");

            var empty = true;
            var e = Head.NextEntry;
            while (e != Tail)
            {
                if (empty)
                    empty = false;
                else
                    buf.Append(", ");

                buf.Append('(');
                buf.Append(e.Name);
                buf.Append(':');
                buf.Append(e.Filter);
                buf.Append(')');

                e = e.NextEntry;
            }

            if (empty)
                buf.Append("empty");

            return buf.Append(" }").ToString();
        }

        private static void CallNextCommand(IEntry<IOHandlerCommand, INextCommand> entry, IOSession session, object message)
        {
            entry.Filter.Execute(entry.NextFilter, session, message);
        }

        class HeadCommand : IOHandlerCommand
        {
            public void Execute(INextCommand next, IOSession session, object message)
            {
                next.Execute(session, message);
            }
        }

        class TailCommand : IOHandlerCommand
        {
            public readonly string NextCommandKey;

            public TailCommand(string nextCommandKey)
            {
                this.NextCommandKey = nextCommandKey;
            }

            public void Execute(INextCommand next, IOSession session, object message)
            {
                next = session.GetAttribute<INextCommand>(NextCommandKey);
                if (next != null)
                    next.Execute(session, message);
            }
        }

        class NextCommand : INextCommand
        {
            readonly Entry _entry;

            public NextCommand(Entry entry)
            {
                _entry = entry;
            }

            public void Execute(IOSession session, object message)
            {
                CallNextCommand(_entry.NextEntry, session, message);
            }
        }
    }
}
