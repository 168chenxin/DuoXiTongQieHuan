using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using DualBootSwitcher;

internal static class UiMotionTests
{
    private static int Main()
    {
        try
        {
            UsesEaseOutQuartTiming();
            UsesSmootherInteractionTiming();
            BlendsEveryColorChannel();
            UsesDpiScaledDrawing();
            BuildsRoundedGeometry();
            ConfiguresAntialiasedDrawing();
            Console.WriteLine("UI motion tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void UsesEaseOutQuartTiming()
    {
        AssertClose(0F, UiMotion.EaseOutQuart(0F), "The easing curve should start at zero.");
        AssertClose(0.9375F, UiMotion.EaseOutQuart(0.5F), "The easing curve should decelerate naturally.");
        AssertClose(1F, UiMotion.EaseOutQuart(1F), "The easing curve should finish at one.");
    }

    private static void UsesSmootherInteractionTiming()
    {
        AssertClose(0F, UiMotion.EaseOutCubic(0F), "The interaction curve should start at zero.");
        AssertClose(0.875F, UiMotion.EaseOutCubic(0.5F), "The interaction curve should retain visible motion after its midpoint.");
        AssertClose(1F, UiMotion.EaseOutCubic(1F), "The interaction curve should finish at one.");
        AssertTrue(
            UiTheme.StateMotionDuration >= 200 && UiTheme.StateMotionDuration <= 250,
            "State transitions should remain smooth without delaying task flow.");
        AssertTrue(
            UiTheme.PressMotionDuration >= 100 && UiTheme.PressMotionDuration < UiTheme.StateMotionDuration,
            "Press feedback should be immediate and shorter than a state transition.");
        AssertEqual(16, UiTheme.MotionFrameInterval, "Motion should target approximately 60 frames per second.");
    }

    private static void BlendsEveryColorChannel()
    {
        Color result = UiMotion.Blend(
            Color.FromArgb(10, 20, 30, 40),
            Color.FromArgb(110, 120, 130, 140),
            0.5F);

        AssertEqual(60, result.A, "Expected the alpha channel to be interpolated.");
        AssertEqual(70, result.R, "Expected the red channel to be interpolated.");
        AssertEqual(80, result.G, "Expected the green channel to be interpolated.");
        AssertEqual(90, result.B, "Expected the blue channel to be interpolated.");
    }

    private static void UsesDpiScaledDrawing()
    {
        using (var bitmap = new Bitmap(10, 10))
        {
            bitmap.SetResolution(192F, 192F);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                AssertClose(2F, UiDrawing.GetScale(graphics), "Expected the drawing scale to follow the bitmap DPI.");
            }
        }
    }

    private static void BuildsRoundedGeometry()
    {
        using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(
            new RectangleF(0F, 0F, 100F, 40F),
            10F))
        {
            RectangleF bounds = path.GetBounds();
            AssertTrue(path.PointCount > 4, "Rounded geometry should contain curved path points.");
            AssertClose(0F, bounds.Left, "Rounded geometry should preserve the left edge.");
            AssertClose(100F, bounds.Right, "Rounded geometry should preserve the right edge.");
        }
    }

    private static void ConfiguresAntialiasedDrawing()
    {
        using (var bitmap = new Bitmap(10, 10))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            UiDrawing.ConfigureQuality(graphics);
            AssertTrue(
                graphics.SmoothingMode == SmoothingMode.AntiAlias,
                "Rounded controls should use antialiased smoothing.");
            AssertTrue(
                graphics.PixelOffsetMode == PixelOffsetMode.HighQuality,
                "Rounded controls should use high-quality pixel offsets.");
        }
    }

    private static void AssertClose(float expected, float actual, string message)
    {
        if (Math.Abs(expected - actual) > 0.001F)
        {
            throw new InvalidOperationException(
                message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
