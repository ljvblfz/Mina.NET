using System.Net;
using Mina.Util;

namespace Mina.Core.Session
{
    /// <summary>
    /// An <see cref="IOSessionRecycler"/> with sessions that time out on inactivity.
    /// </summary>
    public class ExpiringSessionRecycler : IOSessionRecycler
    {
        private readonly ExpiringMap<EndPoint, IOSession> _sessionMap;

        public ExpiringSessionRecycler()
            : this(new ExpiringMap<EndPoint, IOSession>())
        {
        }

        public ExpiringSessionRecycler(int timeToLive)
            : this(new ExpiringMap<EndPoint, IOSession>(timeToLive))
        {
        }

        public ExpiringSessionRecycler(int timeToLive, int expirationInterval)
            : this(new ExpiringMap<EndPoint, IOSession>(timeToLive, expirationInterval))
        {
        }

        private ExpiringSessionRecycler(ExpiringMap<EndPoint, IOSession> map)
        {
            _sessionMap = map;
            _sessionMap.Expired += (sender, e) => e.Object.Close(true);
        }

        /// <inheritdoc/>
        public void Put(IOSession session)
        {
            _sessionMap.StartExpiring();
            var key = session.RemoteEndPoint;
            if (!_sessionMap.ContainsKey(key))
            {
                _sessionMap.Add(key, session);
            }
        }

        /// <inheritdoc/>
        public IOSession Recycle(EndPoint remoteEndPoint)
        {
            return _sessionMap[remoteEndPoint];
        }

        /// <inheritdoc/>
        public void Remove(IOSession session)
        {
            _sessionMap.Remove(session.RemoteEndPoint);
        }

        public void StopExpiring()
        {
            _sessionMap.StopExpiring();
        }

        public int ExpirationInterval
        {
            get { return _sessionMap.ExpirationInterval; }
            set { _sessionMap.ExpirationInterval = value; }
        }

        public int TimeToLive
        {
            get { return _sessionMap.TimeToLive; }
            set { _sessionMap.TimeToLive = value; }
        }
    }
}
