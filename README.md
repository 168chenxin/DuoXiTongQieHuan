# 双系统快速切换

一个免安装的 Windows 小工具，用于在两个 Windows 引导项之间快速切换。

## 使用方法

1. 双击 `release\DualBootSwitcher.exe`。
2. 在 Windows 的管理员权限提示中点击“是”。
3. 选择要进入的系统。
4. 点击“设为默认并重启”，确认后电脑会立即重启到所选系统。

也可以点击“仅设为默认”，稍后自行重启。

## 安全边界

- 程序只读取 Windows BCD 引导配置，并调用 `bcdedit /default` 设置默认启动项。
- 不会删除、重命名或创建任何引导项。
- 只显示 Windows 启动菜单中实际可选的系统；隐藏的恢复或遗留加载器不会被显示或设为默认项。
- “设为默认并重启”会先显示确认提示；确认前不会修改引导配置。
- 该程序必须以管理员权限运行，Windows 会在启动时自动请求权限。
- 当前版本已验证中文和英文 Windows 的 `bcdedit` 输出；其他系统显示语言会显示明确的兼容性提示，而不会修改引导项。

## 重新构建

在 Windows PowerShell 中运行：

```powershell
.\build.ps1
.\run-tests.ps1
```

构建产物会生成在 `release\DualBootSwitcher.exe`。
