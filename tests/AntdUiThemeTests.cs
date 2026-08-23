using System;
using System.Drawing;
using DualBootSwitcher;

internal static class AntdUiThemeTests
{
    private static int Main()
    {
        try
        {
            UsesCorporateCleanPalette();
            SecondaryButtonsKeepReadableInteractionColors();
            DialogsUseUnifiedRoundedChrome();
            Console.WriteLine("AntdUI theme tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void UsesCorporateCleanPalette()
    {
        AssertColor(
            Color.FromArgb(248, 250, 252),
            UiTheme.Canvas,
            "The canvas should use the corporate neutral palette.");
        AssertColor(
            Color.FromArgb(255, 255, 255),
            UiTheme.Surface,
            "The surface should remain white.");
        AssertColor(
            Color.FromArgb(15, 23, 42),
            UiTheme.Ink,
            "The ink color should use the corporate neutral palette.");
        AssertColor(
            Color.FromArgb(37, 99, 235),
            UiTheme.Primary,
            "The primary color should use the corporate clean palette.");
        AssertColor(
            Color.FromArgb(239, 246, 255),
            UiTheme.AccentSoft,
            "The soft accent should use the corporate clean palette.");
    }

    private static void DialogsUseUnifiedRoundedChrome()
    {
        using (var dialog = new ApplicationDialog(
            null,
            "操作提示",
            "用于验证统一窗口外壳。",
            "知道了",
            false,
            DialogKind.Info))
        {
            if (dialog.FormBorderStyle != System.Windows.Forms.FormBorderStyle.None)
            {
                throw new InvalidOperationException("Application dialogs must use unified rounded chrome.");
            }

            if (dialog.Padding.All != 1)
            {
                throw new InvalidOperationException("Application dialogs must preserve the unified one-pixel frame.");
            }
        }
    }

    private static void SecondaryButtonsKeepReadableInteractionColors()
    {
        using (AntdUI.Button button = UiFactory.CreateButton("刷新启动项", 156, false))
        {
            AssertColor(
                UiTheme.PrimaryHover,
                button.BackHover,
                "Default AntdUI buttons use BackHover as their hover text and border color.");
            AssertColor(
                UiTheme.PrimaryPressed,
                button.BackActive,
                "Default AntdUI buttons use BackActive as their pressed text and border color.");
            AssertColor(
                UiTheme.Ink,
                button.ForeColor,
                "Secondary button text should remain readable at rest.");
        }
    }

    private static void AssertColor(Color expected, Color? actual, string message)
    {
        if (!actual.HasValue || actual.Value.ToArgb() != expected.ToArgb())
        {
            throw new InvalidOperationException(
                message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
