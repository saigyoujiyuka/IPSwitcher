using System.Text.Json.Serialization;

namespace IPSwitcher.Models;

/// <summary>
/// IPv4 address assignment of an interface, mirroring the Windows 11 Settings app
/// ("IP 分配": automatic (DHCP) / manual) plus the app's own "leave it alone" state.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Ipv4Mode
{
    /// <summary>不修改 — leave the interface's IPv4 configuration untouched.</summary>
    Unchanged = 0,

    /// <summary>自动（DHCP）— address, mask and gateway are obtained automatically.</summary>
    Automatic = 1,

    /// <summary>手动（静态）— static address, mask and gateway.</summary>
    Manual = 2,
}

/// <summary>Shared, user-visible (Simplified Chinese) text for <see cref="Ipv4Mode"/>.</summary>
public static class Ipv4ModeInfo
{
    /// <summary>ComboBox item text.</summary>
    public static string DisplayLabel(Ipv4Mode mode) => mode switch
    {
        Ipv4Mode.Automatic => "自动（DHCP）",
        Ipv4Mode.Manual => "手动（静态）",
        _ => "不修改",
    };

    /// <summary>Compact profile-summary fragment.</summary>
    public static string SummaryTag(Ipv4Mode mode) => mode switch
    {
        Ipv4Mode.Automatic => "DHCP",
        Ipv4Mode.Manual => string.Empty,
        _ => "不修改",
    };
}
