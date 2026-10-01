using System.Net.NetworkInformation;
using IPSwitcher.Models;
using Microsoft.Win32;

namespace IPSwitcher.Services;

/// <summary>Result of a single DoH configuration write.</summary>
public sealed record DohApplyResult(bool Success, string? Error, IReadOnlyList<string> Logs)
{
    public static DohApplyResult Ok(IReadOnlyList<string> logs) => new(true, null, logs);

    public static DohApplyResult Fail(string error, IReadOnlyList<string> logs) => new(false, error, logs);
}

/// <summary>DoH state of one DNS server on one interface, as stored by Windows.</summary>
public sealed record DohServerState(DohMode Mode, string? Template, bool AllowFallback)
{
    public static DohServerState Off { get; } = new(DohMode.Off, null, false);
}

/// <summary>
/// Reads and writes the per-interface DNS over HTTPS settings used by the Windows 11 Settings app
/// (Network &amp; internet → adapter → "Edit IP settings" → "DNS over HTTPS").
///
/// Windows stores them in the registry — nothing in <c>netsh</c> or the <c>DnsClient</c> PowerShell
/// module writes the per-interface part, which is exactly why the Settings app must be reproduced here:
///
/// <code>
/// HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters
///   \{InterfaceGuid}\DohInterfaceSettings\Doh \{IPv4}      (Doh6\{IPv6} for IPv6 servers)
///     DohFlags    REG_QWORD   1 = automatic template
///                             2 = manual template
///                             4 = fall back to unencrypted DNS (OR-ed onto 1 or 2 → 5 / 6)
///                             0 / missing = DoH off
///     DohTemplate REG_SZ      template URL, used when DohFlags has the manual-template bit
///
/// HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\{IP}
///     Template    REG_SZ      template Windows knows for that resolver ("automatic template")
/// </code>
/// </summary>
public sealed class DohSettingsService
{
    private const string InterfaceParametersPath =
        @"SYSTEM\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters";

    private const string DohInterfaceSettingsName = "DohInterfaceSettings";

    private const string WellKnownServersPath =
        @"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers";

    private const string ValueFlags = "DohFlags";

    private const string ValueTemplate = "DohTemplate";

    /// <summary>DoH is switched on for this server and Windows supplies the template.</summary>
    public const long FlagAutomaticTemplate = 0x1;

    /// <summary>DoH is switched on for this server and the user supplies the template.</summary>
    public const long FlagManualTemplate = 0x2;

    /// <summary>Allowed to fall back to unencrypted DNS when the encrypted query fails.</summary>
    public const long FlagFallbackToPlaintext = 0x4;

    /// <summary>DoH needs the Windows 11 / Server 2022 DNS client (build 20348+).</summary>
    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20348);

    /// <summary>Adapter GUID as it appears in the registry keys, e.g. <c>{58FE8AA3-...}</c>.</summary>
    public static string? GetInterfaceId(string adapterName)
    {
        if (string.IsNullOrWhiteSpace(adapterName))
        {
            return null;
        }

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (string.Equals(nic.Name, adapterName, StringComparison.OrdinalIgnoreCase))
            {
                return nic.Id;
            }
        }

        return null;
    }

    /// <summary>Template Windows already knows for this resolver address, or <c>null</c> when unknown.</summary>
    public string? TryGetKnownTemplate(string serverAddress)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"{WellKnownServersPath}\{serverAddress}");
            return key?.GetValue("Template") as string;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Current DoH state of one DNS server on one interface.</summary>
    public DohServerState Read(string? interfaceId, string serverAddress)
    {
        if (string.IsNullOrWhiteSpace(interfaceId) || string.IsNullOrWhiteSpace(serverAddress))
        {
            return DohServerState.Off;
        }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(ServerKeyPath(interfaceId, serverAddress));
            if (key is null)
            {
                return DohServerState.Off;
            }

            var flags = ReadFlags(key.GetValue(ValueFlags));
            var mode = (flags & FlagManualTemplate) != 0
                ? DohMode.Manual
                : (flags & FlagAutomaticTemplate) != 0
                    ? DohMode.Auto
                    : DohMode.Off;

            return new DohServerState(
                mode,
                key.GetValue(ValueTemplate) as string,
                (flags & FlagFallbackToPlaintext) != 0);
        }
        catch
        {
            return DohServerState.Off;
        }
    }

    /// <summary>
    /// Writes the DoH state of one DNS server on one interface, exactly as the Settings app does.
    /// <see cref="DohMode.Off"/> removes the per-server key.
    /// </summary>
    public DohApplyResult Apply(string? interfaceId, string serverAddress, DohMode mode, string? template, bool allowFallback)
    {
        var logs = new List<string>();

        if (!IsSupported)
        {
            return DohApplyResult.Fail("当前系统不支持 DNS over HTTPS（需要 Windows 11 / Windows Server 2022 及以上）。", logs);
        }

        if (string.IsNullOrWhiteSpace(interfaceId))
        {
            return DohApplyResult.Fail($"未找到适配器「{serverAddress}」对应的接口标识。", logs);
        }

        var path = ServerKeyPath(interfaceId, serverAddress);

        try
        {
            if (mode == DohMode.Off)
            {
                if (Registry.LocalMachine.OpenSubKey(path) is not null)
                {
                    Registry.LocalMachine.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
                    logs.Add($"  DoH 已关闭（删除注册表项 {path}）");
                }
                else
                {
                    logs.Add("  DoH 已关闭（无既有设置）");
                }

                return DohApplyResult.Ok(logs);
            }

            var flags = mode == DohMode.Manual ? FlagManualTemplate : FlagAutomaticTemplate;
            if (allowFallback)
            {
                flags |= FlagFallbackToPlaintext;
            }

            using var key = Registry.LocalMachine.CreateSubKey(path, writable: true);
            if (key is null)
            {
                return DohApplyResult.Fail($"无法创建注册表项 {path}。", logs);
            }

            key.SetValue(ValueFlags, flags, RegistryValueKind.QWord);

            if (mode == DohMode.Manual)
            {
                key.SetValue(ValueTemplate, template ?? string.Empty, RegistryValueKind.String);
                logs.Add($"  已写入 DoH 设置（手动模板，DohFlags={flags}）：{template}");
            }
            else
            {
                // Automatic template: the template must come from the system list, so any stale
                // per-interface template from a previous manual configuration is removed.
                key.DeleteValue(ValueTemplate, throwOnMissingValue: false);
                logs.Add($"  已写入 DoH 设置（自动模板，DohFlags={flags}）");
            }

            return DohApplyResult.Ok(logs);
        }
        catch (UnauthorizedAccessException)
        {
            return DohApplyResult.Fail("写入注册表被拒绝，请以管理员身份运行。", logs);
        }
        catch (Exception ex)
        {
            return DohApplyResult.Fail(ex.Message, logs);
        }
    }

    /// <summary>
    /// Removes every DoH entry of one interface. Windows Settings does the same whenever the DNS
    /// server list of that interface is rewritten or switched back to DHCP, so stale entries for
    /// resolvers that are no longer in use never survive.
    /// </summary>
    public DohApplyResult ClearInterface(string? interfaceId)
    {
        var logs = new List<string>();

        if (!IsSupported || string.IsNullOrWhiteSpace(interfaceId))
        {
            return DohApplyResult.Ok(logs);
        }

        var path = $@"{InterfaceParametersPath}\{interfaceId}\{DohInterfaceSettingsName}";

        try
        {
            if (Registry.LocalMachine.OpenSubKey(path) is null)
            {
                return DohApplyResult.Ok(logs);
            }

            Registry.LocalMachine.DeleteSubKeyTree(path, throwOnMissingSubKey: false);
            logs.Add("  已清除该适配器上原有的 DNS over HTTPS 设置");
            return DohApplyResult.Ok(logs);
        }
        catch (UnauthorizedAccessException)
        {
            return DohApplyResult.Fail("清除 DNS over HTTPS 设置被拒绝，请以管理员身份运行。", logs);
        }
        catch (Exception ex)
        {
            return DohApplyResult.Fail(ex.Message, logs);
        }
    }

    /// <summary>
    /// Validates a DoH template URL the way Windows does: it must be an https URL with a host and a path.
    /// </summary>
    public static bool IsValidTemplate(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return false;
        }

        var value = template.Trim();
        if (!value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
               && !string.IsNullOrWhiteSpace(uri.Host)
               && uri.Host.Contains('.')
               && uri.AbsolutePath.Length > 1;
    }

    private static string ServerKeyPath(string interfaceId, string serverAddress)
    {
        var family = serverAddress.Contains(':') ? "Doh6" : "Doh";
        return $@"{InterfaceParametersPath}\{interfaceId}\{DohInterfaceSettingsName}\{family}\{serverAddress}";
    }

    private static long ReadFlags(object? value) => value switch
    {
        long l => l,
        int i => i,
        byte[] bytes when bytes.Length >= 8 => BitConverter.ToInt64(bytes, 0),
        byte[] bytes when bytes.Length >= 4 => BitConverter.ToInt32(bytes, 0),
        _ => 0L,
    };
}
