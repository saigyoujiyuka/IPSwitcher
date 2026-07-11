using System.Net.NetworkInformation;
using IPSwitcher.Models;

namespace IPSwitcher.Services;

public sealed class AdapterService
{
    public IReadOnlyList<AdapterInfo> GetAdapters()
    {
        var result = new List<AdapterInfo>();
        var defaultName = GetDefaultAdapterName();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                nic.NetworkInterfaceType == NetworkInterfaceType.Unknown)
            {
                continue;
            }

            if (!nic.Supports(NetworkInterfaceComponent.IPv4))
            {
                continue;
            }

            var info = new AdapterInfo
            {
                Name = nic.Name,
                Description = nic.Description,
                InterfaceType = nic.NetworkInterfaceType,
                Status = nic.OperationalStatus,
                IsDefault = string.Equals(nic.Name, defaultName, StringComparison.OrdinalIgnoreCase),
            };
            result.Add(info);
        }

        return result;
    }

    public string? GetDefaultAdapterName()
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }
            if (!nic.Supports(NetworkInterfaceComponent.IPv4))
            {
                continue;
            }

            var props = nic.GetIPProperties();
            foreach (var gw in props.GatewayAddresses)
            {
                if (gw.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return nic.Name;
                }
            }
        }

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus == OperationalStatus.Up &&
                nic.Supports(NetworkInterfaceComponent.IPv4) &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            {
                return nic.Name;
            }
        }

        return null;
    }
}
