using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// Represents an indirect reference to the next <see cref="IOHandlerCommand"/> of
    /// the <see cref="IOHandlerChain"/>.
    /// </summary>
    public interface INextCommand
    {
        void Execute(IOSession session, object message);
    }
}
