# AGENTS.md

## Overview

Windows-only WPF desktop app that switches NIC IP / DNS / network category via `netsh` and PowerShell. Single project: `src/IPSwitcher` (`net10.0-windows`). `README.md` (English) and `README.zh.md` (Simplified Chinese) both document features and usage; together they are the user-facing source of truth.

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
- State lives in `%AppData%\IPSwitcher\` as camelCase JSON (`profiles.json`, `settings.json`); `NetworkCategory` serializes as a string.
- Single instance is enforced with a global mutex; closing the window hides to tray unless exited from the tray menu (`_isExplicitClose`).

## Conventions & Gotchas

- User-visible strings are Simplified Chinese, including code-behind tray menu text; code comments are English. Match both.
- Docs are bilingual: `README.md` is English, `README.zh.md` is Simplified Chinese. Keep the two in lockstep — same section headings, same order, same tables — and keep the `[简体中文](README.zh.md) | [English](README.md)` language links at the top of both. Only the language-link label stays in Chinese inside `README.md`.
- Theme brushes must be defined with identical keys in **both** `Themes/Colors.Dark.xaml` and `Themes/Colors.Light.xaml`; `ThemeManager` clears and re-merges these dictionaries at runtime, so a key missing from one theme breaks that theme. Shared control styles live in `Themes/Controls.xaml`.
- External process output encodings differ and must be preserved: `netsh` output is read as UTF-16 (`Encoding.Unicode`), PowerShell output as UTF-8 with a `[Console]::OutputEncoding=[Text.Encoding]::UTF8;` prefix (`NetworkConfigService.cs`, `CurrentConfigReader.cs`).
- Command success is judged by process exit code, not by output text.
