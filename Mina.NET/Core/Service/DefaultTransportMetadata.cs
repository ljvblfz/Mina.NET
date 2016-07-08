using System;

namespace Mina.Core.Service
{
    /// <summary>
    /// A default immutable implementation of <see cref="ITransportMetadata"/>.
    /// </summary>
    public class DefaultTransportMetadata : ITransportMetadata
    {
        /// <summary>
        /// </summary>
        public DefaultTransportMetadata(string providerName, string name,
            bool connectionless, bool fragmentation, Type endpointType)
        {
            if (providerName == null)
            {
                throw new ArgumentNullException(nameof(providerName));
            }
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            providerName = providerName.Trim().ToLowerInvariant();
            if (providerName.Length == 0)
            {
                throw new ArgumentException("providerName is empty", nameof(providerName));
            }
            name = name.Trim().ToLowerInvariant();
            if (name.Length == 0)
            {
                throw new ArgumentException("name is empty", nameof(name));
            }

            if (endpointType == null)
            {
                throw new ArgumentNullException(nameof(endpointType));
            }

            ProviderName = providerName;
            Name = name;
            Connectionless = connectionless;
            HasFragmentation = fragmentation;
            EndPointType = endpointType;
        }

        /// <inheritdoc/>
        public string ProviderName { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public bool Connectionless { get; }

        /// <inheritdoc/>
        public bool HasFragmentation { get; }

        /// <inheritdoc/>
        public Type EndPointType { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }
    }
}
