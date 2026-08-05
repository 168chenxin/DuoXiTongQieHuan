# Product

## Register

product

## Users

在一台电脑上维护两个 Windows 系统、需要在重启前快速选择默认启动系统的人。使用时通常正在结束当前工作，希望确认目标分区后用一次明确操作完成切换。

## Product Purpose

读取 Windows 启动菜单中的可选系统，让用户安全地设定下次默认启动项，并按需立即重启。成功的标准是用户不必再进入 msconfig，也不会误选隐藏恢复加载器。

## Brand Personality

克制、可靠、直接。

## Anti-references

采用 `ui-modern-dash` 的浅色现代后台视觉，但不要照搬网页营销 Hero、广告模块或堆叠数据卡。不要使用紫色发光效果、复杂渐变或只有颜色却没有状态含义的装饰。

## Design Principles

- 让当前默认系统和即将切换的目标一眼可辨。
- 将 BCD 修改和重启视为需要明确确认的系统操作。
- 使用熟悉的 Windows 表格、按钮和提示交互，不发明新的控件。
- 使用 `ui-modern-dash` 的靛蓝、浅靛蓝、浅灰背景和深蓝灰文字建立一致层级。
- 分发包不依赖运行时旁的资源文件。

## Accessibility & Inclusion

正文与背景保持高对比，状态不只依赖颜色表达。使用 Windows 自带字体并支持系统 DPI 缩放；只为悬停、按压、选择和状态更新提供短动画，并跟随 Windows 的界面动画设置。
