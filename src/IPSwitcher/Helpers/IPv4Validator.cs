using System.Net;
using System.Net.Sockets;

namespace IPSwitcher.Helpers;

public static class IPv4Validator
{
    public static bool TryParseAddress(string? text, out IPAddress? address)
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

        if (parsed.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        address = parsed;
        return true;
    }

    public static bool IsValidAddress(string? text)
    {
        return TryParseAddress(text, out _);
    }

    public static bool IsValidSubnetMask(string? text)
    {
        if (!TryParseAddress(text, out var parsed) || parsed is null)
        {
            return false;
        }

        var bytes = parsed.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return false;
        }

        uint value = ((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                     ((uint)bytes[2] << 8) | (uint)bytes[3];

        if (value == 0u)
        {
            return false;
        }

        uint inv = ~value;
        return (inv & (inv + 1)) == 0u;
    }

    public static bool IsValidOptionalAddress(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }
        return IsValidAddress(text);
    }
}
