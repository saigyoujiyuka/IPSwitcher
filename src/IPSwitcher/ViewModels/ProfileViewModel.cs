using CommunityToolkit.Mvvm.ComponentModel;
using IPSwitcher.Models;
using IPSwitcher.Services;

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
    private DohMode _primaryDnsDoh;

    [ObservableProperty]
    private string? _primaryDnsDohTemplate;

    [ObservableProperty]
    private bool _primaryDnsDohAllowFallback;

    [ObservableProperty]
    private string? _secondaryDns;

    [ObservableProperty]
    private DohMode _secondaryDnsDoh;

    [ObservableProperty]
    private string? _secondaryDnsDohTemplate;

    [ObservableProperty]
    private bool _secondaryDnsDohAllowFallback;

    [ObservableProperty]
    private NetworkCategory? _networkCategory;

    public Guid Id => Source.Id;

    /// <summary>The DoH dropdown is usable once a static DNS server address is present.</summary>
    public bool IsPrimaryDohEnabled =>
        !UseDhcp && !string.IsNullOrWhiteSpace(PrimaryDns) && DohSettingsService.IsSupported;

    /// <summary>Template box and fallback switch follow the dropdown, as in Windows Settings.</summary>
    public bool IsPrimaryDohExpanded => IsPrimaryDohEnabled && PrimaryDnsDoh != DohMode.Off;

    public bool IsPrimaryDohTemplateEnabled => IsPrimaryDohExpanded && PrimaryDnsDoh == DohMode.Manual;

    public bool IsSecondaryDohEnabled =>
        !UseDhcp && !string.IsNullOrWhiteSpace(SecondaryDns) && DohSettingsService.IsSupported;

    public bool IsSecondaryDohExpanded => IsSecondaryDohEnabled && SecondaryDnsDoh != DohMode.Off;

    public bool IsSecondaryDohTemplateEnabled => IsSecondaryDohExpanded && SecondaryDnsDoh == DohMode.Manual;

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

                var doh = DohModeInfo.SummaryTag(PrimaryDnsDoh);
                if (doh.Length > 0)
                {
                    parts.Add(doh);
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
        _primaryDnsDoh = profile.PrimaryDnsDoh;
        _primaryDnsDohTemplate = profile.PrimaryDnsDohTemplate;
        _primaryDnsDohAllowFallback = profile.PrimaryDnsDohAllowFallback;
        _secondaryDns = profile.SecondaryDns;
        _secondaryDnsDoh = profile.SecondaryDnsDoh;
        _secondaryDnsDohTemplate = profile.SecondaryDnsDohTemplate;
        _secondaryDnsDohAllowFallback = profile.SecondaryDnsDohAllowFallback;
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
        PrimaryDnsDoh = Source.PrimaryDnsDoh;
        PrimaryDnsDohTemplate = Source.PrimaryDnsDohTemplate;
        PrimaryDnsDohAllowFallback = Source.PrimaryDnsDohAllowFallback;
        SecondaryDns = Source.SecondaryDns;
        SecondaryDnsDoh = Source.SecondaryDnsDoh;
        SecondaryDnsDohTemplate = Source.SecondaryDnsDohTemplate;
        SecondaryDnsDohAllowFallback = Source.SecondaryDnsDohAllowFallback;
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
        Source.PrimaryDnsDoh = PrimaryDnsDoh;
        Source.PrimaryDnsDohTemplate = PrimaryDnsDohTemplate;
        Source.PrimaryDnsDohAllowFallback = PrimaryDnsDohAllowFallback;
        Source.SecondaryDns = SecondaryDns;
        Source.SecondaryDnsDoh = SecondaryDnsDoh;
        Source.SecondaryDnsDohTemplate = SecondaryDnsDohTemplate;
        Source.SecondaryDnsDohAllowFallback = SecondaryDnsDohAllowFallback;
        Source.NetworkCategory = NetworkCategory;
    }

    private void RaiseSummaryAndDohState()
    {
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(IsPrimaryDohEnabled));
        OnPropertyChanged(nameof(IsPrimaryDohExpanded));
        OnPropertyChanged(nameof(IsPrimaryDohTemplateEnabled));
        OnPropertyChanged(nameof(IsSecondaryDohEnabled));
        OnPropertyChanged(nameof(IsSecondaryDohExpanded));
        OnPropertyChanged(nameof(IsSecondaryDohTemplateEnabled));
    }

    partial void OnNameChanged(string value)
    {
        Source.Name = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnUseDhcpChanged(bool value)
    {
        Source.UseDhcp = value;
        RaiseSummaryAndDohState();
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
        RaiseSummaryAndDohState();
    }

    partial void OnPrimaryDnsDohChanged(DohMode value)
    {
        Source.PrimaryDnsDoh = value;
        RaiseSummaryAndDohState();
    }

    partial void OnPrimaryDnsDohTemplateChanged(string? value)
    {
        Source.PrimaryDnsDohTemplate = value;
    }

    partial void OnPrimaryDnsDohAllowFallbackChanged(bool value)
    {
        Source.PrimaryDnsDohAllowFallback = value;
    }

    partial void OnSecondaryDnsChanged(string? value)
    {
        Source.SecondaryDns = value;
        RaiseSummaryAndDohState();
    }

    partial void OnSecondaryDnsDohChanged(DohMode value)
    {
        Source.SecondaryDnsDoh = value;
        RaiseSummaryAndDohState();
    }

    partial void OnSecondaryDnsDohTemplateChanged(string? value)
    {
        Source.SecondaryDnsDohTemplate = value;
    }

    partial void OnSecondaryDnsDohAllowFallbackChanged(bool value)
    {
        Source.SecondaryDnsDohAllowFallback = value;
    }

    partial void OnNetworkCategoryChanged(NetworkCategory? value)
    {
        Source.NetworkCategory = value;
        OnPropertyChanged(nameof(Summary));
    }
}
