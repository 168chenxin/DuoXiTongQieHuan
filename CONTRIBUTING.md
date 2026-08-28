# Contributing

感谢参与 SysSwitch。提交改动前请先阅读 `README.md` 的安全边界。

## 开发环境

- Windows 10/11
- .NET Framework 4.x 编译器（Windows 自带）
- PowerShell

## 提交流程

1. 从 `master` 创建分支。
2. 修改代码或文档，并保持界面文案与中文/英文 BCD 兼容。
3. 运行 `./run-tests.ps1` 和 `./build.ps1`。
4. 提交 Pull Request，说明行为变化、测试结果和潜在的管理员权限影响。

请不要提交 `build/`、`release/` 或任何包含真实 BCD 数据的文件。
