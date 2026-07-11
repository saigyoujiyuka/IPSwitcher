using CommunityToolkit.Mvvm.ComponentModel;
using IPSwitcher.Models;

namespace IPSwitcher.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    public NetworkProfile Source { get; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private bool _useDhcp;

    [ObservableProperty]
    private string? _ipAddress;

    [ObservableProperty]
    private string? _subnetMask;

    [ObservableProperty]
    private string? _gateway;

    [ObservableProperty]
    private string? _primaryDns;

    [ObservableProperty]
    private string? _secondaryDns;

    [ObservableProperty]
    private NetworkCategory? _networkCategory;

    public Guid Id => Source.Id;

    public string Summary
    {
        get
        {
            var parts = new List<string>();

            if (UseDhcp)
            {
                parts.Add("DHCP");
            }
            else
            {
                parts.Add(IpAddress ?? "—");
                if (!string.IsNullOrWhiteSpace(Gateway))
                {
                    parts.Add($"gw {Gateway}");
                }
                if (!string.IsNullOrWhiteSpace(PrimaryDns))
                {
                    parts.Add($"dns {PrimaryDns}");
                }
            }

            if (NetworkCategory.HasValue)
            {
                parts.Add(NetworkCategory.Value == Models.NetworkCategory.Private ? "专用" : "公用");
            }

            return string.Join(" | ", parts);
        }
    }

    public ProfileViewModel(NetworkProfile profile)
    {
        Source = profile;
        _name = profile.Name;
        _useDhcp = profile.UseDhcp;
        _ipAddress = profile.IpAddress;
        _subnetMask = profile.SubnetMask;
        _gateway = profile.Gateway;
        _primaryDns = profile.PrimaryDns;
        _secondaryDns = profile.SecondaryDns;
        _networkCategory = profile.NetworkCategory;
    }

    public void SyncFromSource()
    {
        Name = Source.Name;
        UseDhcp = Source.UseDhcp;
        IpAddress = Source.IpAddress;
        SubnetMask = Source.SubnetMask;
        Gateway = Source.Gateway;
        PrimaryDns = Source.PrimaryDns;
        SecondaryDns = Source.SecondaryDns;
        NetworkCategory = Source.NetworkCategory;
    }

    public void WriteBackToSource()
    {
        Source.Name = Name;
        Source.UseDhcp = UseDhcp;
        Source.IpAddress = IpAddress;
        Source.SubnetMask = SubnetMask;
        Source.Gateway = Gateway;
        Source.PrimaryDns = PrimaryDns;
        Source.SecondaryDns = SecondaryDns;
        Source.NetworkCategory = NetworkCategory;
    }

    partial void OnNameChanged(string value)
    {
        Source.Name = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnUseDhcpChanged(bool value)
    {
        Source.UseDhcp = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnIpAddressChanged(string? value)
    {
        Source.IpAddress = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnSubnetMaskChanged(string? value)
    {
        Source.SubnetMask = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnGatewayChanged(string? value)
    {
        Source.Gateway = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnPrimaryDnsChanged(string? value)
    {
        Source.PrimaryDns = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnSecondaryDnsChanged(string? value)
    {
        Source.SecondaryDns = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnNetworkCategoryChanged(NetworkCategory? value)
    {
        Source.NetworkCategory = value;
        OnPropertyChanged(nameof(Summary));
    }
}
