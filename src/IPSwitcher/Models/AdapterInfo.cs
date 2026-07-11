using System.Net.NetworkInformation;

namespace IPSwitcher.Models;

public sealed class AdapterInfo
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public NetworkInterfaceType InterfaceType { get; init; }

    public OperationalStatus Status { get; init; }

    public bool IsUp => Status == OperationalStatus.Up;

    public bool IsDefault { get; init; }

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Description) ? Name : $"{Name} ({Description})";

    public string StatusText => IsUp ? "已连接" : "已断开";
}
