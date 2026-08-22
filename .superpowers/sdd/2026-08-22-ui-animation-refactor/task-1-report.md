# 任务 1 报告

## 变更

- 在 `tests/AntdUiThemeTests.cs` 新增并调用 `UsesFocusSplitGreenPalette`，锁定 Canvas、Surface、Ink、Primary 和 AccentSoft 颜色令牌。
- 在 `tests/UiMotionTests.cs` 新增并调用 `SelectionFeedbackUsesStableTiming`，锁定状态/按压动效时长范围及绘制偏移不改变控件 Bounds。
- 在 `src/UiTheme.cs` 将共享主题令牌切换为 Focus Split 绿色调；PrimaryHover 和 PrimaryPressed 使用更深的绿色；保留 Success、Warning 语义颜色和既有动效时长。

## 测试驱动记录

先运行新增测试时按预期失败：

```text
BcdParser tests passed.
AnnouncementParser tests passed.
UI motion tests passed.
The canvas should use the Focus Split neutral palette. Expected: Color [A=255, R=238, G=241, B=244]; actual: Color [A=255, R=240, G=245, B=250].
```

## 测试命令

```powershell
.\run-tests.ps1
```

## 实际输出

```text
BcdParser tests passed.
Announcement parser tests passed.
UI motion tests passed.
AntdUI theme tests passed.
Update service tests passed.
Boot remark tests passed.
Embedded logo source test passed.
Embedded UI dependency license test passed.
OrbiEn design attribution test passed.
Manifest elevation test passed.
Boot timeout save behavior test passed.
```
