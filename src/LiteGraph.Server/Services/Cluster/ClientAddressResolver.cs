namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;

    /// <summary>
    /// Resolves the client address of a request that may have passed through load balancers.
    /// When TrustForwardedHeaders is on and the connection comes from a trusted proxy, X-Forwarded-For is read from right
    /// to left, skipping trusted proxies, and the first untrusted address is the client.  Otherwise the connection's own
    /// address is used, so a client cannot spoof its address by sending the header directly.  The result is for request
    /// history, audit, traces, and logs only; it is never used for access control.
    /// Thread safety: immutable after construction; safe for concurrent use.
    /// </summary>
    public class ClientAddressResolver
    {
        #region Public-Members

        /// <summary>
        /// True when forwarded headers are honored from trusted proxies.
        /// </summary>
        public bool TrustForwardedHeaders { get; }

        #endregion

        #region Private-Members

        private readonly List<byte[]> _TrustedNetworks = new List<byte[]>();
        private readonly List<int> _TrustedPrefixes = new List<int>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="trustForwardedHeaders">Honor X-Forwarded-For from trusted proxies.</param>
        /// <param name="trustedProxies">Trusted proxy addresses or CIDR ranges, for example 10.0.0.0/8 or 172.18.0.5.  Null for none.</param>
        /// <exception cref="ArgumentException">An entry is not an IP address or CIDR range.</exception>
        public ClientAddressResolver(bool trustForwardedHeaders, IEnumerable<string> trustedProxies)
        {
            TrustForwardedHeaders = trustForwardedHeaders;
            foreach (string entry in trustedProxies ?? Enumerable.Empty<string>())
            {
                if (String.IsNullOrWhiteSpace(entry)) continue;
                AddRange(entry.Trim());
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Resolve the client address.
        /// </summary>
        /// <param name="peerAddress">Address of the connection's peer.</param>
        /// <param name="forwardedFor">X-Forwarded-For header value, or null.</param>
        /// <returns>Client address; the peer address when forwarded headers are not trusted or not usable.</returns>
        public string Resolve(string peerAddress, string forwardedFor)
        {
            if (!TrustForwardedHeaders || _TrustedNetworks.Count == 0 || String.IsNullOrWhiteSpace(forwardedFor)) return peerAddress;
            if (!IsTrusted(peerAddress)) return peerAddress;

            string[] hops = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = hops.Length - 1; i >= 0; i--)
            {
                if (!IPAddress.TryParse(hops[i], out IPAddress hop)) return peerAddress;
                if (!IsTrusted(hop)) return Normalize(hop).ToString();
            }

            return hops.Length > 0 && IPAddress.TryParse(hops[0], out IPAddress first) ? Normalize(first).ToString() : peerAddress;
        }

        /// <summary>
        /// Check whether an address belongs to a trusted proxy.
        /// </summary>
        /// <param name="address">Address.</param>
        /// <returns>True if trusted.</returns>
        public bool IsTrusted(string address)
        {
            return !String.IsNullOrWhiteSpace(address) && IPAddress.TryParse(address, out IPAddress parsed) && IsTrusted(parsed);
        }

        #endregion

        #region Private-Methods

        private bool IsTrusted(IPAddress address)
        {
            address = Normalize(address);
            byte[] bytes = address.GetAddressBytes();
            for (int i = 0; i < _TrustedNetworks.Count; i++)
            {
                byte[] net = _TrustedNetworks[i];
                if (net.Length != bytes.Length) continue;
                if (PrefixMatches(bytes, net, _TrustedPrefixes[i])) return true;
            }
            return false;
        }

        private static bool PrefixMatches(byte[] address, byte[] network, int prefixLength)
        {
            int fullBytes = prefixLength / 8;
            for (int i = 0; i < fullBytes; i++)
            {
                if (address[i] != network[i]) return false;
            }

            int remainingBits = prefixLength % 8;
            if (remainingBits == 0) return true;
            int mask = 0xFF << (8 - remainingBits) & 0xFF;
            return (address[fullBytes] & mask) == (network[fullBytes] & mask);
        }

        private void AddRange(string entry)
        {
            string addressPart = entry;
            int prefix = -1;
            int slash = entry.IndexOf('/');
            if (slash >= 0)
            {
                addressPart = entry.Substring(0, slash);
                if (!Int32.TryParse(entry.Substring(slash + 1), out prefix))
                    throw new ArgumentException("Invalid trusted proxy '" + entry + "': the prefix length is not a number.");
            }

            if (!IPAddress.TryParse(addressPart, out IPAddress address))
                throw new ArgumentException("Invalid trusted proxy '" + entry + "': not an IP address or CIDR range.");

            address = Normalize(address);
            int maxPrefix = address.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
            if (prefix < 0) prefix = maxPrefix;
            if (prefix > maxPrefix)
                throw new ArgumentException("Invalid trusted proxy '" + entry + "': the prefix length exceeds " + maxPrefix + ".");

            _TrustedNetworks.Add(address.GetAddressBytes());
            _TrustedPrefixes.Add(prefix);
        }

        private static IPAddress Normalize(IPAddress address)
        {
            return address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        }

        #endregion
    }
}
