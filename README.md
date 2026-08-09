<p align="center">
  <img src="assets/dual-boot-switcher-logo.png" width="132" alt="双系统快速切换 Logo">
</p>

<h1 align="center">双系统快速切换</h1>

<p align="center">一个可直接复制运行的现代 Windows 小工具，用于在多个 Windows 引导项之间快速切换。</p>

<p align="center">
  <img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows&logoColor=white" alt="支持 Windows 10 和 Windows 11">
  <img src="https://img.shields.io/badge/.NET%20Framework-4.x-512BD4" alt="基于 .NET Framework 4.x">
  <img src="https://img.shields.io/badge/便携运行-无需安装-16A34A" alt="便携运行，无需安装">
</p>

## 下载

打开 GitHub 仓库的 **Releases** 页面，下载 `DualBootSwitcher.exe`（单文件版）或 `DualBootSwitcher-portable.zip`（附带 README 和许可证）。无需安装额外组件；首次运行仍会按 Windows 的 UAC 规则申请管理员权限。

## 界面预览

<p align="center">
  <img src="assets/app-preview.png" width="860" alt="双系统快速切换主界面，包含启动项列表、所选系统详情和切换操作">
</p>

<p align="center"><sub>选择目标系统后，可以仅修改默认启动项，也可以确认后立即切换并重启。</sub></p>

## 便携运行

- 适用于 Windows 10 和 Windows 11，使用系统自带的 .NET Framework 4.x；不需要安装 SDK、运行库、DLL 或其他附件。
- 直接下载并双击 `release\DualBootSwitcher.exe` 即可运行；`release\DualBootSwitcher-portable.zip` 仅用于方便分发和附带说明文件。
- Logo 和程序图标已嵌入 exe；标题区直接使用内嵌高清 PNG 绘制，不会放大低分辨率 ICO，也不依赖旁边的资源文件。
- AntdUI 2.4.4、Logo、许可证和全部运行代码均嵌入 exe，运行时不需要旁边放置 DLL 或其他资源。
- 表格选择、按钮悬停/按压/加载、输入焦点和状态文字采用短缓动，并自动跟随 Windows 的界面动画设置。
- 当前便携版未使用商业代码签名证书；从网络下载后若 Windows SmartScreen 提示，请先确认文件来源再选择运行。

## 管理员权限

- 程序清单使用 `requireAdministrator`：当前进程未提权时，Windows 会在启动时自动显示 UAC；如果已经拥有提升后的管理员令牌，则直接进入软件，不会重复申请。
- 程序还包含 `runas` 代码兜底，用于清单未被宿主正确应用的情况。用户取消 UAC 后，软件不会修改任何引导设置。

## 使用方法

1. 双击 `release\DualBootSwitcher.exe`。
2. 在 Windows 的管理员权限提示中点击“是”。
3. 选择要进入的系统。
4. 点击“切换并重启”，确认后电脑会立即重启到所选系统。

也可以点击“仅设为默认”，稍后自行重启。

在启动菜单中选中系统后，点击“编辑备注”或双击该行，可以保存“工作”“游戏”“测试”等用途说明。备注只保存在当前 Windows 用户的设置中，不会改动 BCD；换电脑后需要重新设置备注。

选中系统后，右侧详情区会同时显示系统名称、分区、当前状态和用途备注；切换按钮只在目标不是当前默认系统时可用。

“启动等待”按钮显示开机选择系统的当前超时时间。点击后可以设置 `0` 到 `999` 秒，点击“保存修改”后立即生效。设置为 `0` 秒会直接进入默认系统，不再等待选择。

## 安全边界

- 程序只读取 Windows BCD 引导配置，并调用 `bcdedit /default` 设置默认启动项、调用 `bcdedit /timeout` 设置启动菜单等待时间。
- 启动项备注保存在当前用户注册表 `HKCU\Software\DualBootSwitcher\BootRemarks`，与 BCD 修改完全分开。
- 不会删除、重命名或创建任何引导项。
- 只显示 Windows 启动菜单中实际可选的系统；隐藏的恢复或遗留加载器不会被显示或设为默认项。
- “切换并重启”会先显示确认提示；确认前不会修改引导配置。
- 该程序必须以管理员权限运行，Windows 会在启动时自动请求权限。
- 当前版本已验证中文和英文 Windows 的 `bcdedit` 输出；其他系统显示语言会显示明确的兼容性提示，而不会修改引导项。

## 重新构建

在 Windows PowerShell 中运行：

```powershell
.\build.ps1
.\run-tests.ps1
```

构建产物会生成在 `release\DualBootSwitcher.exe`，便携分发包会生成在 `release\DualBootSwitcher-portable.zip`。GitHub Actions 会在推送 `v*` 标签时自动构建并创建 Release。

首次构建会从 NuGet 下载固定版本的 AntdUI，并校验 SHA-256。该 DLL 只用于编译并会嵌入 exe，最终发布目录不会包含外部 DLL。

## UI 与动画

- 使用 AntdUI 提供统一的 GDI+ 按钮、面板、表格、输入控件和模态提示。
- 沿用 `ui-modern-dash` 的靛蓝、浅靛蓝、白色和冷灰色层级，不使用渐变文字、玻璃效果或装饰性长动画。
- 动效只表达悬停、按压、加载、行选择和状态更新；Windows 关闭客户端动画时会同步关闭应用动画。

## 图标来源

应用 Logo 使用用户提供的 `assets\dual-boot-switcher-logo.png`，构建时会裁掉透明外边距并嵌入多尺寸 ICO。运行时不读取该 PNG。

## UI 配色来源

界面使用 [颜色代码表 ui-modern-dash](https://www.ysdaima.com/palettes/ui-modern-dash/) 的 SaaS Dashboard 配色：`#6366F1`、`#818CF8`、`#E0E7FF`、`#F8FAFC`、`#1E293B`。

## 第三方组件

现代控件由 [AntdUI](https://gitee.com/AntdUI/AntdUI) 提供，版本 `2.4.4`，采用 Apache License 2.0。完整许可证和第三方声明已嵌入 exe，并包含在便携 ZIP 中。
