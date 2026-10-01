# IPSwitcher

*A desktop tool for switching Windows network configurations with one click*

---

English | [简体中文](README.zh.md)

---

## Overview

Quickly switch IP address, DNS, and network category between different network environments (office / home / lab, etc.) using preset profiles.

## Features

- **Profile Management** — Create, edit, delete multiple profiles stored as JSON
- **IPv4** — Per adapter: leave unchanged / automatic (DHCP) / static IP with subnet mask, gateway, primary/secondary DNS
- **IPv6** — Per adapter: leave unchanged / automatic (DHCPv6) / static address, prefix length, gateway, DNS
- **DNS over HTTPS (DoH)** — Per DNS server (IPv4 and IPv6): Off / On (automatic template) / On (manual template), plus "fall back to unencrypted requests" — the same options the Windows 11 Settings app offers
- **Network Category** — Switch between Public or Private network to control discovery and sharing
- **Auto Detect Adapters** — Lists all NICs, defaults to the currently active one
- **One-Click Apply** — Select adapter and profile, then apply with a single click
- **Live Current Config** — Auto-refreshes to show the actual effective IP/IPv6/DNS/DoH/category
- **System Tray** — Minimize to tray with right-click quick-apply menu or exit
- **Single Instance** — Re-launching brings the existing window to front
- **Theme Switcher** — Light / Dark / Follow system, with Windows 11 Mica backdrop
- **Import / Export** — JSON format for backup and sharing across machines

## Requirements

- Windows 10 1809+ / Windows 11
- Windows 11 or Windows Server 2022 for DNS over HTTPS (on older builds the DoH options stay disabled)
- [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download)
- **Administrator privileges** (required for NIC configuration; auto-elevated via UAC)

## Build

```bash
git clone <repo-url>
cd IPSwitcher
dotnet build -c Release
```

Output: `src/IPSwitcher/bin/Release/net10.0-windows/IPSwitcher.exe`.

### Dependencies

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM source generators |
| WinForms (`UseWindowsForms`) | System tray icon |

## Usage

1. Double-click `IPSwitcher.exe`, confirm the UAC prompt
2. Select the target network adapter from the dropdown (defaults to the active one)
3. Choose a profile from the left panel and edit parameters in the right panel
4. Click **"Apply to Current Adapter"** and check the status bar for feedback
5. The "Current Actual Config" section auto-refreshes to show effective values

### Profile Fields

| Field | Description |
|---|---|
| Name | Display name for the profile |
| Network Category | Do not change / Public / Private |
| IPv4 Mode | Do not change / Automatic (DHCP) / Manual (static) |
| IP Address | Manual mode only; must be a valid IPv4 address |
| Subnet Mask | Manual mode only; must be a valid consecutive mask |
| Gateway | Optional |
| Primary DNS | Optional |
| Primary DNS DoH | Off / On (automatic template) / On (manual template) |
| Primary DNS DoH Template | DoH template URL; required when the mode is "On (manual template)" |
| Primary DNS DoH Fallback | "Fall back to unencrypted requests" — use plain DNS when the DoH query fails |
| Secondary DNS | Optional; requires primary DNS |
| Secondary DNS DoH | Same choices as the primary DNS server |
| Secondary DNS DoH Template | DoH template URL; required when the mode is "On (manual template)" |
| Secondary DNS DoH Fallback | "Fall back to unencrypted requests" |
| IPv6 Mode | Do not change / Automatic (DHCPv6) / Manual (static) |
| IPv6 Address | Manual mode only; must be a valid IPv6 address |
| IPv6 Prefix Length | Manual mode only; 0–128 |
| IPv6 Gateway | Optional; a zone index such as `fe80::1%12` is accepted |
| Primary IPv6 DNS | Optional; a zone index is accepted for link-local resolvers |
| Primary IPv6 DNS DoH | Off / On (automatic template) / On (manual template) |
| Primary IPv6 DNS DoH Template | DoH template URL; required when the mode is "On (manual template)" |
| Primary IPv6 DNS DoH Fallback | "Fall back to unencrypted requests" |
| Secondary IPv6 DNS | Optional; requires primary IPv6 DNS |
| Secondary IPv6 DNS DoH | Same choices as the primary IPv6 DNS server |
| Secondary IPv6 DNS DoH Template | DoH template URL; required when the mode is "On (manual template)" |
| Secondary IPv6 DNS DoH Fallback | "Fall back to unencrypted requests" |

### IPv4

A profile carries an IPv4 mode with the same three states as IPv6:

| Mode | What is applied |
|---|---|
| Do not change | The adapter's IPv4 address configuration is left untouched |
| Automatic (DHCP) | `netsh interface ip set address … source=dhcp` |
| Manual (static) | `netsh interface ip set address … source=static address=… mask=… [gateway=…]` |
| Manual (static) with a gateway | Requires a primary DNS server (the Windows Settings rule) |

DNS is configured independently of the mode, exactly like the separate "DNS server assignment" setting in
Windows: fill in a DNS server and it is set statically (with its DoH options) — **this overrides the DNS
servers handed out by DHCP**, which is what Windows allows as well. Leave the fields empty and:

| Mode | DNS source when the fields are empty |
|---|---|
| Do not change | Untouched — the existing resolver list stays as it is |
| Automatic (DHCP) | `netsh interface ip set dns … source=dhcp` (the DHCP-provided list) |
| Manual (static) | `netsh interface ip set dns … source=dhcp` too, matching Windows; note that a static address normally has no DHCP lease, so no resolver may actually be obtained |

One rule is copied from the Windows Settings dialog: a manual configuration with a **gateway** must also
name a primary DNS server, otherwise saving fails with "配置网关需要 DNS" / this app's
"配置网关时必须填写首选 DNS". A manual configuration without a gateway may leave DNS empty.

### IPv6

Windows 11 shows one IPv6 switch per adapter, and switching it **off** does not disable the protocol — it
falls back to obtaining the address automatically (DHCPv6 / router advertisements). A profile therefore
has three states:

| Mode | What is applied |
|---|---|
| Do not change | The adapter's IPv6 configuration is left untouched |
| Automatic (DHCPv6) | Persistent (manual) IPv6 addresses and default routes are removed, then `Set-NetIPInterface -Dhcp Enabled` and `netsh interface ipv6 set interface … routerdiscovery=enabled managedaddress=enabled otherstateful=enabled` |
| Manual (static) | The same cleanup plus `-Dhcp Disabled`, then `netsh interface ipv6 add address … /<prefix>` and, when a gateway is given, `netsh interface ipv6 add route prefix=::/0 …` |

IPv6 DNS servers are configured independently of the address mode: fill them in and they are set with
`netsh interface ipv6 set dnsservers … source=static`, leave them empty and the interface keeps (or goes
back to) DNS from DHCPv6.

### DNS over HTTPS

Windows 11 stores DoH per network interface and per DNS server address. Neither `netsh` nor the
`DnsClient` PowerShell cmdlets write that per-interface state, so this app writes the same registry
values the Settings app writes:

| Registry value | Meaning |
|---|---|
| `HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters\{InterfaceGuid}\DohInterfaceSettings\{Doh\|Doh6}\{DNS server}\DohFlags` | `1` = automatic template, `2` = manual template, `+4` = also fall back to unencrypted requests |
| `…\DohInterfaceSettings\{Doh\|Doh6}\{DNS server}\DohTemplate` | template URL, used when `DohFlags` carries the manual-template bit |
| `HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\{DNS server}\Template` | template Windows already knows for that resolver ("automatic template") |

`Doh` holds IPv4 resolvers, `Doh6` IPv6 ones.

Notes:

- Applying a profile first clears the adapter's existing DoH entries, exactly like the Settings app
  does when you save the DNS dialog; switching a profile back to DHCP clears them as well.
- "On (automatic template)" only works for resolvers Windows already knows — Cloudflare, Google,
  Quad9 and anything registered with `Add-DnsClientDohServerAddress`. When the address is unknown the
  app logs a warning and suggests "On (manual template)" instead.
- Every apply ends with `Register-DnsClient` and a resolver cache flush so the new settings take
  effect immediately.

### Tray Operations

- **Double-click tray icon** → Restore window
- **Right-click menu** → Show window / Refresh adapters / Quick apply a profile / Exit

## Storage

All data is stored in `%AppData%\IPSwitcher\`:

| File | Description |
|---|---|
| `profiles.json` | All network profiles |
| `settings.json` | Theme, last selected adapter / profile |

## Architecture

```
IPSwitcher.sln
└── src/IPSwitcher/
    ├── Models/          # Data models
    ├── Services/        # netsh / PowerShell calls, adapter enumeration, config readback
    ├── ViewModels/      # MVVM ViewModels + converters
    ├── Views/           # (reserved)
    ├── Helpers/         # IPv4 validation, DWM / Mica, theme detection
    ├── Themes/          # Light / dark ResourceDictionary + Fluent styles
    ├── Assets/          # App icon
    ├── MainWindow.xaml  # Main UI layout
    └── App.xaml         # Entry point + theme switching + single instance
```

Configuration is applied through the following native Windows commands:

| Operation | Command |
|---|---|
| DHCP / Static IP | `netsh interface ip set address` |
| DNS | `netsh interface ip set dns` / `add dns` |
| IPv6 address | `netsh interface ipv6 add address` (persistent store) |
| IPv6 gateway | `netsh interface ipv6 add route prefix=::/0` |
| IPv6 DHCPv6 / router discovery | `Set-NetIPInterface -Dhcp` (PowerShell) + `netsh interface ipv6 set interface` |
| IPv6 DNS | `netsh interface ipv6 set dnsservers` / `add dnsservers` |
| DNS over HTTPS | Registry: `…\Services\Dnscache\InterfaceSpecificParameters\{InterfaceGuid}\DohInterfaceSettings` (`Doh` / `Doh6`) |
| Refresh DNS client | `Register-DnsClient` / `Clear-DnsClientCache` (PowerShell) |
| Network Category | `Set-NetConnectionProfile` (PowerShell) |

## License

MIT
