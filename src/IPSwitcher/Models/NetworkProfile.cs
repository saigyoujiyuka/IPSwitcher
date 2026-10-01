using System.Text.Json.Serialization;

namespace IPSwitcher.Models;

public sealed class NetworkProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Kept in sync with <see cref="Ipv4Mode"/> so that profiles written by older versions stay
    /// readable in both directions.
    /// </summary>
    public bool UseDhcp { get; set; } = true;

    private Ipv4Mode? _ipv4Mode;

    /// <summary>
    /// IPv4 address mode. Profiles saved before this property existed only carry <see cref="UseDhcp"/>,
    /// so a missing value is derived from it instead of silently becoming "leave unchanged".
    /// </summary>
    public Ipv4Mode Ipv4Mode
    {
        get => _ipv4Mode ?? (UseDhcp ? Ipv4Mode.Automatic : Ipv4Mode.Manual);
        set
        {
            _ipv4Mode = value;
            UseDhcp = value != Ipv4Mode.Manual;
        }
    }

    public string? IpAddress { get; set; }

    public string? SubnetMask { get; set; } = "255.255.255.0";

    public string? Gateway { get; set; }

    public string? PrimaryDns { get; set; }

    public string? SecondaryDns { get; set; }

    public DohMode PrimaryDnsDoh { get; set; } = DohMode.Off;

    public string? PrimaryDnsDohTemplate { get; set; }

    public bool PrimaryDnsDohAllowFallback { get; set; }

    public DohMode SecondaryDnsDoh { get; set; } = DohMode.Off;

    public string? SecondaryDnsDohTemplate { get; set; }

    public bool SecondaryDnsDohAllowFallback { get; set; }

    public NetworkCategory? NetworkCategory { get; set; }

    public Ipv6Mode Ipv6Mode { get; set; } = Ipv6Mode.Unchanged;

    public string? Ipv6Address { get; set; }

    public string? Ipv6PrefixLength { get; set; }

    public string? Ipv6Gateway { get; set; }

    public string? Ipv6PrimaryDns { get; set; }

    public string? Ipv6SecondaryDns { get; set; }

    public DohMode Ipv6PrimaryDnsDoh { get; set; } = DohMode.Off;

    public string? Ipv6PrimaryDnsDohTemplate { get; set; }

    public bool Ipv6PrimaryDnsDohAllowFallback { get; set; }

    public DohMode Ipv6SecondaryDnsDoh { get; set; } = DohMode.Off;

    public string? Ipv6SecondaryDnsDohTemplate { get; set; }

    public bool Ipv6SecondaryDnsDohAllowFallback { get; set; }

    [JsonIgnore]
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

    public NetworkProfile Clone()
    {
        return new NetworkProfile
        {
            Id = Id,
            Name = Name,
            Ipv4Mode = Ipv4Mode,
            IpAddress = IpAddress,
            SubnetMask = SubnetMask,
            Gateway = Gateway,
            PrimaryDns = PrimaryDns,
            SecondaryDns = SecondaryDns,
            PrimaryDnsDoh = PrimaryDnsDoh,
            PrimaryDnsDohTemplate = PrimaryDnsDohTemplate,
            PrimaryDnsDohAllowFallback = PrimaryDnsDohAllowFallback,
            SecondaryDnsDoh = SecondaryDnsDoh,
            SecondaryDnsDohTemplate = SecondaryDnsDohTemplate,
            SecondaryDnsDohAllowFallback = SecondaryDnsDohAllowFallback,
            NetworkCategory = NetworkCategory,
            Ipv6Mode = Ipv6Mode,
            Ipv6Address = Ipv6Address,
            Ipv6PrefixLength = Ipv6PrefixLength,
            Ipv6Gateway = Ipv6Gateway,
            Ipv6PrimaryDns = Ipv6PrimaryDns,
            Ipv6SecondaryDns = Ipv6SecondaryDns,
            Ipv6PrimaryDnsDoh = Ipv6PrimaryDnsDoh,
            Ipv6PrimaryDnsDohTemplate = Ipv6PrimaryDnsDohTemplate,
            Ipv6PrimaryDnsDohAllowFallback = Ipv6PrimaryDnsDohAllowFallback,
            Ipv6SecondaryDnsDoh = Ipv6SecondaryDnsDoh,
            Ipv6SecondaryDnsDohTemplate = Ipv6SecondaryDnsDohTemplate,
            Ipv6SecondaryDnsDohAllowFallback = Ipv6SecondaryDnsDohAllowFallback,
        };
    }
}
