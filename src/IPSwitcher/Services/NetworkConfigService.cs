using System.Diagnostics;
using IPSwitcher.Helpers;
using IPSwitcher.Models;

namespace IPSwitcher.Services;

public sealed class NetworkConfigResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<string> Logs { get; init; } = Array.Empty<string>();

    public static NetworkConfigResult Ok(string message, IReadOnlyList<string> logs) =>
        new() { Success = true, Message = message, Logs = logs };

    public static NetworkConfigResult Fail(string message, IReadOnlyList<string> logs) =>
        new() { Success = false, Message = message, Logs = logs };
}

public sealed class NetworkConfigService
{
    private readonly DohSettingsService _dohSettings;

    public NetworkConfigService(DohSettingsService dohSettings)
    {
        _dohSettings = dohSettings;
    }

    public async Task<NetworkConfigResult> ApplyAsync(string adapterName, NetworkProfile profile, CancellationToken ct = default)
    {
        var logs = new List<string>();

        if (string.IsNullOrWhiteSpace(adapterName))
        {
            return NetworkConfigResult.Fail("未选择网络适配器。", logs);
        }

        switch (profile.Ipv4Mode)
        {
            case Ipv4Mode.Automatic:
            {
                var r1 = await RunNetshAsync(
                    $"interface ip set address name=\"{adapterName}\" source=dhcp", ct);
                logs.AddRange(r1.Logs);
                if (!r1.Success)
                {
                    return NetworkConfigResult.Fail($"设置 DHCP 地址失败：{r1.Error}", logs);
                }

                // A DNS server may still be configured by hand on top of a DHCP address,
                // exactly like the separate "DNS server assignment" setting in Windows.
                var r2 = await ApplyIpv4DnsAsync(adapterName, profile, logs, ct, autoWhenEmpty: true);
                if (!r2.Success)
                {
                    return r2;
                }

                break;
            }

            case Ipv4Mode.Manual:
            {
                var staticResult = await ApplyStaticAsync(adapterName, profile, logs, ct);
                if (!staticResult.Success)
                {
                    return staticResult;
                }

                break;
            }

            default:
            {
                logs.Add("> IPv4：地址保持不修改");

                var r = await ApplyIpv4DnsAsync(adapterName, profile, logs, ct, autoWhenEmpty: false);
                if (!r.Success)
                {
                    return r;
                }

                break;
            }
        }

        var ipv6Result = await ApplyIpv6Async(adapterName, profile, logs, ct);
        if (!ipv6Result.Success)
        {
            return ipv6Result;
        }

        var dohResult = await ApplyDohAsync(adapterName, profile, logs, ct);
        if (!dohResult.Success)
        {
            return NetworkConfigResult.Fail($"DNS over HTTPS 设置失败：{dohResult.Error}", logs);
        }

        if (profile.NetworkCategory.HasValue)
        {
            var rc2 = await SetNetworkCategoryAsync(adapterName, profile.NetworkCategory.Value, ct);
            logs.AddRange(rc2.Logs);
            if (!rc2.Success)
            {
                logs.Add($"(警告：设置网络类别失败：{rc2.Error})");
            }
        }

        var mode = profile.Ipv4Mode switch
        {
            Ipv4Mode.Automatic => "IPv4 自动获取（DHCP）",
            Ipv4Mode.Manual => $"IPv4 静态配置「{profile.Name}」",
            _ => "IPv4 不修改",
        };

        return NetworkConfigResult.Ok($"已将「{adapterName}」切换为 {mode}。", logs);
    }

    /// <summary>
    /// Applies the profile's IPv4 DNS servers. When the profile does not name any, the DNS source
    /// either becomes DHCP (Windows' "obtain DNS server address automatically") or is left alone.
    /// </summary>
    private async Task<NetworkConfigResult> ApplyIpv4DnsAsync(
        string adapterName, NetworkProfile profile, List<string> logs, CancellationToken ct, bool autoWhenEmpty)
    {
        if (string.IsNullOrWhiteSpace(profile.PrimaryDns))
        {
            if (!autoWhenEmpty)
            {
                logs.Add("  (未填写 DNS，保持原有 DNS 设置)");
                return NetworkConfigResult.Ok(string.Empty, logs);
            }

            var rd0 = await RunNetshAsync(
                $"interface ip set dns name=\"{adapterName}\" source=dhcp", ct);
            logs.AddRange(rd0.Logs);
            if (!rd0.Success)
            {
                return NetworkConfigResult.Fail($"设置 DHCP DNS 失败：{rd0.Error}", logs);
            }

            return NetworkConfigResult.Ok(string.Empty, logs);
        }

        var rc = await RunNetshAsync(
            $"interface ip delete dns name=\"{adapterName}\" all", ct);
        logs.AddRange(rc.Logs);

        var rd = await RunNetshAsync(
            $"interface ip set dns name=\"{adapterName}\" source=static address={profile.PrimaryDns} register=primary validate=no", ct);
        logs.AddRange(rd.Logs);
        if (!rd.Success)
        {
            return NetworkConfigResult.Fail($"设置首选 DNS 失败：{rd.Error}", logs);
        }

        if (!string.IsNullOrWhiteSpace(profile.SecondaryDns))
        {
            var rd2 = await RunNetshAsync(
                $"interface ip add dns name=\"{adapterName}\" address={profile.SecondaryDns} index=2 validate=no", ct);
            logs.AddRange(rd2.Logs);
            if (!rd2.Success)
            {
                logs.Add($"(警告：添加备用 DNS 失败：{rd2.Error})");
            }
        }

        return NetworkConfigResult.Ok(string.Empty, logs);
    }

    /// <summary>
    /// Mirrors the Windows Settings behaviour for "DNS over HTTPS": every existing DoH entry of the
    /// interface is dropped first (the Settings app does the same whenever the DNS list is rewritten)
    /// and then one entry per configured DNS server is written.
    /// </summary>
    private async Task<DohApplyResult> ApplyDohAsync(string adapterName, NetworkProfile profile, List<string> logs, CancellationToken ct)
    {
        logs.Add("> DoH（DNS over HTTPS）");

        var servers = DohServers(profile).ToList();
        var anyRequested = servers.Any(s => s.Mode != DohMode.Off);

        if (!DohSettingsService.IsSupported)
        {
            if (anyRequested)
            {
                return DohApplyResult.Fail(
                    "当前系统不支持 DNS over HTTPS（需要 Windows 11 / Windows Server 2022 及以上），请将该配置的 DoH 设为「关」。", logs);
            }

            logs.Add("  当前系统不支持 DoH，已跳过");
            return DohApplyResult.Ok(logs);
        }

        var interfaceId = DohSettingsService.GetInterfaceId(adapterName);
        if (string.IsNullOrWhiteSpace(interfaceId))
        {
            if (anyRequested)
            {
                return DohApplyResult.Fail($"未找到适配器「{adapterName}」对应的接口标识。", logs);
            }

            logs.Add("  未找到适配器对应的接口标识，已跳过 DoH");
            return DohApplyResult.Ok(logs);
        }

        var cleared = _dohSettings.ClearInterface(interfaceId);
        logs.AddRange(cleared.Logs);
        if (!cleared.Success)
        {
            return cleared;
        }

        if (servers.Count == 0)
        {
            logs.Add("  没有需要配置 DoH 的 DNS 服务器");
        }

        foreach (var server in servers)
        {
            if (server.Mode == DohMode.Off)
            {
                logs.Add($"  {server.Address}：DoH 关");
                continue;
            }

            if (server.Mode == DohMode.Manual && !DohSettingsService.IsValidTemplate(server.Template))
            {
                return DohApplyResult.Fail(
                    $"{server.Address} 的 DoH 模板无效，需为 https:// 开头的完整地址。", logs);
            }

            if (server.Mode == DohMode.Auto &&
                _dohSettings.TryGetKnownTemplate(server.Address) is null)
            {
                logs.Add($"  (警告：系统未内置 {server.Address} 的 DoH 模板，自动模板可能不生效；" +
                         "请改用「开（手动模板）」)");
            }

            var applied = _dohSettings.Apply(
                interfaceId, server.Address, server.Mode, server.Template, server.AllowFallback);
            logs.AddRange(applied.Logs);
            if (!applied.Success)
            {
                return applied;
            }
        }

        // Make the DNS client pick the new settings up immediately instead of at the next network change.
        var refresh = await RunPowerShellAsync("Register-DnsClient; Clear-DnsClientCache", ct);
        logs.AddRange(refresh.Logs);
        if (!refresh.Success)
        {
            logs.Add($"(警告：刷新 DNS 客户端失败：{refresh.Error})");
        }

        return DohApplyResult.Ok(logs);
    }

    /// <summary>
    /// Every DNS server the profile may carry DoH settings for: the IPv4 pair (only meaningful with a
    /// static IPv4 configuration) and the IPv6 pair (<c>Doh6</c> registry branch).
    /// </summary>
    private static IEnumerable<(string Address, DohMode Mode, string? Template, bool AllowFallback)> DohServers(NetworkProfile profile)
    {
        // A DNS server listed in the profile is always configured statically, in every IPv4 mode
        // (a DHCP address can still carry hand-picked DNS servers), so it can carry DoH settings.
        if (!string.IsNullOrWhiteSpace(profile.PrimaryDns))
        {
            yield return (
                profile.PrimaryDns.Trim(),
                profile.PrimaryDnsDoh,
                profile.PrimaryDnsDohTemplate?.Trim(),
                profile.PrimaryDnsDohAllowFallback);
        }

        if (!string.IsNullOrWhiteSpace(profile.SecondaryDns))
        {
            yield return (
                profile.SecondaryDns.Trim(),
                profile.SecondaryDnsDoh,
                profile.SecondaryDnsDohTemplate?.Trim(),
                profile.SecondaryDnsDohAllowFallback);
        }

        if (!string.IsNullOrWhiteSpace(profile.Ipv6PrimaryDns))
        {
            yield return (
                profile.Ipv6PrimaryDns.Trim(),
                profile.Ipv6PrimaryDnsDoh,
                profile.Ipv6PrimaryDnsDohTemplate?.Trim(),
                profile.Ipv6PrimaryDnsDohAllowFallback);
        }

        if (!string.IsNullOrWhiteSpace(profile.Ipv6SecondaryDns))
        {
            yield return (
                profile.Ipv6SecondaryDns.Trim(),
                profile.Ipv6SecondaryDnsDoh,
                profile.Ipv6SecondaryDnsDohTemplate?.Trim(),
                profile.Ipv6SecondaryDnsDohAllowFallback);
        }
    }

    /// <summary>
    /// Applies the IPv6 part of the profile. "Automatic" is the state the Windows Settings switch
    /// shows as IPv6 off: the address is obtained through DHCPv6 / router advertisements.
    /// </summary>
    private async Task<NetworkConfigResult> ApplyIpv6Async(string adapterName, NetworkProfile profile, List<string> logs, CancellationToken ct)
    {
        var hasDns = !string.IsNullOrWhiteSpace(profile.Ipv6PrimaryDns) ||
                     !string.IsNullOrWhiteSpace(profile.Ipv6SecondaryDns);

        if (profile.Ipv6Mode == Ipv6Mode.Unchanged && !hasDns)
        {
            logs.Add("> IPv6：不修改");
            return NetworkConfigResult.Ok(string.Empty, logs);
        }

        logs.Add("> IPv6");

        if (string.IsNullOrWhiteSpace(profile.Ipv6PrimaryDns) &&
            !string.IsNullOrWhiteSpace(profile.Ipv6SecondaryDns))
        {
            return NetworkConfigResult.Fail("设置备用 IPv6 DNS 需同时设置首选 IPv6 DNS。", logs);
        }

        if (profile.Ipv6Mode == Ipv6Mode.Manual)
        {
            if (!Ipv6Validator.IsValidAddress(profile.Ipv6Address))
            {
                return NetworkConfigResult.Fail("手动模式下 IPv6 地址无效。", logs);
            }
            if (!Ipv6Validator.TryParsePrefixLength(profile.Ipv6PrefixLength, out var prefixLength))
            {
                return NetworkConfigResult.Fail("手动模式下 IPv6 子网前缀长度无效（需为 0-128）。", logs);
            }
            if (!Ipv6Validator.IsValidOptionalScopedAddress(profile.Ipv6Gateway))
            {
                return NetworkConfigResult.Fail("IPv6 网关地址无效。", logs);
            }

            // Same rule the Windows Settings dialog enforces for IPv4: a default route needs a resolver.
            if (!string.IsNullOrWhiteSpace(profile.Ipv6Gateway) &&
                string.IsNullOrWhiteSpace(profile.Ipv6PrimaryDns))
            {
                return NetworkConfigResult.Fail("手动模式下配置了 IPv6 网关时必须填写首选 IPv6 DNS。", logs);
            }

            // Windows Settings replaces the static configuration instead of stacking addresses on top of it,
            // and a static address also means the address is no longer obtained through DHCPv6.
            var cleanup = await RunPowerShellAsync(
                ClearManualIpv6Script(adapterName) +
                $"; Set-NetIPInterface -InterfaceAlias '{EscapeSingleQuotes(adapterName)}' -AddressFamily IPv6 -Dhcp Disabled", ct);
            logs.AddRange(cleanup.Logs);
            if (!cleanup.Success)
            {
                logs.Add($"(警告：清除原有 IPv6 自动/手动配置失败：{cleanup.Error})");
            }

            var ra = await RunNetshAsync(
                $"interface ipv6 add address interface=\"{adapterName}\" address={profile.Ipv6Address!.Trim()}/{prefixLength} store=persistent", ct);
            logs.AddRange(ra.Logs);
            if (!ra.Success)
            {
                return NetworkConfigResult.Fail($"设置静态 IPv6 地址失败：{ra.Error}", logs);
            }

            if (!string.IsNullOrWhiteSpace(profile.Ipv6Gateway))
            {
                var rg = await RunNetshAsync(
                    $"interface ipv6 add route prefix=::/0 interface=\"{adapterName}\" nexthop={Ipv6Validator.StripScope(profile.Ipv6Gateway)} store=persistent", ct);
                logs.AddRange(rg.Logs);
                if (!rg.Success)
                {
                    logs.Add($"(警告：设置 IPv6 默认网关失败：{rg.Error})");
                }
            }
        }
        else if (profile.Ipv6Mode == Ipv6Mode.Automatic)
        {
            var script = ClearManualIpv6Script(adapterName) +
                         $"; Set-NetIPInterface -InterfaceAlias '{EscapeSingleQuotes(adapterName)}' -AddressFamily IPv6 -Dhcp Enabled";
            var ra = await RunPowerShellAsync(script, ct);
            logs.AddRange(ra.Logs);
            if (!ra.Success)
            {
                return NetworkConfigResult.Fail($"切换 IPv6 为自动（DHCPv6）失败：{ra.Error}", logs);
            }

            var ri = await RunNetshAsync(
                $"interface ipv6 set interface interface=\"{adapterName}\" routerdiscovery=enabled managedaddress=enabled otherstateful=enabled store=persistent", ct);
            logs.AddRange(ri.Logs);
            if (!ri.Success)
            {
                logs.Add($"(警告：启用 IPv6 路由器发现 / DHCPv6 失败：{ri.Error})");
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.Ipv6PrimaryDns))
        {
            var rd = await RunNetshAsync(
                $"interface ipv6 set dnsservers name=\"{adapterName}\" source=static address={profile.Ipv6PrimaryDns.Trim()} register=primary validate=no", ct);
            logs.AddRange(rd.Logs);
            if (!rd.Success)
            {
                return NetworkConfigResult.Fail($"设置首选 IPv6 DNS 失败：{rd.Error}", logs);
            }

            if (!string.IsNullOrWhiteSpace(profile.Ipv6SecondaryDns))
            {
                var rd2 = await RunNetshAsync(
                    $"interface ipv6 add dnsservers name=\"{adapterName}\" address={profile.Ipv6SecondaryDns.Trim()} index=2 validate=no", ct);
                logs.AddRange(rd2.Logs);
                if (!rd2.Success)
                {
                    logs.Add($"(警告：添加备用 IPv6 DNS 失败：{rd2.Error})");
                }
            }
        }
        else if (profile.Ipv6Mode != Ipv6Mode.Unchanged)
        {
            var rd0 = await RunNetshAsync(
                $"interface ipv6 set dnsservers name=\"{adapterName}\" source=dhcp", ct);
            logs.AddRange(rd0.Logs);
            if (!rd0.Success)
            {
                logs.Add($"(警告：设置 IPv6 DNS 为自动获取失败：{rd0.Error})");
            }
        }

        return NetworkConfigResult.Ok(string.Empty, logs);
    }

    /// <summary>Drops the persistent (i.e. manually configured) IPv6 addresses and default routes.</summary>
    private static string ClearManualIpv6Script(string adapterName)
    {
        var alias = EscapeSingleQuotes(adapterName);
        return "Get-NetIPAddress -InterfaceAlias '" + alias + "' -AddressFamily IPv6 -PolicyStore PersistentStore " +
               "-ErrorAction SilentlyContinue | Remove-NetIPAddress -Confirm:$false -ErrorAction SilentlyContinue; " +
               "Get-NetRoute -InterfaceAlias '" + alias + "' -AddressFamily IPv6 -DestinationPrefix '::/0' -PolicyStore PersistentStore " +
               "-ErrorAction SilentlyContinue | Remove-NetRoute -Confirm:$false -ErrorAction SilentlyContinue";
    }

    private static string EscapeSingleQuotes(string value) => value.Replace("'", "''");

    private async Task<NetworkConfigResult> ApplyStaticAsync(string adapterName, NetworkProfile profile, List<string> logs, CancellationToken ct)
    {

        if (string.IsNullOrWhiteSpace(profile.IpAddress))
        {
            return NetworkConfigResult.Fail("静态模式下 IP 地址不能为空。", logs);
        }
        if (string.IsNullOrWhiteSpace(profile.SubnetMask))
        {
            return NetworkConfigResult.Fail("静态模式下子网掩码不能为空。", logs);
        }

        // Windows Settings refuses to save a manual configuration that has a gateway but no DNS server
        // ("配置网关需要 DNS"), so a profile that asks for a default route must name a resolver too.
        if (!string.IsNullOrWhiteSpace(profile.Gateway) && string.IsNullOrWhiteSpace(profile.PrimaryDns))
        {
            return NetworkConfigResult.Fail("手动模式下配置了网关时必须填写首选 DNS。", logs);
        }

        string addrArgs;
        if (string.IsNullOrWhiteSpace(profile.Gateway))
        {
            addrArgs = $"interface ip set address name=\"{adapterName}\" source=static address={profile.IpAddress} mask={profile.SubnetMask}";
        }
        else
        {
            addrArgs = $"interface ip set address name=\"{adapterName}\" source=static address={profile.IpAddress} mask={profile.SubnetMask} gateway={profile.Gateway} gwmetric=0";
        }

        var ra = await RunNetshAsync(addrArgs, ct);
        logs.AddRange(ra.Logs);
        if (!ra.Success)
        {
            return NetworkConfigResult.Fail($"设置静态地址失败：{ra.Error}", logs);
        }

        // With no DNS server named, Windows leaves the DNS source on "obtain automatically".
        return await ApplyIpv4DnsAsync(adapterName, profile, logs, ct, autoWhenEmpty: true);
    }

    private static async Task<NetshRun> RunNetshAsync(string args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "netsh.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.Unicode,
            StandardErrorEncoding = System.Text.Encoding.Unicode,
        };

        try
        {
            using var p = Process.Start(psi);
            if (p is null)
            {
                return new NetshRun(false, "无法启动 netsh 进程。", new[] { $"> netsh {args}", "无法启动进程" });
            }

            var outTask = p.StandardOutput.ReadToEndAsync(ct);
            var errTask = p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            var stdout = await outTask;
            var stderr = await errTask;

            var logs = new List<string> { $"> netsh {args}" };
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    logs.Add("  " + line.Trim());
                }
            }
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    logs.Add("  [err] " + line.Trim());
                }
            }

            if (p.ExitCode != 0)
            {
                return new NetshRun(false, $"netsh 退出码 {p.ExitCode}", logs);
            }

            return new NetshRun(true, string.Empty, logs);
        }
        catch (Exception ex)
        {
            return new NetshRun(false, ex.Message, new[] { $"> netsh {args}", ex.Message });
        }
    }

    private sealed record NetshRun(bool Success, string Error, IReadOnlyList<string> Logs);

    private static async Task<NetshRun> SetNetworkCategoryAsync(string adapterName, NetworkCategory category, CancellationToken ct)
    {
        var categoryStr = category == NetworkCategory.Public ? "Public" : "Private";
        var script = $"Set-NetConnectionProfile -InterfaceAlias '{adapterName}' -NetworkCategory {categoryStr}";

        return await RunPowerShellAsync(script, ct);
    }

    private static async Task<NetshRun> RunPowerShellAsync(string script, CancellationToken ct)
    {
        var script2 = $"[Console]::OutputEncoding=[Text.Encoding]::UTF8; {script}";
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script2}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };

        try
        {
            using var p = Process.Start(psi);
            if (p is null)
            {
                return new NetshRun(false, "无法启动 PowerShell 进程。", new[] { $"> ps {script}", "无法启动进程" });
            }

            var outTask = p.StandardOutput.ReadToEndAsync(ct);
            var errTask = p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);

            var stdout = await outTask;
            var stderr = await errTask;

            var logs = new List<string> { $"> ps {script}" };
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    logs.Add("  " + line.Trim());
                }
            }
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    logs.Add("  [err] " + line.Trim());
                }
            }

            if (p.ExitCode != 0)
            {
                return new NetshRun(false, $"PowerShell 退出码 {p.ExitCode}", logs);
            }

            return new NetshRun(true, string.Empty, logs);
        }
        catch (Exception ex)
        {
            return new NetshRun(false, ex.Message, new[] { $"> ps {script}", ex.Message });
        }
    }
}
