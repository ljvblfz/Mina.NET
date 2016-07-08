using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// A <see cref="IOHandlerCommand"/> encapsulates a unit of processing work to be
    /// performed, whose purpose is to examine and/or modify the state of a
    /// transaction that is represented by custom attributes provided by
    /// <see cref="IOSession"/>.
    /// </summary>
    public interface IOHandlerCommand
    {
        void Execute(INextCommand next, IOSession session, object message);
    }
}
