using System;
using System.Drawing;
using DualBootSwitcher;

internal static class AntdUiThemeTests
{
    private static int Main()
    {
        try
        {
            SecondaryButtonsKeepReadableInteractionColors();
            Console.WriteLine("AntdUI theme tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
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
