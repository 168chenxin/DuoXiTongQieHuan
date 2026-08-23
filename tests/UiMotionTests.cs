using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using DualBootSwitcher;

internal static class UiMotionTests
{
    private static int Main()
    {
        try
        {
            UsesEaseOutQuartTiming();
            UsesSmootherInteractionTiming();
            SelectionFeedbackUsesStableTiming();
            BlendsEveryColorChannel();
            UsesDpiScaledDrawing();
            BuildsRoundedGeometry();
            ConfiguresAntialiasedDrawing();
            AnimatesPaintOffsetWithoutChangingLayout();
            OverlayAnimationDoesNotChangeParentColor();
            AppleBootListKeepsRowsStableWhileSelecting();
            AppleBootListUsesStructuredHeaderSurface();
            DashboardHeaderAndSummaryKeepApprovedGeometry();
            StopsAnimationWithoutCompletingIt();
            Console.WriteLine("UI motion tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void SelectionFeedbackUsesStableTiming()
    {
        AssertTrue(
            UiTheme.StateMotionDuration >= 150 && UiTheme.StateMotionDuration <= 180,
            "State transitions should use stable timing.");
        AssertTrue(
            UiTheme.PressMotionDuration >= 80 && UiTheme.PressMotionDuration < UiTheme.StateMotionDuration,
            "Press feedback should be shorter than a state transition.");

        using (var control = new Panel { Bounds = new Rectangle(10, 20, 120, 48) })
        {
            Rectangle originalBounds = control.Bounds;
            control.AnimatePaintOffset(8F, 4F, 0);
            AssertTrue(control.Bounds == originalBounds, "Paint offset animation must not change layout bounds.");
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
            UiTheme.StateMotionDuration >= 150 && UiTheme.StateMotionDuration <= 180,
            "State transitions should match the OrbiEn interaction timing.");
        AssertTrue(
            UiTheme.PressMotionDuration >= 80 && UiTheme.PressMotionDuration < UiTheme.StateMotionDuration,
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

    private static void AnimatesPaintOffsetWithoutChangingLayout()
    {
        using (var control = new Panel { Bounds = new Rectangle(10, 20, 120, 48) })
        {
            Rectangle originalBounds = control.Bounds;
            control.AnimatePaintOffset(8F, 4F, 0);
            PointF offset = control.GetPaintOffset();
            AssertClose(8F, offset.X, "Paint offset should reach the requested X value.");
            AssertClose(4F, offset.Y, "Paint offset should reach the requested Y value.");
            AssertTrue(control.Bounds == originalBounds, "Paint offset animation must not change layout bounds.");
        }
    }

    private static void OverlayAnimationDoesNotChangeParentColor()
    {
        using (var control = new Panel { BackColor = Color.CornflowerBlue, Size = new Size(120, 48) })
        {
            Color originalColor = control.BackColor;
            control.AnimateOverlayColor(Color.FromArgb(80, Color.Red), 0);
            AssertTrue(control.BackColor == originalColor, "Overlay animation must not change the parent BackColor.");
            AssertEqual(0, control.Controls.Count, "A completed transient overlay should clean itself up.");
        }
    }

    private static void StopsAnimationWithoutCompletingIt()
    {
        if (!UiMotion.IsEnabled)
        {
            return;
        }

        bool completed = false;
        int token = UiMotion.Start(
            delegate(float progress) { },
            1000,
            delegate { completed = true; },
            UiMotion.EaseOutQuart);
        UiMotion.Stop(token);
        Application.DoEvents();
        AssertTrue(!completed, "Stopping an animation must not invoke its completion callback.");
    }

    private static void AppleBootListKeepsRowsStableWhileSelecting()
    {
        using (var control = new AppleBootList { Bounds = new Rectangle(10, 20, 520, 220) })
        {
            control.SetItems(new[]
            {
                new AppleBootListItem { Name = "Windows 11", Status = "当前默认" },
                new AppleBootListItem { Name = "Windows 10", Status = "可切换" }
            });
            control.SelectedIndex = 0;
            Rectangle originalBounds = control.Bounds;
            control.SelectedIndex = 1;
            AssertEqual(68, control.RowHeight, "Boot list rows should keep the approved stable height.");
            AssertEqual(34, control.HeaderHeight, "Boot list headers should keep the approved stable height.");
            AssertTrue(control.Bounds == originalBounds, "Selecting a boot row must not change layout bounds.");
        }
    }

    private static void AppleBootListUsesStructuredHeaderSurface()
    {
        using (var list = new AppleBootList { Size = new Size(480, 170) })
        using (var bitmap = new Bitmap(list.Width, list.Height))
        {
            list.SetItems(new[]
            {
                new AppleBootListItem { Name = "Windows 11", Status = "当前默认" }
            });
            list.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));

            AssertColor(UiTheme.Vibrancy, bitmap.GetPixel(8, 8),
                "The boot-list header should use the muted reference surface.");
            Color separatorPixel = bitmap.GetPixel(8, list.HeaderHeight - 1);
            AssertTrue(
                separatorPixel.R < UiTheme.Vibrancy.R &&
                separatorPixel.G < UiTheme.Vibrancy.G &&
                separatorPixel.B < UiTheme.Vibrancy.B,
                "The boot-list header should end with a visible separator.");
        }
    }

    private static void DashboardHeaderAndSummaryKeepApprovedGeometry()
    {
        AssertEqual(58, UiTheme.DashboardHeaderHeight,
            "The app bar should keep the approved fixed height.");
        AssertEqual(88, UiTheme.DashboardSummaryBandHeight,
            "The startup summary should keep the approved bounded height.");
        AssertTrue(
            UiTheme.DashboardStackBreakpoint < 980,
            "The dashboard must be able to switch to a stacked layout before the desktop minimum width.");
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

    private static void AssertColor(Color expected, Color actual, string message)
    {
        if (expected.ToArgb() != actual.ToArgb())
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
