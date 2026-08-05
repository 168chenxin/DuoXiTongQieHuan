using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal static class UiMotion
    {
        private const uint GetClientAreaAnimation = 0x1042;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(
            uint action,
            uint parameter,
            [MarshalAs(UnmanagedType.Bool)] ref bool value,
            uint updateFlags);

        public static bool IsEnabled
        {
            get
            {
                bool enabled = true;
                try
                {
                    return SystemParametersInfo(GetClientAreaAnimation, 0, ref enabled, 0) && enabled;
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
            }
        }

        public static float EaseOutQuart(float value)
        {
            float clamped = Math.Max(0F, Math.Min(1F, value));
            float inverse = 1F - clamped;
            return 1F - (inverse * inverse * inverse * inverse);
        }

        public static Color Blend(Color from, Color to, float progress)
        {
            float clamped = Math.Max(0F, Math.Min(1F, progress));
            return Color.FromArgb(
                BlendChannel(from.A, to.A, clamped),
                BlendChannel(from.R, to.R, clamped),
                BlendChannel(from.G, to.G, clamped),
                BlendChannel(from.B, to.B, clamped));
        }

        private static int BlendChannel(int from, int to, float progress)
        {
            return (int)Math.Round(from + ((to - from) * progress));
        }
    }

    internal static class UiDrawing
    {
        public static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float safeRadius = Math.Max(0F, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2F));
            if (safeRadius <= 0F)
            {
                path.AddRectangle(bounds);
                return path;
            }

            float diameter = safeRadius * 2F;
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180F, 90F);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270F, 90F);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0F, 90F);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90F, 90F);
            path.CloseFigure();
            return path;
        }

        public static void ConfigureQuality(Graphics graphics)
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        }

        public static float GetScale(Graphics graphics)
        {
            if (graphics == null || graphics.DpiX <= 0F)
            {
                return 1F;
            }

            return graphics.DpiX / 96F;
        }
    }

    internal sealed class RoundedPanel : Panel
    {
        private Color fillColor = UiTheme.Surface;
        private Color borderColor = Color.Transparent;
        private int cornerRadius = UiTheme.SurfaceCornerRadius;

        public RoundedPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint,
                true);
            base.BackColor = Color.Transparent;
        }

        public Color FillColor
        {
            get { return fillColor; }
            set
            {
                fillColor = value;
                Invalidate();
            }
        }

        public Color BorderColor
        {
            get { return borderColor; }
            set
            {
                borderColor = value;
                Invalidate();
            }
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            Color backdrop = Parent == null || Parent.BackColor == Color.Transparent
                ? UiTheme.Canvas
                : Parent.BackColor;
            eventArgs.Graphics.Clear(backdrop);

            if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
            {
                return;
            }

            UiDrawing.ConfigureQuality(eventArgs.Graphics);
            float scale = UiDrawing.GetScale(eventArgs.Graphics);
            var bounds = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);
            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(bounds, cornerRadius * scale))
            using (var fillBrush = new SolidBrush(fillColor))
            {
                eventArgs.Graphics.FillPath(fillBrush, path);
            }
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
            {
                return;
            }

            UiDrawing.ConfigureQuality(eventArgs.Graphics);
            float scale = UiDrawing.GetScale(eventArgs.Graphics);
            var bounds = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);
            if (borderColor.A <= 0)
            {
                return;
            }

            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(bounds, cornerRadius * scale))
            using (var borderPen = new Pen(borderColor, 1F))
            {
                eventArgs.Graphics.DrawPath(borderPen, path);
            }
        }
    }

    internal sealed class RoundedLabel : Label
    {
        private Color fillColor = UiTheme.AccentSoft;
        private Color backdropColor = UiTheme.Surface;
        private int cornerRadius = UiTheme.BadgeCornerRadius;

        public RoundedLabel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint,
                true);
            base.BackColor = Color.Transparent;
        }

        public Color FillColor
        {
            get { return fillColor; }
            set
            {
                fillColor = value;
                Invalidate();
            }
        }

        public Color BackdropColor
        {
            get { return backdropColor; }
            set
            {
                backdropColor = value;
                Invalidate();
            }
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(backdropColor);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
            {
                return;
            }

            UiDrawing.ConfigureQuality(eventArgs.Graphics);
            float scale = UiDrawing.GetScale(eventArgs.Graphics);
            var bounds = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);
            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(bounds, cornerRadius * scale))
            using (var fillBrush = new SolidBrush(fillColor))
            {
                eventArgs.Graphics.FillPath(fillBrush, path);
            }

            TextRenderer.DrawText(
                eventArgs.Graphics,
                Text,
                Font,
                ClientRectangle,
                Enabled ? ForeColor : UiTheme.DisabledText,
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine |
                TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class AnimatedLabel : Label
    {
        private readonly Timer animationTimer;
        private string previousText = string.Empty;
        private string targetText = string.Empty;
        private DateTime animationStartedAt;
        private float animationProgress = 1F;
        private Color backdropColor = UiTheme.Canvas;

        public AnimatedLabel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.UserPaint,
                true);
            base.BackColor = Color.Transparent;

            animationTimer = new Timer { Interval = 15 };
            animationTimer.Tick += OnAnimationTick;
        }

        public Color BackdropColor
        {
            get { return backdropColor; }
            set
            {
                backdropColor = value;
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs eventArgs)
        {
            base.OnTextChanged(eventArgs);
            string newText = Text ?? string.Empty;

            if (!IsHandleCreated || !UiMotion.IsEnabled || string.IsNullOrEmpty(targetText))
            {
                animationTimer.Stop();
                previousText = string.Empty;
                targetText = newText;
                animationProgress = 1F;
                Invalidate();
                return;
            }

            previousText = targetText;
            targetText = newText;
            animationProgress = 0F;
            animationStartedAt = DateTime.UtcNow;
            animationTimer.Start();
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(backdropColor);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            if (animationProgress >= 1F || string.IsNullOrEmpty(previousText))
            {
                DrawText(eventArgs.Graphics, targetText, ForeColor, 0);
                return;
            }

            float eased = UiMotion.EaseOutQuart(animationProgress);
            DrawText(
                eventArgs.Graphics,
                previousText,
                UiMotion.Blend(ForeColor, backdropColor, eased),
                -(int)Math.Round(eased * 2F));
            DrawText(
                eventArgs.Graphics,
                targetText,
                UiMotion.Blend(backdropColor, ForeColor, eased),
                (int)Math.Round((1F - eased) * 2F));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnAnimationTick(object sender, EventArgs eventArgs)
        {
            double elapsed = (DateTime.UtcNow - animationStartedAt).TotalMilliseconds;
            animationProgress = Math.Min(1F, (float)(elapsed / UiTheme.StateMotionDuration));
            if (animationProgress >= 1F)
            {
                animationTimer.Stop();
                previousText = string.Empty;
            }

            Invalidate();
        }

        private void DrawText(Graphics graphics, string value, Color color, int verticalOffset)
        {
            Rectangle bounds = ClientRectangle;
            bounds.Offset(0, verticalOffset);
            TextRenderer.DrawText(graphics, value, Font, bounds, color, GetTextFlags());
        }

        private TextFormatFlags GetTextFlags()
        {
            TextFormatFlags flags = TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine |
                TextFormatFlags.VerticalCenter;

            if (TextAlign == ContentAlignment.MiddleRight ||
                TextAlign == ContentAlignment.TopRight ||
                TextAlign == ContentAlignment.BottomRight)
            {
                return flags | TextFormatFlags.Right;
            }

            if (TextAlign == ContentAlignment.MiddleCenter ||
                TextAlign == ContentAlignment.TopCenter ||
                TextAlign == ContentAlignment.BottomCenter)
            {
                return flags | TextFormatFlags.HorizontalCenter;
            }

            return flags | TextFormatFlags.Left;
        }
    }

    internal sealed class AnimatedButton : Button
    {
        private readonly Timer animationTimer;
        private readonly bool isPrimary;
        private bool isHovered;
        private bool isPressed;
        private DateTime animationStartedAt;
        private int animationDuration;
        private Color startFill;
        private Color targetFill;
        private Color currentFill;
        private Color startBorder;
        private Color targetBorder;
        private Color currentBorder;
        private Color startText;
        private Color targetText;
        private Color currentText;
        private float startPressDepth;
        private float targetPressDepth;
        private float currentPressDepth;

        public AnimatedButton(bool primary)
        {
            isPrimary = primary;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            TabStop = true;

            currentFill = primary ? UiTheme.Primary : UiTheme.Surface;
            currentBorder = primary ? UiTheme.Primary : UiTheme.Border;
            currentText = primary ? UiTheme.Surface : UiTheme.Ink;
            targetFill = currentFill;
            targetBorder = currentBorder;
            targetText = currentText;

            animationTimer = new Timer { Interval = 15 };
            animationTimer.Tick += OnAnimationTick;
        }

        protected override void OnMouseEnter(EventArgs eventArgs)
        {
            base.OnMouseEnter(eventArgs);
            isHovered = true;
            RefreshVisualState(UiTheme.StateMotionDuration);
        }

        protected override void OnMouseLeave(EventArgs eventArgs)
        {
            base.OnMouseLeave(eventArgs);
            isHovered = false;
            isPressed = false;
            RefreshVisualState(UiTheme.StateMotionDuration);
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            base.OnMouseDown(eventArgs);
            if (eventArgs.Button == MouseButtons.Left)
            {
                isPressed = true;
                RefreshVisualState(UiTheme.PressMotionDuration);
            }
        }

        protected override void OnMouseUp(MouseEventArgs eventArgs)
        {
            base.OnMouseUp(eventArgs);
            if (eventArgs.Button == MouseButtons.Left)
            {
                isPressed = false;
                RefreshVisualState(UiTheme.PressMotionDuration);
            }
        }

        protected override void OnKeyDown(KeyEventArgs eventArgs)
        {
            base.OnKeyDown(eventArgs);
            if (eventArgs.KeyCode == Keys.Space || eventArgs.KeyCode == Keys.Enter)
            {
                isPressed = true;
                RefreshVisualState(UiTheme.PressMotionDuration);
            }
        }

        protected override void OnKeyUp(KeyEventArgs eventArgs)
        {
            base.OnKeyUp(eventArgs);
            if (eventArgs.KeyCode == Keys.Space || eventArgs.KeyCode == Keys.Enter)
            {
                isPressed = false;
                RefreshVisualState(UiTheme.PressMotionDuration);
            }
        }

        protected override void OnGotFocus(EventArgs eventArgs)
        {
            base.OnGotFocus(eventArgs);
            RefreshVisualState(UiTheme.StateMotionDuration);
        }

        protected override void OnLostFocus(EventArgs eventArgs)
        {
            base.OnLostFocus(eventArgs);
            isPressed = false;
            RefreshVisualState(UiTheme.StateMotionDuration);
        }

        protected override void OnEnabledChanged(EventArgs eventArgs)
        {
            base.OnEnabledChanged(eventArgs);
            if (!Enabled)
            {
                isHovered = false;
                isPressed = false;
            }
            else if (IsHandleCreated)
            {
                isHovered = ClientRectangle.Contains(PointToClient(Cursor.Position));
            }

            RefreshVisualState(UiTheme.StateMotionDuration);
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            Color backdrop = Parent == null || Parent.BackColor == Color.Transparent
                ? UiTheme.Canvas
                : Parent.BackColor;
            eventArgs.Graphics.Clear(backdrop);
            UiDrawing.ConfigureQuality(eventArgs.Graphics);

            float scale = UiDrawing.GetScale(eventArgs.Graphics);
            float pressDepth = currentPressDepth * scale;
            float inset = (0.5F * scale) + pressDepth;
            var bounds = new RectangleF(
                inset,
                inset,
                ClientSize.Width - (inset * 2F),
                ClientSize.Height - (inset * 2F));
            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(
                bounds,
                Math.Max(2F * scale, (UiTheme.ControlCornerRadius * scale) - pressDepth)))
            using (var fillBrush = new SolidBrush(currentFill))
            using (var borderPen = new Pen(currentBorder, 1F))
            {
                eventArgs.Graphics.FillPath(fillBrush, path);
                eventArgs.Graphics.DrawPath(borderPen, path);
            }

            if (Focused && ShowFocusCues && Enabled)
            {
                float focusInset = 3.5F * scale;
                var focusBounds = new RectangleF(
                    focusInset,
                    focusInset,
                    ClientSize.Width - (focusInset * 2F),
                    ClientSize.Height - (focusInset * 2F));
                using (GraphicsPath focusPath = UiDrawing.CreateRoundedRectangle(
                    focusBounds,
                    Math.Max(2F * scale, (UiTheme.ControlCornerRadius - 3F) * scale)))
                using (var focusPen = new Pen(isPrimary ? UiTheme.Surface : UiTheme.Secondary, 1F))
                {
                    eventArgs.Graphics.DrawPath(focusPen, focusPath);
                }
            }

            Rectangle textBounds = ClientRectangle;
            textBounds.Offset(0, (int)Math.Round(pressDepth));
            TextRenderer.DrawText(
                eventArgs.Graphics,
                Text,
                Font,
                textBounds,
                currentText,
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.NoPadding |
                TextFormatFlags.SingleLine |
                TextFormatFlags.VerticalCenter);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void RefreshVisualState(int duration)
        {
            Color fill;
            Color border;
            Color text;
            float pressDepth = isPressed && Enabled ? 1F : 0F;

            if (!Enabled)
            {
                fill = UiTheme.Disabled;
                border = UiTheme.Border;
                text = UiTheme.DisabledText;
            }
            else if (isPrimary)
            {
                fill = isPressed
                    ? UiTheme.PrimaryPressed
                    : (isHovered ? UiTheme.PrimaryHover : UiTheme.Primary);
                border = fill;
                text = UiTheme.Surface;
            }
            else
            {
                fill = isPressed
                    ? UiTheme.Selection
                    : (isHovered ? UiTheme.Canvas : UiTheme.Surface);
                border = isHovered || Focused ? UiTheme.Secondary : UiTheme.Border;
                text = UiTheme.Ink;
            }

            BeginTransition(fill, border, text, pressDepth, duration);
        }

        private void BeginTransition(Color fill, Color border, Color text, float pressDepth, int duration)
        {
            if (fill == targetFill && border == targetBorder && text == targetText &&
                Math.Abs(pressDepth - targetPressDepth) < 0.001F)
            {
                return;
            }

            animationTimer.Stop();
            startFill = currentFill;
            startBorder = currentBorder;
            startText = currentText;
            startPressDepth = currentPressDepth;
            targetFill = fill;
            targetBorder = border;
            targetText = text;
            targetPressDepth = pressDepth;
            animationDuration = Math.Max(1, duration);
            animationStartedAt = DateTime.UtcNow;

            if (!IsHandleCreated || !UiMotion.IsEnabled)
            {
                CompleteTransition();
                return;
            }

            animationTimer.Start();
            Invalidate();
        }

        private void OnAnimationTick(object sender, EventArgs eventArgs)
        {
            double elapsed = (DateTime.UtcNow - animationStartedAt).TotalMilliseconds;
            float linearProgress = Math.Min(1F, (float)(elapsed / animationDuration));
            float easedProgress = UiMotion.EaseOutQuart(linearProgress);
            currentFill = UiMotion.Blend(startFill, targetFill, easedProgress);
            currentBorder = UiMotion.Blend(startBorder, targetBorder, easedProgress);
            currentText = UiMotion.Blend(startText, targetText, easedProgress);
            currentPressDepth = startPressDepth + ((targetPressDepth - startPressDepth) * easedProgress);

            if (linearProgress >= 1F)
            {
                CompleteTransition();
                return;
            }

            Invalidate();
        }

        private void CompleteTransition()
        {
            animationTimer.Stop();
            currentFill = targetFill;
            currentBorder = targetBorder;
            currentText = targetText;
            currentPressDepth = targetPressDepth;
            Invalidate();
        }
    }

    internal sealed class AnimatedDataGridView : DataGridView
    {
        private readonly Timer selectionAnimationTimer;
        private DateTime selectionAnimationStartedAt;
        private Color backdropColor = UiTheme.Canvas;
        private int cornerRadius = UiTheme.SurfaceCornerRadius;

        public AnimatedDataGridView()
        {
            DoubleBuffered = true;
            selectionAnimationTimer = new Timer { Interval = 15 };
            selectionAnimationTimer.Tick += OnSelectionAnimationTick;
        }

        public Color BackdropColor
        {
            get { return backdropColor; }
            set
            {
                backdropColor = value;
                Invalidate();
            }
        }

        public int CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                cornerRadius = Math.Max(0, value);
                Invalidate();
            }
        }

        protected override void OnSelectionChanged(EventArgs eventArgs)
        {
            base.OnSelectionChanged(eventArgs);
            if (SelectedRows.Count == 0)
            {
                return;
            }

            if (!IsHandleCreated || !UiMotion.IsEnabled)
            {
                ApplySelectionColor(UiTheme.Selection);
                return;
            }

            selectionAnimationTimer.Stop();
            selectionAnimationStartedAt = DateTime.UtcNow;
            ApplySelectionColor(UiMotion.Blend(UiTheme.Surface, UiTheme.Selection, 0.12F));
            selectionAnimationTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            base.OnPaint(eventArgs);
            if (ClientSize.Width <= 1 || ClientSize.Height <= 1)
            {
                return;
            }

            UiDrawing.ConfigureQuality(eventArgs.Graphics);
            PaintCornerMasks(eventArgs.Graphics);
            float scale = UiDrawing.GetScale(eventArgs.Graphics);
            var bounds = new RectangleF(0.5F, 0.5F, ClientSize.Width - 1F, ClientSize.Height - 1F);
            using (GraphicsPath borderPath = UiDrawing.CreateRoundedRectangle(bounds, cornerRadius * scale))
            using (var borderPen = new Pen(UiTheme.Border, 1F))
            {
                eventArgs.Graphics.DrawPath(borderPen, borderPath);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                selectionAnimationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnSelectionAnimationTick(object sender, EventArgs eventArgs)
        {
            double elapsed = (DateTime.UtcNow - selectionAnimationStartedAt).TotalMilliseconds;
            float linearProgress = Math.Min(1F, (float)(elapsed / UiTheme.StateMotionDuration));
            float easedProgress = UiMotion.EaseOutQuart(linearProgress);
            ApplySelectionColor(UiMotion.Blend(UiTheme.Surface, UiTheme.Selection, easedProgress));

            if (linearProgress >= 1F)
            {
                selectionAnimationTimer.Stop();
            }
        }

        private void ApplySelectionColor(Color color)
        {
            DefaultCellStyle.SelectionBackColor = color;
            AlternatingRowsDefaultCellStyle.SelectionBackColor = color;
            Invalidate();
        }

        private void PaintCornerMasks(Graphics graphics)
        {
            float scale = UiDrawing.GetScale(graphics);
            float radius = Math.Max(0F, Math.Min(
                cornerRadius * scale,
                Math.Min(ClientSize.Width, ClientSize.Height) / 2F));
            if (radius <= 0F)
            {
                return;
            }

            float width = ClientSize.Width;
            float height = ClientSize.Height;
            using (var maskBrush = new SolidBrush(backdropColor))
            {
                using (var topLeft = new GraphicsPath())
                {
                    topLeft.AddLine(0F, 0F, radius, 0F);
                    topLeft.AddArc(0F, 0F, radius * 2F, radius * 2F, 270F, -90F);
                    topLeft.CloseFigure();
                    graphics.FillPath(maskBrush, topLeft);
                }

                using (var topRight = new GraphicsPath())
                {
                    topRight.AddLine(width - radius, 0F, width, 0F);
                    topRight.AddLine(width, 0F, width, radius);
                    topRight.AddArc(width - (radius * 2F), 0F, radius * 2F, radius * 2F, 0F, -90F);
                    topRight.CloseFigure();
                    graphics.FillPath(maskBrush, topRight);
                }

                using (var bottomRight = new GraphicsPath())
                {
                    bottomRight.AddLine(width, height - radius, width, height);
                    bottomRight.AddLine(width, height, width - radius, height);
                    bottomRight.AddArc(
                        width - (radius * 2F),
                        height - (radius * 2F),
                        radius * 2F,
                        radius * 2F,
                        90F,
                        -90F);
                    bottomRight.CloseFigure();
                    graphics.FillPath(maskBrush, bottomRight);
                }

                using (var bottomLeft = new GraphicsPath())
                {
                    bottomLeft.AddLine(0F, height - radius, 0F, height);
                    bottomLeft.AddLine(0F, height, radius, height);
                    bottomLeft.AddArc(
                        0F,
                        height - (radius * 2F),
                        radius * 2F,
                        radius * 2F,
                        90F,
                        90F);
                    bottomLeft.CloseFigure();
                    graphics.FillPath(maskBrush, bottomLeft);
                }
            }
        }
    }
}
