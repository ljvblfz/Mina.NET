using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// Provides data structures to a newly created session.
    /// </summary>
    public interface IOSessionDataStructureFactory
    {
        IOSessionAttributeMap GetAttributeMap(IOSession session);
        IWriteRequestQueue GetWriteRequestQueue(IOSession session);
    }
}
