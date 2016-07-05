using Mina.Core.Session;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An interface that builds <see cref="IOFilterChain"/> in predefined way
    /// when <see cref="IOSession"/> is created.
    /// </summary>
    public interface IOFilterChainBuilder
    {
        /// <summary>
        /// Builds the specified <paramref name="chain"/>.
        /// </summary>
        /// <param name="chain">the chain to build</param>
        void BuildFilterChain(IOFilterChain chain);
    }
}
