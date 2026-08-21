using System.Net;
using System.Net.Sockets;

namespace DevToolbox.Tools.CidrCalculator
{
    public readonly record struct CidrResult(
        int PrefixLength,
        string NetworkAddress,
        string BroadcastAddress,
        string SubnetMask,
        string WildcardMask,
        string FirstUsableHost,
        string LastUsableHost,
        long TotalAddresses,
        long UsableHosts);

    public static class CidrCalculatorService
    {
        /// <summary>Parses "&lt;ipv4&gt;/&lt;prefix&gt;" and computes the subnet's network/broadcast/usable-range details.</summary>
        public static CidrResult Calculate(string input)
        {
            input = (input ?? string.Empty).Trim();
            if (input.Length == 0) throw new FormatException("Enter an IPv4 address with a CIDR prefix, e.g. 192.168.1.10/24.");

            var parts = input.Split('/');
            if (parts.Length != 2) throw new FormatException("Expected the form <ip>/<prefix>, e.g. 192.168.1.10/24.");

            if (!IPAddress.TryParse(parts[0].Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
                throw new FormatException($"'{parts[0].Trim()}' is not a valid IPv4 address.");

            if (!int.TryParse(parts[1].Trim(), out var prefix) || prefix < 0 || prefix > 32)
                throw new FormatException("Prefix must be a number from 0 to 32.");

            var ipUint = ToUInt32(ip.GetAddressBytes());

            // Shifting a 32-bit uint by 32 is undefined in C# (the shift count wraps mod 32),
            // so /0 needs an explicit zero mask instead of relying on the shift formula.
            var maskUint = prefix == 0 ? 0u : unchecked(0xFFFFFFFFu << (32 - prefix));
            var wildcardUint = ~maskUint;

            var networkUint = ipUint & maskUint;
            var broadcastUint = networkUint | wildcardUint;

            // 1L << 32 is well-defined on a 64-bit long (unlike on a 32-bit uint/int), so this
            // stays correct even for /0.
            var totalAddresses = 1L << (32 - prefix);

            string firstUsable, lastUsable;
            long usableHosts;

            if (prefix >= 31)
            {
                // /31 (point-to-point, RFC 3021) and /32 (single host) have no separate
                // network/broadcast address to exclude - every address in range is usable.
                firstUsable = ToIpString(networkUint);
                lastUsable = ToIpString(broadcastUint);
                usableHosts = prefix == 32 ? 1 : 2;
            }
            else
            {
                firstUsable = ToIpString(networkUint + 1);
                lastUsable = ToIpString(broadcastUint - 1);
                usableHosts = totalAddresses - 2;
            }

            return new CidrResult(
                prefix,
                ToIpString(networkUint),
                ToIpString(broadcastUint),
                ToIpString(maskUint),
                ToIpString(wildcardUint),
                firstUsable,
                lastUsable,
                totalAddresses,
                usableHosts);
        }

        /// <summary>Renders a CidrResult as the multi-line summary shown in the output box.</summary>
        public static string FormatSummary(CidrResult r) => string.Join("\r\n", new[]
        {
            $"Network Address: {r.NetworkAddress}/{r.PrefixLength}",
            $"Broadcast Address: {r.BroadcastAddress}",
            $"Subnet Mask: {r.SubnetMask}",
            $"Wildcard Mask: {r.WildcardMask}",
            $"First Usable Host: {r.FirstUsableHost}",
            $"Last Usable Host: {r.LastUsableHost}",
            $"Total Addresses: {r.TotalAddresses:N0}",
            $"Usable Hosts: {r.UsableHosts:N0}"
        });

        private static uint ToUInt32(byte[] bigEndianBytes) =>
            ((uint)bigEndianBytes[0] << 24) | ((uint)bigEndianBytes[1] << 16) | ((uint)bigEndianBytes[2] << 8) | bigEndianBytes[3];

        private static string ToIpString(uint value) => new IPAddress(new[]
        {
            (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value
        }).ToString();
    }
}
