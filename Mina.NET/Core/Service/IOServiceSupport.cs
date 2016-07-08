using Mina.Core.Session;

namespace Mina.Core.Service
{
    interface IOServiceSupport
    {
        void FireServiceActivated();

        void FireServiceIdle(IdleStatus idleStatus);

        void FireSessionCreated(IOSession session);

        void FireSessionDestroyed(IOSession session);

        void FireServiceDeactivated();
    }
}
