# IPSwitcher

*A desktop tool for switching Windows network configurations with one click*

---

English | [简体中文](README.zh.md)

---

## Overview

Quickly switch IP address, DNS, and network category between different network environments (office / home / lab, etc.) using preset profiles.

## Features

- **Profile Management** — Create, edit, delete multiple profiles stored as JSON
- **DHCP / Static IP** — Supports both automatic (DHCP) and manual static configuration
- **Full IPv4 Support** — IP address, subnet mask, gateway, primary/secondary DNS
- **Network Category** — Switch between Public or Private network to control discovery and sharing
- **Auto Detect Adapters** — Lists all NICs, defaults to the currently active one
- **One-Click Apply** — Select adapter and profile, then apply with a single click
- **Live Current Config** — Auto-refreshes to show the actual effective IP/DNS/category
- **System Tray** — Minimize to tray with right-click quick-apply menu or exit
- **Single Instance** — Re-launching brings the existing window to front
- **Theme Switcher** — Light / Dark / Follow system, with Windows 11 Mica backdrop
- **Import / Export** — JSON format for backup and sharing across machines

## Requirements

- Windows 10 1809+ / Windows 11
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
| Enable DHCP | When checked, IP/mask/gateway/DNS are obtained automatically |
| Network Category | Do not change / Public / Private |
| IP Address | Static mode only; must be a valid IPv4 address |
| Subnet Mask | Static mode only; must be a valid consecutive mask |
| Gateway | Optional |
| Primary DNS | Optional |
| Secondary DNS | Optional; requires primary DNS |

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
| Network Category | `Set-NetConnectionProfile` (PowerShell) |

## License

MIT
