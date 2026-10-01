# IPSwitcher · 网络配置切换工具

*一键切换 Windows 网络配置的桌面工具 · A desktop tool for switching Windows network configurations with one click*

---

[English](README.md) | 简体中文

---

## 简介

通过预置配置文件，快速在不同网络环境（公司 / 家庭 / 实验室等）间切换 IP 地址、DNS 及网络类别。

## 功能

- **配置文件管理** — 新建、编辑、删除多个网络配置文件，JSON 格式存储
- **IPv4** — 每个适配器可选：不修改 / 自动（DHCP）/ 手动（静态 IP 地址、子网掩码、网关、首选 / 备用 DNS）
- **IPv6** — 每个适配器可选：不修改 / 自动（DHCPv6）/ 手动（静态 IP 地址、前缀长度、网关、DNS）
- **DNS over HTTPS（DoH）** — 每个 DNS 服务器（IPv4 与 IPv6）可单独设置：关 / 开（自动模板）/ 开（手动模板），以及「失败时使用未加密请求」，与 Windows 11 设置项一致
- **网络类别** — 切换公用网络或专用网络，控制网络发现与文件共享
- **适配器自动检测** — 列出本机所有网卡，默认选中当前活跃适配器
- **一键应用** — 选定适配器和配置文件，单击应用
- **当前配置回显** — 应用后自动刷新显示实际生效的 IP / IPv6 / DNS / DoH / 网络类别
- **系统托盘** — 最小化到托盘，右键菜单可快速应用配置或退出
- **单实例** — 重复启动自动唤起已有窗口
- **主题切换** — 浅色 / 深色 / 跟随系统，支持 Windows 11 Mica 背景
- **导入 / 导出** — JSON 格式，方便备份与多机共享

## 系统要求

- Windows 10 1809+ / Windows 11
- 使用 DNS over HTTPS 需 Windows 11 或 Windows Server 2022（更低版本会禁用 DoH 相关选项）
- [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download)
- **管理员权限**（配置网卡必需，启动时自动 UAC 提权）

## 构建

```bash
git clone <repo-url>
cd IPSwitcher
dotnet build -c Release
```

产物位于 `src/IPSwitcher/bin/Release/net10.0-windows/IPSwitcher.exe`。

### 依赖

| 包 | 用途 |
|---|---|
| `CommunityToolkit.Mvvm` | MVVM 源生成器 |
| WinForms (`UseWindowsForms`) | 系统托盘图标 |

## 使用

1. 双击 `IPSwitcher.exe`，UAC 确认后启动
2. 顶部下拉框选择目标网络适配器（默认选中当前活跃网卡）
3. 左侧列表选择配置文件，右侧面板编辑参数
4. 点击**「应用到当前适配器」**，状态栏反馈结果
5. 下方「当前实际配置」区域自动刷新显示生效值

### 配置文件字段

| 字段 | 说明 |
|---|---|
| 名称 | 配置显示名称 |
| 网络类别 | 不修改 / 公用网络 / 专用网络 |
| IPv4 地址模式 | 不修改 / 自动（DHCP）/ 手动（静态） |
| IP 地址 | 仅手动模式，须为合法 IPv4 |
| 子网掩码 | 仅手动模式，须为合法连续掩码 |
| 网关 | 可选 |
| 首选 DNS | 可选 |
| 首选 DNS DoH | 关 / 开（自动模板）/ 开（手动模板） |
| 首选 DNS DoH 模板 | DoH 模板 URL，选择「开（手动模板）」时必填 |
| 首选 DNS DoH 回退 | 「失败时使用未加密请求」，DoH 查询失败时改用明文 DNS |
| 备用 DNS | 可选，需先填首选 DNS |
| 备用 DNS DoH | 与首选 DNS 相同的三个选项 |
| 备用 DNS DoH 模板 | DoH 模板 URL，选择「开（手动模板）」时必填 |
| 备用 DNS DoH 回退 | 「失败时使用未加密请求」 |
| IPv6 地址模式 | 不修改 / 自动（DHCPv6）/ 手动（静态） |
| IPv6 地址 | 仅手动模式，须为合法 IPv6 地址 |
| 子网前缀长度 | 仅手动模式，取值 0–128 |
| IPv6 网关 | 可选，允许带区域索引，如 `fe80::1%12` |
| 首选 IPv6 DNS | 可选，链路本地地址允许带区域索引 |
| 首选 IPv6 DNS DoH | 关 / 开（自动模板）/ 开（手动模板） |
| 首选 IPv6 DNS DoH 模板 | DoH 模板 URL，选择「开（手动模板）」时必填 |
| 首选 IPv6 DNS DoH 回退 | 「失败时使用未加密请求」 |
| 备用 IPv6 DNS | 可选，需先填首选 IPv6 DNS |
| 备用 IPv6 DNS DoH | 与首选 IPv6 DNS 相同的三个选项 |
| 备用 IPv6 DNS DoH 模板 | DoH 模板 URL，选择「开（手动模板）」时必填 |
| 备用 IPv6 DNS DoH 回退 | 「失败时使用未加密请求」 |

### IPv4

配置里的 IPv4 地址模式与 IPv6 一样是三态：

| 模式 | 实际执行的操作 |
|---|---|
| 不修改 | 完全不动该适配器的 IPv4 地址配置 |
| 自动（DHCP） | `netsh interface ip set address … source=dhcp` |
| 手动（静态） | `netsh interface ip set address … source=static address=… mask=… [gateway=…]` |
| 手动（静态）且填了网关 | 必须同时填写首选 DNS（沿用 Windows 设置里的这条规则） |

DNS 与地址模式相互独立，和 Windows 里单独的「DNS 服务器分配」一样：填了 DNS 服务器就设为静态（并可配置
DoH）——**这会覆盖 DHCP 下发的 DNS**，Windows 也是允许这样覆盖的。留空时：

| 模式 | DNS 留空时的来源 |
|---|---|
| 不修改 | 完全不动，保持现有 DNS 服务器 |
| 自动（DHCP） | `netsh interface ip set dns … source=dhcp`，即 DHCP 下发的列表 |
| 手动（静态） | 同样执行 `netsh interface ip set dns … source=dhcp`（与 Windows 一致）；注意静态地址通常没有 DHCP 租约，因此实际可能获取不到任何 DNS |

还有一条规则照搬自 Windows 设置对话框：**手动配置里填了网关就必须填首选 DNS**，否则保存会失败（设置里提示
「配置网关需要 DNS」，本程序提示「配置网关时必须填写首选 DNS」）；不带网关的手动配置可以留空 DNS。

### IPv6

Windows 11 每个适配器只有一个 IPv6 开关，而把它**关掉并不会禁用 IPv6 协议**——只是回退为自动获取地址
（DHCPv6 / 路由器通告）。因此配置里提供三种状态：

| 模式 | 实际执行的操作 |
|---|---|
| 不修改 | 完全不动该适配器的 IPv6 配置 |
| 自动（DHCPv6） | 先删除持久化的（手动）IPv6 地址与默认路由，再执行 `Set-NetIPInterface -Dhcp Enabled` 以及 `netsh interface ipv6 set interface … routerdiscovery=enabled managedaddress=enabled otherstateful=enabled` |
| 手动（静态） | 同样的清理，外加 `-Dhcp Disabled`，再用 `netsh interface ipv6 add address … /<前缀>` 设置地址；填了网关时用 `netsh interface ipv6 add route prefix=::/0 …` 添加默认路由 |

IPv6 DNS 与地址模式相互独立：填了就用 `netsh interface ipv6 set dnsservers … source=static` 设为静态，
留空则保持（或恢复为）由 DHCPv6 下发。

### DNS over HTTPS

Windows 11 的 DoH 设置按「网络接口 + DNS 服务器地址」保存。`netsh` 和 `DnsClient` PowerShell 命令都不写这部分
按接口保存的状态，因此本程序写入与「设置」应用完全相同的注册表值：

| 注册表值 | 含义 |
|---|---|
| `HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters\{接口GUID}\DohInterfaceSettings\{Doh\|Doh6}\{DNS 服务器}\DohFlags` | `1` = 自动模板，`2` = 手动模板，`+4` = 同时允许回退到未加密请求 |
| `…\DohInterfaceSettings\{Doh\|Doh6}\{DNS 服务器}\DohTemplate` | 模板 URL，仅当 `DohFlags` 带手动模板位时使用 |
| `HKLM\SYSTEM\CurrentControlSet\Services\Dnscache\Parameters\DohWellKnownServers\{DNS 服务器}\Template` | 系统已内置的该解析器模板（「自动模板」的来源） |

其中 `Doh` 存放 IPv4 DNS 服务器，`Doh6` 存放 IPv6 的。

说明：

- 应用配置时会先清除该适配器上已有的全部 DoH 设置，与「设置」应用点保存时的行为一致；把配置切回 DHCP 时同样会清除。
- 「开（自动模板）」只对系统已知的解析器有效——Cloudflare、Google、Quad9，以及通过
  `Add-DnsClientDohServerAddress` 注册过的地址。地址未知时日志会给出警告，并建议改用「开（手动模板）」。
- 每次应用结束都会调用 `Register-DnsClient` 并刷新解析器缓存，使设置立即生效。

### 托盘操作

- **双击托盘图标** → 恢复窗口
- **右键菜单** → 显示窗口 / 刷新适配器 / 快速应用某配置 / 退出

## 存储位置

所有数据存储在 `%AppData%\IPSwitcher\`：

| 文件 | 说明 |
|---|---|
| `profiles.json` | 所有网络配置文件 |
| `settings.json` | 主题、上次选中的适配器 / 配置文件 |

## 技术架构

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
| IPv6 地址 | `netsh interface ipv6 add address`（持久存储） |
| IPv6 网关 | `netsh interface ipv6 add route prefix=::/0` |
| IPv6 DHCPv6 / 路由器发现 | `Set-NetIPInterface -Dhcp`（PowerShell）+ `netsh interface ipv6 set interface` |
| IPv6 DNS | `netsh interface ipv6 set dnsservers` / `add dnsservers` |
| DNS over HTTPS | 注册表：`…\Services\Dnscache\InterfaceSpecificParameters\{接口GUID}\DohInterfaceSettings`（`Doh` / `Doh6`） |
| 刷新 DNS 客户端 | `Register-DnsClient` / `Clear-DnsClientCache`（PowerShell） |
| 网络类别 | `Set-NetConnectionProfile` (PowerShell) |

## 许可

MIT
