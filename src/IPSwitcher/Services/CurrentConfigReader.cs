using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using IPSwitcher.Models;

namespace IPSwitcher.Services;

public sealed class CurrentConfig
{
    public string IpAddress { get; init; } = "—";

    public string SubnetMask { get; init; } = "—";

    public string Gateway { get; init; } = "—";

    public string PrimaryDns { get; init; } = "—";

    public string SecondaryDns { get; init; } = "—";

    public bool IsDhcp { get; init; }

    public string NetworkCategory { get; init; } = "—";

    public string SourceText => IsDhcp ? "DHCP（自动）" : "静态";

    public static CurrentConfig Empty => new();
}

public sealed class CurrentConfigReader
{
    public CurrentConfig Read(string adapterName)
    {
        if (string.IsNullOrWhiteSpace(adapterName))
        {
            return CurrentConfig.Empty;
        }

        var nic = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => string.Equals(n.Name, adapterName, StringComparison.OrdinalIgnoreCase));

        if (nic is null)
        {
            return CurrentConfig.Empty;
        }

        var props = nic.GetIPProperties();
        var ipv4 = props.GetIPv4Properties();

        string ip = "—";
        string mask = "—";

        foreach (var addr in props.UnicastAddresses)
        {
            if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                ip = addr.Address.ToString();
                var prefix = addr.PrefixLength;
                mask = PrefixToMask(prefix);
                break;
            }
        }

        string gw = "—";
        foreach (var g in props.GatewayAddresses)
        {
            if (g.Address.AddressFamily == AddressFamily.InterNetwork)
            {
                gw = g.Address.ToString();
                break;
            }
        }

        var dnsList = props.DnsAddresses
            .Where(d => d.AddressFamily == AddressFamily.InterNetwork)
            .Select(d => d.ToString())
            .ToList();

        string dns1 = dnsList.Count > 0 ? dnsList[0] : "—";
        string dns2 = dnsList.Count > 1 ? dnsList[1] : "—";

        bool isDhcp = ipv4?.IsDhcpEnabled ?? false;

        var category = ReadNetworkCategory(adapterName);

        return new CurrentConfig
        {
            IpAddress = ip,
            SubnetMask = mask,
            Gateway = gw,
            PrimaryDns = dns1,
            SecondaryDns = dns2,
            IsDhcp = isDhcp,
            NetworkCategory = category,
        };
    }

    private static string ReadNetworkCategory(string adapterName)
    {
        try
        {
            var script = $"[Console]::OutputEncoding=[Text.Encoding]::UTF8; (Get-NetConnectionProfile -InterfaceAlias '{adapterName}').NetworkCategory";
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
            };

            using var p = Process.Start(psi);
            if (p is null)
            {
                return "—";
            }

            if (!p.WaitForExit(3000))
            {
                try { p.Kill(); } catch { }
                return "—";
            }

            var stdout = p.StandardOutput.ReadToEnd().Trim();
            if (string.IsNullOrWhiteSpace(stdout))
            {
                return "—";
            }

            return stdout switch
            {
                "Public" => "公用",
                "Private" => "专用",
                "DomainAuthenticated" => "域",
                _ => stdout,
            };
        }
        catch
        {
            return "—";
        }
    }

    private static string PrefixToMask(int prefixLength)
    {
        if (prefixLength == 0)
        {
            return "0.0.0.0";
        }
        if (prefixLength > 32)
        {
            return "—";
        }
        uint mask = 0xFFFFFFFFu << (32 - prefixLength);
        return $"{(mask >> 24) & 0xFF}.{(mask >> 16) & 0xFF}.{(mask >> 8) & 0xFF}.{mask & 0xFF}";
    }
}
