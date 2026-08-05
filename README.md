# 双系统快速切换

一个可直接复制运行的 Windows 小工具，用于在两个 Windows 引导项之间快速切换。

## 便携运行

- 适用于 Windows 10 和 Windows 11，系统自带的 .NET Framework 4.x 即可运行，不需要安装 SDK 或额外组件。
- 分发给其他电脑时，使用 `release\DualBootSwitcher-portable.zip`；解压后双击其中的 `DualBootSwitcher.exe`。
- 程序图标已嵌入 exe，运行时不依赖旁边的图片或资源文件。
- 圆角控件使用抗锯齿绘制；悬停、按压、选择和状态文字采用短缓动，并自动跟随 Windows 的界面动画设置。
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

## 安全边界

- 程序只读取 Windows BCD 引导配置，并调用 `bcdedit /default` 设置默认启动项。
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

构建产物会生成在 `release\DualBootSwitcher.exe`，便携分发包会生成在 `release\DualBootSwitcher-portable.zip`。

## 图标来源

应用图标改编自开源 Lucide Icons 的 `monitor-cog` 图标。完整许可见 `assets\THIRD_PARTY_NOTICES.md`。

## UI 配色来源

界面使用 [颜色代码表 ui-modern-dash](https://www.ysdaima.com/palettes/ui-modern-dash/) 的 SaaS Dashboard 配色：`#6366F1`、`#818CF8`、`#E0E7FF`、`#F8FAFC`、`#1E293B`。
