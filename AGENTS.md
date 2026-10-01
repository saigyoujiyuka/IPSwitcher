# AGENTS.md

## Overview

Windows-only WPF desktop app that switches NIC IP / DNS / network category via `netsh` and PowerShell, and DNS over HTTPS via the DoH registry keys. Single project: `src/IPSwitcher` (`net10.0-windows`). `README.md` (English) and `README.zh.md` (Simplified Chinese) both document features and usage; together they are the user-facing source of truth.

## Build & Verify

- Build from repo root: `dotnet build -c Release` (solution file is `IPSwitcher.slnx`).
- Output: `src/IPSwitcher/bin/Release/net10.0-windows/IPSwitcher.exe`.
- No tests, linter, formatter config, or CI exist. A successful build is the only automated verification.
- Do not launch the exe to verify changes: it is a GUI app, `app.manifest` requests `requireAdministrator` (UAC), and applying a profile mutates real machine network settings.

## Architecture

- No DI container: services are hand-wired in `App.xaml.cs` `OnStartup` and passed to `MainViewModel` via constructor.
- `Views/` is empty/reserved. All UI is `MainWindow.xaml` (+ code-behind).
- WPF and WinForms are both enabled (`UseWindowsForms` in csproj) for the tray `NotifyIcon`; WinForms types are aliased (`WinForms = System.Windows.Forms`).
- `ProfileViewModel` wraps `NetworkProfile Source` and writes through on every property setter; persistence is explicit via `MainViewModel.PersistProfiles()` / `PersistOnExit()`.
- `DohSettingsService` owns the per-interface DNS-over-HTTPS registry state: `NetworkConfigService` calls it after the DNS step, and `CurrentConfigReader` uses it for the read-back panel.
- State lives in `%AppData%\IPSwitcher\` as camelCase JSON (`profiles.json`, `settings.json`); `NetworkCategory` and `DohMode` serialize as strings.
- Single instance is enforced with a global mutex; closing the window hides to tray unless exited from the tray menu (`_isExplicitClose`).

## Conventions & Gotchas

- User-visible strings are Simplified Chinese, including code-behind tray menu text; code comments are English. Match both.
- Docs are bilingual: `README.md` is English, `README.zh.md` is Simplified Chinese. Keep the two in lockstep — same section headings, same order, same tables — and keep the `[简体中文](README.zh.md) | [English](README.md)` language links at the top of both. Only the language-link label stays in Chinese inside `README.md`.
- Theme brushes must be defined with identical keys in **both** `Themes/Colors.Dark.xaml` and `Themes/Colors.Light.xaml`; `ThemeManager` clears and re-merges these dictionaries at runtime, so a key missing from one theme breaks that theme. Shared control styles live in `Themes/Controls.xaml`.
- External process output encodings differ and must be preserved: `netsh` output is read as UTF-16 (`Encoding.Unicode`), PowerShell output as UTF-8 with a `[Console]::OutputEncoding=[Text.Encoding]::UTF8;` prefix (`NetworkConfigService.cs`, `CurrentConfigReader.cs`).
- Command success is judged by process exit code, not by output text.
- DNS over HTTPS has no per-interface API: neither `netsh dnsclient` nor the `DnsClient` cmdlets write the state the Settings app shows, so `DohSettingsService` writes `HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters\{InterfaceGuid}\DohInterfaceSettings\{Doh|Doh6}\{server}` directly. `DohFlags` (REG_QWORD) is `1` = automatic template, `2` = manual template, `+4` = fall back to unencrypted DNS; `DohTemplate` (REG_SZ) is only meaningful with the manual bit. Known templates come from `…\Dnscache\Parameters\DohWellKnownServers\{server}\Template`. Applying a profile clears the adapter's whole `DohInterfaceSettings` subtree first, matching Windows Settings. DoH needs build 20348+; `DohSettingsService.IsSupported` gates it.
