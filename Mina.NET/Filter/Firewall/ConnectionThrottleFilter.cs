using System;
using System.Collections.Concurrent;
using System.Net;
using Common.Logging;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A <see cref="IOFilter"/> which blocks connections from connecting
    /// at a rate faster than the specified interval.
    /// </summary>
    public class ConnectionThrottleFilter : IOFilterAdapter
    {
        static readonly long DefaultTime = 1000L;
        static readonly ILog Log = LogManager.GetLogger(typeof(ConnectionThrottleFilter));

        private readonly ConcurrentDictionary<string, DateTime> _clients = new ConcurrentDictionary<string, DateTime>();
        // TODO expire overtime clients

        /// <summary>
        /// Default constructor.  Sets the wait time to 1 second
        /// </summary>
        public ConnectionThrottleFilter()
            : this(DefaultTime)
        {
        }

        /// <summary>
        /// Constructor that takes in a specified wait time.
        /// </summary>
        /// <param name="allowedInterval">The number of milliseconds a client is allowed to wait before making another successful connection</param>
        public ConnectionThrottleFilter(long allowedInterval)
        {
            AllowedInterval = allowedInterval;
        }

        /// <summary>
        /// Gets or sets the minimal interval (ms) between connections from a client.
        /// </summary>
        public long AllowedInterval { get; set; }

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IOSession session)
        {
            if (!IsConnectionOk(session))
            {
                if (Log.IsWarnEnabled)
                {
                    Log.Warn("Connections coming in too fast; closing.");
                }
                session.Close(true);
            }
            base.SessionCreated(nextFilter, session);
        }

        /// <summary>
        /// Method responsible for deciding if a connection is OK to continue.
        /// </summary>
        /// <param name="session">the new session that will be verified</param>
        /// <returns>true if the session meets the criteria, otherwise false</returns>
        public bool IsConnectionOk(IOSession session)
        {
            var ep = session.RemoteEndPoint as IPEndPoint;
            if (ep != null)
            {
                var addr = ep.Address.ToString();
                var now = DateTime.Now;
                DateTime? lastConnTime = null;

                _clients.AddOrUpdate(addr, now, (k, v) =>
                {
                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug("This is not a new client");
                    }
                    lastConnTime = v;
                    return now;
                });

                if (lastConnTime.HasValue)
                {
                    // if the interval between now and the last connection is
                    // less than the allowed interval, return false
                    if ((now - lastConnTime.Value).TotalMilliseconds < AllowedInterval)
                    {
                        if (Log.IsWarnEnabled)
                        {
                            Log.Warn("Session connection interval too short");
                        }
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
