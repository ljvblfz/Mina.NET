using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Common.Logging;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A {@link IoFilter} which blocks connections from blacklisted remote address.
    /// </summary>
    public class BlacklistFilter : IOFilterAdapter
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(BlacklistFilter));

        private readonly List<Subnet> _blacklist = new List<Subnet>();

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IOSession session)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionCreated(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IOSession session)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionOpened(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IOSession session)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionClosed(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IOSession session, IdleStatus status)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionIdle(nextFilter, session, status);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IOSession session, object message)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.MessageReceived(nextFilter, session, message);
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IOSession session, IWriteRequest writeRequest)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.MessageSent(nextFilter, session, writeRequest);
        }

        /// <summary>
        /// Sets the addresses to be blacklisted.
        /// </summary>
        public void SetBlacklist(IEnumerable<IPAddress> addresses)
        {
            if (addresses == null)
                throw new ArgumentNullException(nameof(addresses));
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Clear();
                foreach (var addr in addresses)
                {
                    Block(addr);
                }
            }
        }

        /// <summary>
        /// Sets the subnets to be blacklisted.
        /// </summary>
        public void SetSubnetBlacklist(Subnet[] subnets)
        {
            if (subnets == null)
                throw new ArgumentNullException(nameof(subnets));
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Clear();
                foreach (var subnet in subnets)
                {
                    Block(subnet);
                }
            }
        }

        /// <summary>
        /// Blocks the specified endpoint.
        /// </summary>
        public void Block(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            Block(new Subnet(address, 32));
        }

        /// <summary>
        /// Blocks the specified subnet.
        /// </summary>
        public void Block(Subnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Add(subnet);
            }
        }

        /// <summary>
        /// Unblocks the specified endpoint.
        /// </summary>
        public void Unblock(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            Unblock(new Subnet(address, 32));
        }

        /// <summary>
        /// Unblocks the specified subnet.
        /// </summary>
        private void Unblock(Subnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException(nameof(subnet));
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Remove(subnet);
            }
        }

        private void BlockSession(IOSession session)
        {
            if (Log.IsWarnEnabled)
                Log.Warn("Remote address in the blacklist; closing.");
            session.Close(true);
        }

        private bool IsBlocked(IOSession session)
        {
            var ep = session.RemoteEndPoint as IPEndPoint;
            if (ep != null)
            {
                var address = ep.Address;

                // check all subnets
                lock (((IList)_blacklist).SyncRoot)
                {
                    foreach (var subnet in _blacklist)
                    {
                        if (subnet.InSubnet(address))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
