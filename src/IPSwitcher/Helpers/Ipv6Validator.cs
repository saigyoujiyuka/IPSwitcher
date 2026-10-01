using System.Net;
using System.Net.Sockets;

namespace IPSwitcher.Helpers;

public static class Ipv6Validator
{
    /// <summary>
    /// Parses an IPv6 address. <paramref name="allowScope"/> permits a zone index
    /// (<c>fe80::1%8</c>), which link-local gateways and DNS servers require.
    /// </summary>
    public static bool TryParseAddress(string? text, bool allowScope, out IPAddress? address)
    {
        address = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!IPAddress.TryParse(text.Trim(), out var parsed))
        {
            return false;
        }

        if (parsed.AddressFamily != AddressFamily.InterNetworkV6)
        {
            return false;
        }

        if (!allowScope && parsed.ScopeId != 0)
        {
            return false;
        }

        address = parsed;
        return true;
    }

    /// <summary>Interface address: a plain IPv6 address without a zone index.</summary>
    public static bool IsValidAddress(string? text) => TryParseAddress(text, allowScope: false, out _);

    /// <summary>Gateway or DNS server: a zone index is allowed for link-local addresses.</summary>
    public static bool IsValidScopedAddress(string? text) => TryParseAddress(text, allowScope: true, out _);

    public static bool IsValidOptionalScopedAddress(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return IsValidScopedAddress(text);
    }

    /// <summary>Subnet prefix length, 0–128.</summary>
    public static bool TryParsePrefixLength(string? text, out int prefixLength)
    {
        prefixLength = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (!int.TryParse(text.Trim(), out var value))
        {
            return false;
        }

        if (value is < 0 or > 128)
        {
            return false;
        }

        prefixLength = value;
        return true;
    }

    /// <summary>
    /// Strips a zone index from a gateway address: netsh already knows the interface it is
    /// configuring, and it rejects the <c>%index</c> form there.
    /// </summary>
    public static string StripScope(string address)
    {
        var value = address.Trim();
        var percent = value.IndexOf('%');
        return percent < 0 ? value : value[..percent];
    }
}
