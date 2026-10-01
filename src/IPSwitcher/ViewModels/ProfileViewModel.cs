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
    private Ipv4Mode _ipv4Mode;

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

    [ObservableProperty]
    private Ipv6Mode _ipv6Mode;

    [ObservableProperty]
    private string? _ipv6Address;

    [ObservableProperty]
    private string? _ipv6PrefixLength;

    [ObservableProperty]
    private string? _ipv6Gateway;

    [ObservableProperty]
    private string? _ipv6PrimaryDns;

    [ObservableProperty]
    private DohMode _ipv6PrimaryDnsDoh;

    [ObservableProperty]
    private string? _ipv6PrimaryDnsDohTemplate;

    [ObservableProperty]
    private bool _ipv6PrimaryDnsDohAllowFallback;

    [ObservableProperty]
    private string? _ipv6SecondaryDns;

    [ObservableProperty]
    private DohMode _ipv6SecondaryDnsDoh;

    [ObservableProperty]
    private string? _ipv6SecondaryDnsDohTemplate;

    [ObservableProperty]
    private bool _ipv6SecondaryDnsDohAllowFallback;

    public Guid Id => Source.Id;

    /// <summary>Address, prefix length and gateway only apply to a static IPv6 configuration.</summary>
    public bool IsIpv6ManualFieldsEnabled => Ipv6Mode == Ipv6Mode.Manual;

    public bool IsIpv6PrimaryDohEnabled =>
        !string.IsNullOrWhiteSpace(Ipv6PrimaryDns) && DohSettingsService.IsSupported;

    public bool IsIpv6PrimaryDohExpanded => IsIpv6PrimaryDohEnabled && Ipv6PrimaryDnsDoh != DohMode.Off;

    public bool IsIpv6PrimaryDohTemplateEnabled => IsIpv6PrimaryDohExpanded && Ipv6PrimaryDnsDoh == DohMode.Manual;

    public bool IsIpv6SecondaryDohEnabled =>
        !string.IsNullOrWhiteSpace(Ipv6SecondaryDns) && DohSettingsService.IsSupported;

    public bool IsIpv6SecondaryDohExpanded => IsIpv6SecondaryDohEnabled && Ipv6SecondaryDnsDoh != DohMode.Off;

    public bool IsIpv6SecondaryDohTemplateEnabled => IsIpv6SecondaryDohExpanded && Ipv6SecondaryDnsDoh == DohMode.Manual;

    /// <summary>Address, subnet mask and gateway only apply to a static IPv4 configuration.</summary>
    public bool IsIpv4ManualFieldsEnabled => Ipv4Mode == Ipv4Mode.Manual;

    /// <summary>The DoH dropdown is usable once a DNS server address is present.</summary>
    public bool IsPrimaryDohEnabled =>
        !string.IsNullOrWhiteSpace(PrimaryDns) && DohSettingsService.IsSupported;

    /// <summary>Template box and fallback switch follow the dropdown, as in Windows Settings.</summary>
    public bool IsPrimaryDohExpanded => IsPrimaryDohEnabled && PrimaryDnsDoh != DohMode.Off;

    public bool IsPrimaryDohTemplateEnabled => IsPrimaryDohExpanded && PrimaryDnsDoh == DohMode.Manual;

    public bool IsSecondaryDohEnabled =>
        !string.IsNullOrWhiteSpace(SecondaryDns) && DohSettingsService.IsSupported;

    public bool IsSecondaryDohExpanded => IsSecondaryDohEnabled && SecondaryDnsDoh != DohMode.Off;

    public bool IsSecondaryDohTemplateEnabled => IsSecondaryDohExpanded && SecondaryDnsDoh == DohMode.Manual;

    public string Summary
    {
        get
        {
            var parts = new List<string>();

            if (Ipv4Mode == Ipv4Mode.Manual)
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
            else
            {
                parts.Add(Ipv4ModeInfo.SummaryTag(Ipv4Mode));

                if (Ipv4Mode == Ipv4Mode.Unchanged && !string.IsNullOrWhiteSpace(PrimaryDns))
                {
                    parts.Add($"dns {PrimaryDns}");
                }
            }

            if (NetworkCategory.HasValue)
            {
                parts.Add(NetworkCategory.Value == Models.NetworkCategory.Private ? "专用" : "公用");
            }

            var ipv6 = Ipv6ModeInfo.SummaryTag(Ipv6Mode);
            if (ipv6.Length > 0)
            {
                parts.Add(ipv6);
            }

            return string.Join(" | ", parts);
        }
    }

    public ProfileViewModel(NetworkProfile profile)
    {
        Source = profile;
        _name = profile.Name;
        _ipv4Mode = profile.Ipv4Mode;
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
        _ipv6Mode = profile.Ipv6Mode;
        _ipv6Address = profile.Ipv6Address;
        _ipv6PrefixLength = profile.Ipv6PrefixLength;
        _ipv6Gateway = profile.Ipv6Gateway;
        _ipv6PrimaryDns = profile.Ipv6PrimaryDns;
        _ipv6PrimaryDnsDoh = profile.Ipv6PrimaryDnsDoh;
        _ipv6PrimaryDnsDohTemplate = profile.Ipv6PrimaryDnsDohTemplate;
        _ipv6PrimaryDnsDohAllowFallback = profile.Ipv6PrimaryDnsDohAllowFallback;
        _ipv6SecondaryDns = profile.Ipv6SecondaryDns;
        _ipv6SecondaryDnsDoh = profile.Ipv6SecondaryDnsDoh;
        _ipv6SecondaryDnsDohTemplate = profile.Ipv6SecondaryDnsDohTemplate;
        _ipv6SecondaryDnsDohAllowFallback = profile.Ipv6SecondaryDnsDohAllowFallback;
    }

    public void SyncFromSource()
    {
        Name = Source.Name;
        Ipv4Mode = Source.Ipv4Mode;
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
        Ipv6Mode = Source.Ipv6Mode;
        Ipv6Address = Source.Ipv6Address;
        Ipv6PrefixLength = Source.Ipv6PrefixLength;
        Ipv6Gateway = Source.Ipv6Gateway;
        Ipv6PrimaryDns = Source.Ipv6PrimaryDns;
        Ipv6PrimaryDnsDoh = Source.Ipv6PrimaryDnsDoh;
        Ipv6PrimaryDnsDohTemplate = Source.Ipv6PrimaryDnsDohTemplate;
        Ipv6PrimaryDnsDohAllowFallback = Source.Ipv6PrimaryDnsDohAllowFallback;
        Ipv6SecondaryDns = Source.Ipv6SecondaryDns;
        Ipv6SecondaryDnsDoh = Source.Ipv6SecondaryDnsDoh;
        Ipv6SecondaryDnsDohTemplate = Source.Ipv6SecondaryDnsDohTemplate;
        Ipv6SecondaryDnsDohAllowFallback = Source.Ipv6SecondaryDnsDohAllowFallback;
    }

    public void WriteBackToSource()
    {
        Source.Name = Name;
        Source.Ipv4Mode = Ipv4Mode;
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
        Source.Ipv6Mode = Ipv6Mode;
        Source.Ipv6Address = Ipv6Address;
        Source.Ipv6PrefixLength = Ipv6PrefixLength;
        Source.Ipv6Gateway = Ipv6Gateway;
        Source.Ipv6PrimaryDns = Ipv6PrimaryDns;
        Source.Ipv6PrimaryDnsDoh = Ipv6PrimaryDnsDoh;
        Source.Ipv6PrimaryDnsDohTemplate = Ipv6PrimaryDnsDohTemplate;
        Source.Ipv6PrimaryDnsDohAllowFallback = Ipv6PrimaryDnsDohAllowFallback;
        Source.Ipv6SecondaryDns = Ipv6SecondaryDns;
        Source.Ipv6SecondaryDnsDoh = Ipv6SecondaryDnsDoh;
        Source.Ipv6SecondaryDnsDohTemplate = Ipv6SecondaryDnsDohTemplate;
        Source.Ipv6SecondaryDnsDohAllowFallback = Ipv6SecondaryDnsDohAllowFallback;
    }

    private void RaiseSummaryAndDohState()
    {
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(IsIpv4ManualFieldsEnabled));
        OnPropertyChanged(nameof(IsPrimaryDohEnabled));
        OnPropertyChanged(nameof(IsPrimaryDohExpanded));
        OnPropertyChanged(nameof(IsPrimaryDohTemplateEnabled));
        OnPropertyChanged(nameof(IsSecondaryDohEnabled));
        OnPropertyChanged(nameof(IsSecondaryDohExpanded));
        OnPropertyChanged(nameof(IsSecondaryDohTemplateEnabled));
        OnPropertyChanged(nameof(IsIpv6ManualFieldsEnabled));
        OnPropertyChanged(nameof(IsIpv6PrimaryDohEnabled));
        OnPropertyChanged(nameof(IsIpv6PrimaryDohExpanded));
        OnPropertyChanged(nameof(IsIpv6PrimaryDohTemplateEnabled));
        OnPropertyChanged(nameof(IsIpv6SecondaryDohEnabled));
        OnPropertyChanged(nameof(IsIpv6SecondaryDohExpanded));
        OnPropertyChanged(nameof(IsIpv6SecondaryDohTemplateEnabled));
    }

    partial void OnNameChanged(string value)
    {
        Source.Name = value;
        OnPropertyChanged(nameof(Summary));
    }

    partial void OnIpv4ModeChanged(Ipv4Mode value)
    {
        Source.Ipv4Mode = value;
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

    partial void OnIpv6ModeChanged(Ipv6Mode value)
    {
        Source.Ipv6Mode = value;
        RaiseSummaryAndDohState();
    }

    partial void OnIpv6AddressChanged(string? value)
    {
        Source.Ipv6Address = value;
    }

    partial void OnIpv6PrefixLengthChanged(string? value)
    {
        Source.Ipv6PrefixLength = value;
    }

    partial void OnIpv6GatewayChanged(string? value)
    {
        Source.Ipv6Gateway = value;
    }

    partial void OnIpv6PrimaryDnsChanged(string? value)
    {
        Source.Ipv6PrimaryDns = value;
        RaiseSummaryAndDohState();
    }

    partial void OnIpv6PrimaryDnsDohChanged(DohMode value)
    {
        Source.Ipv6PrimaryDnsDoh = value;
        RaiseSummaryAndDohState();
    }

    partial void OnIpv6PrimaryDnsDohTemplateChanged(string? value)
    {
        Source.Ipv6PrimaryDnsDohTemplate = value;
    }

    partial void OnIpv6PrimaryDnsDohAllowFallbackChanged(bool value)
    {
        Source.Ipv6PrimaryDnsDohAllowFallback = value;
    }

    partial void OnIpv6SecondaryDnsChanged(string? value)
    {
        Source.Ipv6SecondaryDns = value;
        RaiseSummaryAndDohState();
    }

    partial void OnIpv6SecondaryDnsDohChanged(DohMode value)
    {
        Source.Ipv6SecondaryDnsDoh = value;
        RaiseSummaryAndDohState();
    }

    partial void OnIpv6SecondaryDnsDohTemplateChanged(string? value)
    {
        Source.Ipv6SecondaryDnsDohTemplate = value;
    }

    partial void OnIpv6SecondaryDnsDohAllowFallbackChanged(bool value)
    {
        Source.Ipv6SecondaryDnsDohAllowFallback = value;
    }
}
