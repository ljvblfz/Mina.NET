using Mina.Core.Write;

namespace Mina.Core.Session
{
    class DefaultIOSessionDataStructureFactory : IOSessionDataStructureFactory
    {
        public IOSessionAttributeMap GetAttributeMap(IOSession session)
        {
            return new DefaultIOSessionAttributeMap();
        }

        public IWriteRequestQueue GetWriteRequestQueue(IOSession session)
        {
            return new DefaultWriteRequestQueue();
        }
    }
}
