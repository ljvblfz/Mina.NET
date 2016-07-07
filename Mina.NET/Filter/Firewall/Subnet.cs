using System;
using System.Net;
using System.Net.Sockets;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A IP subnet using the CIDR notation. Currently, only IPv4 address are supported.
    /// </summary>
    public class Subnet
    {
        private const uint IpMaskV4 = 0x80000000;
        private const ulong IpMaskV6 = 0x8000000000000000L;
        private const int ByteMask = 0xFF;

        private IPAddress _subnet;

        /// <summary>
        /// An int representation of a subnet for IPV4 addresses.
        /// </summary>
        private int _subnetInt;

        /// <summary>
        /// An long representation of a subnet for IPV6 addresses.
        /// </summary>
        private long _subnetLong;

        private long _subnetMask;
        private int _suffix;

        /// <summary>
        /// Creates a subnet from CIDR notation.
        /// For example, the subnet 192.168.0.0/24 would be created using the
        /// <see cref="IPAddress"/> 192.168.0.0 and the mask 24.
        /// </summary>
        /// <param name="subnet">the <see cref="IPAddress"/> of the subnet</param>
        /// <param name="mask">the mask</param>
        public Subnet(IPAddress subnet, int mask)
        {
            if (subnet == null)
            {
                throw new ArgumentNullException(nameof(subnet));
            }

            if (subnet.AddressFamily == AddressFamily.InterNetwork)
            {
                if (mask < 0 || mask > 32)
                {
                    throw new ArgumentException("Mask has to be an integer between 0 and 32 for an IPv4 address");
                }
                _subnet = subnet;
                _subnetInt = ToInt(subnet);
                _suffix = mask;

                // binary mask for this subnet
                unchecked
                {
                    _subnetMask = (int) IpMaskV4 >> (mask - 1);
                }
            }
            else if (subnet.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (mask < 0 || mask > 128)
                {
                    throw new ArgumentException("Mask has to be an integer between 0 and 128 for an IPv6 address");
                }
                _subnet = subnet;
                _subnetLong = ToLong(subnet);
                _suffix = mask;

                // binary mask for this subnet
                unchecked
                {
                    _subnetMask = (long) IpMaskV6 >> (mask - 1);
                }
            }
            else
            {
                throw new ArgumentException("Unsupported address family: " + subnet.AddressFamily, nameof(subnet));
            }
        }

        /// <summary>
        /// Checks if the <see cref="IPAddress"/> is within this subnet.
        /// </summary>
        /// <param name="address">the <see cref="IPAddress"/> to check</param>
        /// <returns>true if the address is within this subnet, otherwise false</returns>
        public bool InSubnet(IPAddress address)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }
            if (IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address))
            {
                return true;
            }
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                return ToSubnet32(address) == _subnetInt;
            }
            return ToSubnet64(address) == _subnetLong;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _subnet + "/" + _suffix;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var other = obj as Subnet;

            if (other == null)
            {
                return false;
            }

            return other._subnetInt == _subnetInt && other._suffix == _suffix;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 17*_subnetInt + _suffix;
        }

        private int ToSubnet32(IPAddress address)
        {
            return (int) (ToInt(address) & _subnetMask);
        }

        private long ToSubnet64(IPAddress address)
        {
            return ToLong(address) & _subnetMask;
        }

        private static int ToInt(IPAddress addr)
        {
            var address = addr.GetAddressBytes();
            var result = 0;
            for (var i = 0; i < address.Length; i++)
            {
                result <<= 8;
                result |= address[i] & ByteMask;
            }
            return result;
        }

        private static long ToLong(IPAddress addr)
        {
            var address = addr.GetAddressBytes();
            long result = 0;

            for (var i = 0; i < address.Length; i++)
            {
                result <<= 8;
                result |= (uint) (address[i] & ByteMask);
            }

            return result;
        }
    }
}
