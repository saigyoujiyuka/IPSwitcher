# IPSwitcher · 网络配置切换工具

*A desktop tool for switching Windows network configurations with one click · 一键切换 Windows 网络配置的桌面工具*

---

[简体中文](#简体中文) | [English](#english)

---

## 简体中文

### 简介

通过预置配置文件，快速在不同网络环境（公司 / 家庭 / 实验室等）间切换 IP 地址、DNS 及网络类别。

### 功能

- **配置文件管理** — 新建、编辑、删除多个网络配置文件，JSON 格式存储
- **DHCP / 静态 IP** — 支持自动获取和手动静态配置两种模式
- **IPv4 全参数** — IP 地址、子网掩码、网关、首选 / 备用 DNS
- **网络类别** — 切换公用网络或专用网络，控制网络发现与文件共享
- **适配器自动检测** — 列出本机所有网卡，默认选中当前活跃适配器
- **一键应用** — 选定适配器和配置文件，单击应用
- **当前配置回显** — 应用后自动刷新显示实际生效的 IP / DNS / 网络类别
- **系统托盘** — 最小化到托盘，右键菜单可快速应用配置或退出
- **单实例** — 重复启动自动唤起已有窗口
- **主题切换** — 浅色 / 深色 / 跟随系统，支持 Windows 11 Mica 背景
- **导入 / 导出** — JSON 格式，方便备份与多机共享

### 系统要求

- Windows 10 1809+ / Windows 11
- [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download)
- **管理员权限**（配置网卡必需，启动时自动 UAC 提权）

### 构建

```bash
git clone <repo-url>
cd IPSwitcher
dotnet build -c Release
```

产物位于 `src/IPSwitcher/bin/Release/net10.0-windows/IPSwitcher.exe`。

#### 依赖

| 包 | 用途 |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM 源生成器 |
| WinForms (`UseWindowsForms`) | 系统托盘图标 |

### 使用

1. 双击 `IPSwitcher.exe`，UAC 确认后启动
2. 顶部下拉框选择目标网络适配器（默认选中当前活跃网卡）
3. 左侧列表选择配置文件，右侧面板编辑参数
4. 点击**「应用到当前适配器」**，状态栏反馈结果
5. 下方「当前实际配置」区域自动刷新显示生效值

#### 配置文件字段

| 字段 | 说明 |
|---|---|
| 名称 | 配置显示名称 |
| 启用 DHCP | 勾选后 IP / 子网掩码 / 网关 / DNS 自动获取 |
| 网络类别 | 不修改 / 公用网络 / 专用网络 |
| IP 地址 | 仅静态模式，须为合法 IPv4 |
| 子网掩码 | 仅静态模式，须为合法连续掩码 |
| 网关 | 可选 |
| 首选 DNS | 可选 |
| 备用 DNS | 可选，需先填首选 DNS |

#### 托盘操作

- **双击托盘图标** → 恢复窗口
- **右键菜单** → 显示窗口 / 刷新适配器 / 快速应用某配置 / 退出

### 存储位置

所有数据存储在 `%AppData%\IPSwitcher\`：

| 文件 | 说明 |
|---|---|
| `profiles.json` | 所有网络配置文件 |
| `settings.json` | 主题、上次选中的适配器 / 配置文件 |

### 技术架构

```
IPSwitcher.sln
└── src/IPSwitcher/
    ├── Models/          # 数据模型
    ├── Services/        # netsh / PowerShell 调用、适配器枚举、配置回读
    ├── ViewModels/      # MVVM ViewModel + 转换器
    ├── Views/           # (预留)
    ├── Helpers/         # IPv4 校验、DWM / Mica、主题检测
    ├── Themes/          # 浅色 / 深色 ResourceDictionary + Fluent 样式
    ├── Assets/          # 应用图标
    ├── MainWindow.xaml  # 主界面
    └── App.xaml         # 入口 + 主题切换 + 单例
```

应用配置通过以下 Windows 原生命令执行：

| 操作 | 命令 |
|---|---|
| DHCP / 静态 IP | `netsh interface ip set address` |
| DNS | `netsh interface ip set dns` / `add dns` |
| 网络类别 | `Set-NetConnectionProfile` (PowerShell) |

### 许可

MIT

---

## English

### Overview

Quickly switch IP address, DNS, and network category between different network environments (office / home / lab, etc.) using preset profiles.

### Features

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

### Requirements

- Windows 10 1809+ / Windows 11
- [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download)
- **Administrator privileges** (required for NIC configuration; auto-elevated via UAC)

### Build

```bash
git clone <repo-url>
cd IPSwitcher
dotnet build -c Release
```

Output: `src/IPSwitcher/bin/Release/net10.0-windows/IPSwitcher.exe`.

#### Dependencies

| Package | Purpose |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM source generators |
| WinForms (`UseWindowsForms`) | System tray icon |

### Usage

1. Double-click `IPSwitcher.exe`, confirm the UAC prompt
2. Select the target network adapter from the dropdown (defaults to the active one)
3. Choose a profile from the left panel and edit parameters in the right panel
4. Click **"Apply to Current Adapter"** and check the status bar for feedback
5. The "Current Actual Config" section auto-refreshes to show effective values

#### Profile Fields

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

#### Tray Operations

- **Double-click tray icon** → Restore window
- **Right-click menu** → Show window / Refresh adapters / Quick apply a profile / Exit

### Storage

All data is stored in `%AppData%\IPSwitcher\`:

| File | Description |
|---|---|
| `profiles.json` | All network profiles |
| `settings.json` | Theme, last selected adapter / profile |

### Architecture

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

### License

MIT
