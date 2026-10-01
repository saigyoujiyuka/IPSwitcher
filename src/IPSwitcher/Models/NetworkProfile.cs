using System.Text.Json.Serialization;

namespace IPSwitcher.Models;

public sealed class NetworkProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public bool UseDhcp { get; set; } = true;

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

    [JsonIgnore]
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

    public NetworkProfile Clone()
    {
        return new NetworkProfile
        {
            Id = Id,
            Name = Name,
            UseDhcp = UseDhcp,
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
        };
    }
}
