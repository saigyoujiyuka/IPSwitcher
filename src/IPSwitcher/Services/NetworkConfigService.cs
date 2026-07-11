using System.Diagnostics;
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
    public async Task<NetworkConfigResult> ApplyAsync(string adapterName, NetworkProfile profile, CancellationToken ct = default)
    {
        var logs = new List<string>();

        if (string.IsNullOrWhiteSpace(adapterName))
        {
            return NetworkConfigResult.Fail("未选择网络适配器。", logs);
        }

        if (profile.UseDhcp)
        {
            var r1 = await RunNetshAsync(
                $"interface ip set address name=\"{adapterName}\" source=dhcp", ct);
            logs.AddRange(r1.Logs);
            if (!r1.Success)
            {
                return NetworkConfigResult.Fail($"设置 DHCP 地址失败：{r1.Error}", logs);
            }

            var r2 = await RunNetshAsync(
                $"interface ip set dns name=\"{adapterName}\" source=dhcp", ct);
            logs.AddRange(r2.Logs);
            if (!r2.Success)
            {
                return NetworkConfigResult.Fail($"设置 DHCP DNS 失败：{r2.Error}", logs);
            }
        }
        else
        {
            var staticResult = await ApplyStaticAsync(adapterName, profile, logs, ct);
            if (!staticResult.Success)
            {
                return staticResult;
            }
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

        var mode = profile.UseDhcp ? "DHCP 自动获取" : $"静态配置「{profile.Name}」";
        return NetworkConfigResult.Ok($"已将「{adapterName}」切换为 {mode}。", logs);
    }

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

        var rc = await RunNetshAsync(
            $"interface ip delete dns name=\"{adapterName}\" all", ct);
        logs.AddRange(rc.Logs);

        if (string.IsNullOrWhiteSpace(profile.PrimaryDns))
        {
            logs.Add("(未配置 DNS，已清空原有 DNS)");
        }
        else
        {
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
        }

        return NetworkConfigResult.Ok(string.Empty, logs);
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
