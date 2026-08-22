using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DualBootSwitcher
{
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

    internal enum NavigationIcon
    {
        Systems,
        Settings,
        Announcement
    }

    internal sealed class SidebarNavigationButton : Control
    {
        private readonly Timer animationTimer;
        private readonly NavigationIcon icon;
        private bool active;
        private bool hovered;
        private bool pointerPressed;
        private float visualProgress;
        private float startProgress;
        private float targetProgress;
        private DateTime animationStartedAt;

        public SidebarNavigationButton(NavigationIcon iconKind)
        {
            icon = iconKind;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);
            Cursor = Cursors.Hand;
            AccessibleRole = AccessibleRole.PushButton;
            Size = new Size(44, 44);
            TabStop = true;
            animationTimer = new Timer { Interval = UiTheme.MotionFrameInterval };
            animationTimer.Tick += OnAnimationTick;
        }

        public bool Active
        {
            get { return active; }
            set
            {
                if (active == value)
                {
                    return;
                }

                active = value;
                BeginTransition(active || hovered ? 1F : 0F);
                AccessibleDescription = active ? "当前页面" : string.Empty;
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Space || keyData == Keys.Enter)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnMouseEnter(EventArgs eventArgs)
        {
            base.OnMouseEnter(eventArgs);
            hovered = true;
            BeginTransition(1F);
        }

        protected override void OnMouseLeave(EventArgs eventArgs)
        {
            base.OnMouseLeave(eventArgs);
            hovered = false;
            pointerPressed = false;
            BeginTransition(active ? 1F : 0F);
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            base.OnMouseDown(eventArgs);
            if (eventArgs.Button == MouseButtons.Left)
            {
                pointerPressed = true;
                Focus();
            }
        }

        protected override void OnMouseUp(MouseEventArgs eventArgs)
        {
            bool shouldClick = pointerPressed &&
                eventArgs.Button == MouseButtons.Left &&
                ClientRectangle.Contains(eventArgs.Location);
            pointerPressed = false;
            base.OnMouseUp(eventArgs);
            if (shouldClick)
            {
                OnClick(EventArgs.Empty);
            }
        }

        protected override void OnKeyDown(KeyEventArgs eventArgs)
        {
            if (eventArgs.KeyCode == Keys.Space || eventArgs.KeyCode == Keys.Enter)
            {
                OnClick(EventArgs.Empty);
                eventArgs.Handled = true;
                return;
            }

            base.OnKeyDown(eventArgs);
        }

        protected override void OnGotFocus(EventArgs eventArgs)
        {
            base.OnGotFocus(eventArgs);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs eventArgs)
        {
            base.OnLostFocus(eventArgs);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(UiTheme.Header);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            UiDrawing.ConfigureQuality(eventArgs.Graphics);
            float scale = UiDrawing.GetScale(eventArgs.Graphics);
            RectangleF bounds = new RectangleF(
                1F * scale,
                1F * scale,
                ClientSize.Width - (2F * scale),
                ClientSize.Height - (2F * scale));
            Color fill = UiMotion.Blend(UiTheme.Header, UiTheme.AccentSoft, visualProgress);
            Color stroke = UiMotion.Blend(UiTheme.Muted, UiTheme.Accent, visualProgress);
            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(
                bounds,
                UiTheme.ControlCornerRadius * scale))
            using (var brush = new SolidBrush(fill))
            {
                eventArgs.Graphics.FillPath(brush, path);
            }

            DrawIcon(eventArgs.Graphics, stroke, scale);
            if (Focused && ShowFocusCues)
            {
                using (GraphicsPath focusPath = UiDrawing.CreateRoundedRectangle(
                    new RectangleF(3.5F * scale, 3.5F * scale,
                        ClientSize.Width - (7F * scale), ClientSize.Height - (7F * scale)),
                    (UiTheme.ControlCornerRadius - 2) * scale))
                using (var focusPen = new Pen(UiTheme.Secondary, 1F))
                {
                    eventArgs.Graphics.DrawPath(focusPen, focusPath);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        private void BeginTransition(float target)
        {
            animationTimer.Stop();
            startProgress = visualProgress;
            targetProgress = target;
            animationStartedAt = DateTime.UtcNow;
            if (!IsHandleCreated || !UiMotion.IsEnabled)
            {
                visualProgress = target;
                Invalidate();
                return;
            }

            animationTimer.Start();
        }

        private void OnAnimationTick(object sender, EventArgs eventArgs)
        {
            double elapsed = (DateTime.UtcNow - animationStartedAt).TotalMilliseconds;
            float linear = Math.Min(1F, (float)(elapsed / UiTheme.StateMotionDuration));
            visualProgress = startProgress +
                ((targetProgress - startProgress) * UiMotion.EaseOutQuart(linear));
            if (linear >= 1F)
            {
                animationTimer.Stop();
                visualProgress = targetProgress;
            }

            Invalidate();
        }

        private void DrawIcon(Graphics graphics, Color color, float scale)
        {
            float left = (ClientSize.Width - (22F * scale)) / 2F;
            float top = (ClientSize.Height - (22F * scale)) / 2F;
            using (var pen = new Pen(color, 1.7F * scale))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                if (icon == NavigationIcon.Systems)
                {
                    graphics.DrawRectangle(pen, left + (2F * scale), top + (3F * scale), 18F * scale, 13F * scale);
                    graphics.DrawLine(pen, left + (8F * scale), top + (20F * scale), left + (14F * scale), top + (20F * scale));
                    graphics.DrawLine(pen, left + (11F * scale), top + (16F * scale), left + (11F * scale), top + (20F * scale));
                }
                else if (icon == NavigationIcon.Settings)
                {
                    graphics.DrawEllipse(pen, left + (7F * scale), top + (7F * scale), 8F * scale, 8F * scale);
                    graphics.DrawEllipse(pen, left + (3F * scale), top + (3F * scale), 16F * scale, 16F * scale);
                    graphics.DrawLine(pen, left + (11F * scale), top, left + (11F * scale), top + (3F * scale));
                    graphics.DrawLine(pen, left + (11F * scale), top + (19F * scale), left + (11F * scale), top + (22F * scale));
                    graphics.DrawLine(pen, left, top + (11F * scale), left + (3F * scale), top + (11F * scale));
                    graphics.DrawLine(pen, left + (19F * scale), top + (11F * scale), left + (22F * scale), top + (11F * scale));
                }
                else
                {
                    graphics.DrawRectangle(pen, left + (3F * scale), top + (2F * scale), 16F * scale, 18F * scale);
                    graphics.DrawLine(pen, left + (7F * scale), top + (7F * scale), left + (15F * scale), top + (7F * scale));
                    graphics.DrawLine(pen, left + (7F * scale), top + (11F * scale), left + (15F * scale), top + (11F * scale));
                    graphics.DrawLine(pen, left + (7F * scale), top + (15F * scale), left + (12F * scale), top + (15F * scale));
                }
            }
        }
    }

    internal sealed class PageTransitionPanel : Panel
    {
        private readonly Timer animationTimer;
        private Control activePage;
        private DateTime animationStartedAt;
        private int destinationLeft;

        public PageTransitionPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            animationTimer = new Timer { Interval = UiTheme.MotionFrameInterval };
            animationTimer.Tick += OnAnimationTick;
        }

        public void ShowPage(Control page)
        {
            if (page == null || page == activePage)
            {
                return;
            }

            animationTimer.Stop();
            destinationLeft = 0;
            foreach (Control control in Controls)
            {
                control.Visible = false;
            }

            activePage = page;
            activePage.Dock = DockStyle.None;
            activePage.Size = ClientSize;
            activePage.Visible = true;
            activePage.BringToFront();
            if (!UiMotion.IsEnabled)
            {
                activePage.Left = destinationLeft;
                return;
            }

            activePage.Left = destinationLeft + 10;
            animationStartedAt = DateTime.UtcNow;
            animationTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                animationTimer.Dispose();
            }

            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs eventArgs)
        {
            base.OnResize(eventArgs);
            if (activePage != null && !activePage.IsDisposed)
            {
                activePage.Size = ClientSize;
            }
        }

        private void OnAnimationTick(object sender, EventArgs eventArgs)
        {
            if (activePage == null || activePage.IsDisposed)
            {
                animationTimer.Stop();
                return;
            }

            double elapsed = (DateTime.UtcNow - animationStartedAt).TotalMilliseconds;
            float linear = Math.Min(1F, (float)(elapsed / UiTheme.StateMotionDuration));
            float eased = UiMotion.EaseOutQuart(linear);
            activePage.Left = destinationLeft + (int)Math.Round((1F - eased) * 10F);
            if (linear >= 1F)
            {
                animationTimer.Stop();
                activePage.Left = destinationLeft;
            }
        }
    }

    internal sealed class AppleBootListItem
    {
        public object Tag { get; set; }
        public string Name { get; set; }
        public string Remark { get; set; }
        public string Status { get; set; }
        public bool IsDefault { get; set; }
    }

    internal sealed class AppleBootListEventArgs : EventArgs
    {
        public AppleBootListEventArgs(AppleBootListItem item, int index)
        {
            Item = item;
            Index = index;
        }

        public AppleBootListItem Item { get; private set; }
        public int Index { get; private set; }
    }

    internal sealed class AppleBootList : Control
    {
        private readonly List<AppleBootListItem> items = new List<AppleBootListItem>();
        private int selectedIndex = -1;
        private int hoveredIndex = -1;

        public AppleBootList()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable |
                ControlStyles.UserPaint,
                true);
            BackColor = UiTheme.Surface;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            TabStop = true;
        }

        public event EventHandler<AppleBootListEventArgs> ItemSelected;
        public event EventHandler<AppleBootListEventArgs> ItemDoubleClicked;

        public int RowHeight
        {
            get { return 58; }
        }

        public int HeaderHeight
        {
            get { return 34; }
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int next = Math.Max(-1, Math.Min(items.Count - 1, value));
                if (selectedIndex == next)
                {
                    return;
                }

                selectedIndex = next;
                Invalidate();
            }
        }

        public void SetItems(IEnumerable<AppleBootListItem> source)
        {
            items.Clear();
            if (source != null)
            {
                items.AddRange(source);
            }

            selectedIndex = items.Count == 0 ? -1 : Math.Min(selectedIndex, items.Count - 1);
            hoveredIndex = -1;
            Invalidate();
        }

        public Rectangle GetRemarkBounds(int index)
        {
            if (index < 0 || index >= items.Count)
            {
                return Rectangle.Empty;
            }

            int nameWidth = (int)Math.Round(ClientSize.Width * 0.48);
            int remarkWidth = (int)Math.Round(ClientSize.Width * 0.28);
            return new Rectangle(
                nameWidth + 8,
                HeaderHeight + (index * RowHeight) + 11,
                Math.Max(100, remarkWidth - 16),
                RowHeight - 22);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(BackColor);
            UiDrawing.ConfigureQuality(eventArgs.Graphics);
            DrawHeader(eventArgs.Graphics);
            for (int index = 0; index < items.Count; index++)
            {
                DrawRow(eventArgs.Graphics, items[index], index);
            }
        }

        protected override void OnMouseMove(MouseEventArgs eventArgs)
        {
            base.OnMouseMove(eventArgs);
            int next = HitTest(eventArgs.Location);
            if (next != hoveredIndex)
            {
                hoveredIndex = next;
                Cursor = next >= 0 ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs eventArgs)
        {
            base.OnMouseLeave(eventArgs);
            hoveredIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs eventArgs)
        {
            base.OnMouseDown(eventArgs);
            if (eventArgs.Button != MouseButtons.Left)
            {
                return;
            }

            Focus();
            int index = HitTest(eventArgs.Location);
            if (index >= 0)
            {
                SelectIndex(index, true);
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs eventArgs)
        {
            base.OnMouseDoubleClick(eventArgs);
            int index = HitTest(eventArgs.Location);
            if (index >= 0 && ItemDoubleClicked != null)
            {
                ItemDoubleClicked(this, new AppleBootListEventArgs(items[index], index));
            }
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Up || keyData == Keys.Down || keyData == Keys.Enter)
            {
                return true;
            }

            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs eventArgs)
        {
            if (eventArgs.KeyCode == Keys.Up)
            {
                SelectIndex(Math.Max(0, selectedIndex - 1), true);
                eventArgs.Handled = true;
                return;
            }

            if (eventArgs.KeyCode == Keys.Down)
            {
                SelectIndex(Math.Min(items.Count - 1, selectedIndex + 1), true);
                eventArgs.Handled = true;
                return;
            }

            if (eventArgs.KeyCode == Keys.Enter && selectedIndex >= 0 && ItemDoubleClicked != null)
            {
                ItemDoubleClicked(this, new AppleBootListEventArgs(items[selectedIndex], selectedIndex));
                eventArgs.Handled = true;
                return;
            }

            base.OnKeyDown(eventArgs);
        }

        private void SelectIndex(int index, bool raiseEvent)
        {
            if (index < 0 || index >= items.Count)
            {
                return;
            }

            selectedIndex = index;
            Invalidate();
            if (raiseEvent && ItemSelected != null)
            {
                ItemSelected(this, new AppleBootListEventArgs(items[index], index));
            }
        }

        private int HitTest(Point point)
        {
            int index = (point.Y - HeaderHeight) / RowHeight;
            return point.Y >= HeaderHeight && index >= 0 && index < items.Count ? index : -1;
        }

        private void DrawHeader(Graphics graphics)
        {
            using (var font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point))
            {
                TextRenderer.DrawText(graphics, "系统", font,
                    new Rectangle(12, 0, (int)(Width * 0.48) - 12, HeaderHeight),
                    UiTheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(graphics, "用途", font,
                    new Rectangle((int)(Width * 0.48), 0, (int)(Width * 0.28), HeaderHeight),
                    UiTheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(graphics, "状态", font,
                    new Rectangle((int)(Width * 0.76), 0, (int)(Width * 0.24) - 12, HeaderHeight),
                    UiTheme.Muted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void DrawRow(Graphics graphics, AppleBootListItem item, int index)
        {
            var rowBounds = new RectangleF(2F, HeaderHeight + (index * RowHeight) + 4F, Width - 4F, RowHeight - 8F);
            if (index == selectedIndex || index == hoveredIndex)
            {
                Color fill = index == selectedIndex ? UiTheme.SelectionStrong : UiTheme.Hover;
                using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(rowBounds, 10F))
                using (var brush = new SolidBrush(fill))
                {
                    graphics.FillPath(brush, path);
                }
            }

            int textTop = (int)rowBounds.Top;
            using (var nameFont = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point))
            using (var metaFont = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point))
            {
                TextRenderer.DrawText(graphics, item.Name ?? string.Empty, nameFont,
                    new Rectangle(14, textTop, (int)(Width * 0.46) - 14, (int)rowBounds.Height),
                    UiTheme.Ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

                Rectangle remarkBounds = GetRemarkBounds(index);
                string remark = string.IsNullOrWhiteSpace(item.Remark) ? "未设置" : item.Remark;
                Size remarkSize = TextRenderer.MeasureText(remark, metaFont, Size.Empty, TextFormatFlags.NoPadding);
                int pillWidth = Math.Min(remarkBounds.Width, remarkSize.Width + 20);
                var pillBounds = new RectangleF(remarkBounds.Left, remarkBounds.Top, pillWidth, remarkBounds.Height);
                using (GraphicsPath pillPath = UiDrawing.CreateRoundedRectangle(pillBounds, 9F))
                using (var pillBrush = new SolidBrush(string.IsNullOrWhiteSpace(item.Remark)
                    ? UiTheme.Disabled
                    : UiTheme.AccentSoft))
                {
                    graphics.FillPath(pillBrush, pillPath);
                }
                TextRenderer.DrawText(graphics, remark, metaFont,
                    Rectangle.Round(pillBounds), string.IsNullOrWhiteSpace(item.Remark) ? UiTheme.Muted : UiTheme.Accent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

                int dotX = (int)(Width * 0.78);
                int dotY = textTop + ((int)rowBounds.Height / 2) - 3;
                using (var dotBrush = new SolidBrush(item.IsDefault ? UiTheme.Success : Color.FromArgb(148, 163, 184)))
                {
                    graphics.FillEllipse(dotBrush, dotX, dotY, 7, 7);
                }
                TextRenderer.DrawText(graphics, item.Status ?? string.Empty, metaFont,
                    new Rectangle(dotX + 13, textTop, Width - dotX - 22, (int)rowBounds.Height),
                    item.IsDefault ? UiTheme.Success : UiTheme.Muted,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }
    }

    internal sealed class HighQualityImageControl : Control
    {
        private Image image;

        public HighQualityImageControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);
        }

        public Image Image
        {
            get { return image; }
            set
            {
                image = value;
                Invalidate();
            }
        }

        protected override void OnPaintBackground(PaintEventArgs eventArgs)
        {
            eventArgs.Graphics.Clear(BackColor);
        }

        protected override void OnPaint(PaintEventArgs eventArgs)
        {
            if (image == null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            eventArgs.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            eventArgs.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            eventArgs.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            float imageAspect = image.Width / (float)image.Height;
            float controlAspect = ClientSize.Width / (float)ClientSize.Height;
            Rectangle destination;
            if (imageAspect > controlAspect)
            {
                int height = (int)Math.Round(ClientSize.Width / imageAspect);
                destination = new Rectangle(0, (ClientSize.Height - height) / 2, ClientSize.Width, height);
            }
            else
            {
                int width = (int)Math.Round(ClientSize.Height * imageAspect);
                destination = new Rectangle((ClientSize.Width - width) / 2, 0, width, ClientSize.Height);
            }

            eventArgs.Graphics.DrawImage(image, destination);
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
        private int animationToken;
        private string previousText = string.Empty;
        private string targetText = string.Empty;
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

            if (newText == targetText)
            {
                return;
            }

            if (!IsHandleCreated || !UiMotion.IsEnabled || string.IsNullOrEmpty(targetText))
            {
                UiMotion.Stop(animationToken);
                animationToken = 0;
                previousText = string.Empty;
                targetText = newText;
                animationProgress = 1F;
                Invalidate();
                return;
            }

            previousText = targetText;
            targetText = newText;
            animationProgress = 0F;
            UiMotion.Stop(animationToken);
            animationToken = UiMotion.Start(
                delegate(float progress)
                {
                    animationProgress = progress;
                    if (!IsDisposed)
                    {
                        Invalidate();
                    }
                },
                UiTheme.StateMotionDuration,
                delegate
                {
                    animationToken = 0;
                    previousText = string.Empty;
                    animationProgress = 1F;
                    if (!IsDisposed)
                    {
                        Invalidate();
                    }
                },
                UiMotion.EaseOutCubic);
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

            float eased = animationProgress;
            DrawText(
                eventArgs.Graphics,
                previousText,
                UiMotion.Blend(ForeColor, backdropColor, eased),
                -(int)Math.Round(eased * 4F));
            DrawText(
                eventArgs.Graphics,
                targetText,
                UiMotion.Blend(backdropColor, ForeColor, eased),
                (int)Math.Round((1F - eased) * 4F));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UiMotion.Stop(animationToken);
                animationToken = 0;
            }

            base.Dispose(disposing);
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

            animationTimer = new Timer { Interval = UiTheme.MotionFrameInterval };
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
            float easedProgress = UiMotion.EaseOutCubic(linearProgress);
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
            selectionAnimationTimer = new Timer { Interval = UiTheme.MotionFrameInterval };
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
            float easedProgress = UiMotion.EaseOutCubic(linearProgress);
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
