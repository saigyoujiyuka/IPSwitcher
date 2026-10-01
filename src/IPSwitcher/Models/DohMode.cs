using System.Text.Json.Serialization;

namespace IPSwitcher.Models;

/// <summary>
/// DNS over HTTPS mode of a single DNS server, mirroring the three choices offered by
/// the Windows 11 Settings app ("DNS over HTTPS": Off / On (automatic template) / On (manual template)).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DohMode
{
    /// <summary>关 — plain DNS, no encryption.</summary>
    Off = 0,

    /// <summary>开（自动模板）— use the DoH template Windows already knows for this server address.</summary>
    Auto = 1,

    /// <summary>开（手动模板）— use the template supplied by the user.</summary>
    Manual = 2,
}

/// <summary>Shared, user-visible (Simplified Chinese) text for <see cref="DohMode"/>.</summary>
public static class DohModeInfo
{
    /// <summary>ComboBox item text, matching the Windows Settings wording.</summary>
    public static string DisplayLabel(DohMode mode) => mode switch
    {
        DohMode.Auto => "开（自动模板）",
        DohMode.Manual => "开（手动模板）",
        _ => "关",
    };

    /// <summary>Compact profile-summary fragment, e.g. <c>DoH(自动)</c>; empty when DoH is off.</summary>
    public static string SummaryTag(DohMode mode) => mode switch
    {
        DohMode.Auto => "DoH(自动)",
        DohMode.Manual => "DoH(手动)",
        _ => string.Empty,
    };

    /// <summary>Read-back text for the "current actual config" panel.</summary>
    public static string StateText(DohMode mode, bool allowFallback)
    {
        if (mode == DohMode.Off)
        {
            return "关";
        }

        var text = DisplayLabel(mode);
        return allowFallback ? $"{text} / 可回退" : $"{text} / 仅加密";
    }
}
