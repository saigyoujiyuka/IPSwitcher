using System.Text.Json.Serialization;

namespace IPSwitcher.Models;

/// <summary>
/// IPv6 address assignment of an interface. Mirrors the Windows 11 Settings behaviour, where the
/// "IPv6" switch turned <b>off</b> is equivalent to obtaining the address automatically
/// (DHCPv6 / router advertisements) rather than disabling the protocol.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Ipv6Mode
{
    /// <summary>不修改 — leave the interface's IPv6 address configuration untouched.</summary>
    Unchanged = 0,

    /// <summary>自动（DHCPv6）— the state Windows shows when the IPv6 switch is off.</summary>
    Automatic = 1,

    /// <summary>手动（静态）— static address, prefix length and gateway.</summary>
    Manual = 2,
}

/// <summary>Shared, user-visible (Simplified Chinese) text for <see cref="Ipv6Mode"/>.</summary>
public static class Ipv6ModeInfo
{
    /// <summary>ComboBox item text.</summary>
    public static string DisplayLabel(Ipv6Mode mode) => mode switch
    {
        Ipv6Mode.Automatic => "自动（DHCPv6）",
        Ipv6Mode.Manual => "手动（静态）",
        _ => "不修改",
    };

    /// <summary>Compact profile-summary fragment; empty when IPv6 is left alone.</summary>
    public static string SummaryTag(Ipv6Mode mode) => mode switch
    {
        Ipv6Mode.Automatic => "IPv6自动",
        Ipv6Mode.Manual => "IPv6手动",
        _ => string.Empty,
    };
}
