using System.Diagnostics;
using System.Net;
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

    public string PrimaryDnsDoh { get; init; } = "—";

    public string SecondaryDnsDoh { get; init; } = "—";

    public string Ipv6Address { get; init; } = "—";

    public string Ipv6Gateway { get; init; } = "—";

    public string Ipv6AddressSource { get; init; } = "—";

    public string Ipv6PrimaryDns { get; init; } = "—";

    public string Ipv6SecondaryDns { get; init; } = "—";

    public string Ipv6PrimaryDnsDoh { get; init; } = "—";

    public string Ipv6SecondaryDnsDoh { get; init; } = "—";

    public bool IsDhcp { get; init; }

    public string NetworkCategory { get; init; } = "—";

    public string SourceText => IsDhcp ? "DHCP（自动）" : "静态";

    public static CurrentConfig Empty => new();
}

public sealed class CurrentConfigReader
{
    private readonly DohSettingsService _dohSettings;

    public CurrentConfigReader(DohSettingsService dohSettings)
    {
        _dohSettings = dohSettings;
    }

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

        string doh1 = "—";
        if (dnsList.Count > 0)
        {
            var state = _dohSettings.Read(nic.Id, dnsList[0]);
            doh1 = DohModeInfo.StateText(state.Mode, state.AllowFallback);
        }

        string doh2 = "—";
        if (dnsList.Count > 1)
        {
            var state = _dohSettings.Read(nic.Id, dnsList[1]);
            doh2 = DohModeInfo.StateText(state.Mode, state.AllowFallback);
        }

        // IPv6
        string ipv6 = "—";
        var ipv6Addresses = props.UnicastAddresses
            .Where(a => a.Address.AddressFamily == AddressFamily.InterNetworkV6)
            .ToList();

        var preferredV6 = ipv6Addresses.FirstOrDefault(a => IsReportableIpv6(a.Address)) ?? ipv6Addresses.FirstOrDefault();
        if (preferredV6 is not null)
        {
            ipv6 = $"{preferredV6.Address}/{preferredV6.PrefixLength}";
        }

        string gw6 = "—";
        foreach (var g in props.GatewayAddresses)
        {
            if (g.Address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                gw6 = g.Address.ToString();
                break;
            }
        }

        var dns6List = props.DnsAddresses
            .Where(d => d.AddressFamily == AddressFamily.InterNetworkV6)
            .Select(d => d.ToString())
            .ToList();

        string dns61 = dns6List.Count > 0 ? dns6List[0] : "—";
        string dns62 = dns6List.Count > 1 ? dns6List[1] : "—";

        string doh61 = "—";
        if (dns6List.Count > 0)
        {
            var state = _dohSettings.Read(nic.Id, dns6List[0]);
            doh61 = DohModeInfo.StateText(state.Mode, state.AllowFallback);
        }

        string doh62 = "—";
        if (dns6List.Count > 1)
        {
            var state = _dohSettings.Read(nic.Id, dns6List[1]);
            doh62 = DohModeInfo.StateText(state.Mode, state.AllowFallback);
        }

        var ipv6Source = ReadIpv6AddressSource(nic.Id);

        return new CurrentConfig
        {
            IpAddress = ip,
            SubnetMask = mask,
            Gateway = gw,
            PrimaryDns = dns1,
            SecondaryDns = dns2,
            PrimaryDnsDoh = doh1,
            SecondaryDnsDoh = doh2,
            Ipv6Address = ipv6,
            Ipv6Gateway = gw6,
            Ipv6AddressSource = ipv6Source,
            Ipv6PrimaryDns = dns61,
            Ipv6SecondaryDns = dns62,
            Ipv6PrimaryDnsDoh = doh61,
            Ipv6SecondaryDnsDoh = doh62,
            IsDhcp = isDhcp,
            NetworkCategory = category,
        };
    }

    /// <summary>Global unicast addresses are worth showing; link-local ones are a fallback.</summary>
    private static bool IsReportableIpv6(IPAddress address) =>
        !address.IsIPv6LinkLocal &&
        !address.IsIPv6Multicast &&
        !address.IsIPv6Teredo &&
        !address.IsIPv6SiteLocal;

    /// <summary>
    /// Reads <c>Tcpip6\Parameters\Interfaces\{guid}\EnableDHCP</c>: 1 means the address is obtained
    /// automatically (DHCPv6 / router advertisements), 0 means it is statically configured.
    /// </summary>
    private static string ReadIpv6AddressSource(string interfaceId)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\{interfaceId}");

            var value = key?.GetValue("EnableDHCP");
            return value switch
            {
                int i => i != 0 ? "自动（DHCPv6）" : "静态",
                long l => l != 0 ? "自动（DHCPv6）" : "静态",
                _ => "—",
            };
        }
        catch
        {
            return "—";
        }
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
