using System;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// An <see cref="IOHandler"/> which executes an <see cref="IoHandlerChain"/>
    /// on a <tt>messageReceived</tt> event.
    /// </summary>
    public class ChainedIoHandler : IOHandlerAdapter
    {
        /// <summary>
        /// </summary>
        public ChainedIoHandler()
            : this(new IoHandlerChain())
        { }

        /// <summary>
        /// </summary>
        public ChainedIoHandler(IoHandlerChain chain)
        {
            if (chain == null)
                throw new ArgumentNullException(nameof(chain));
            Chain = chain;
        }

        /// <summary>
        /// Gets the associated <see cref="IoHandlerChain"/>.
        /// </summary>
        public IoHandlerChain Chain { get; }

        /// <inheritdoc/>
        public override void MessageReceived(IOSession session, object message)
        {
            Chain.Execute(null, session, message);
        }
    }
}
