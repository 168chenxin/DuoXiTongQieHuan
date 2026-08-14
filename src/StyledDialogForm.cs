using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal class StyledDialogForm : Form
    {
        private const int ChromeHeight = 42;
        private const int WindowCornerRadius = 12;
        private const int WmNcLButtonDown = 0x00A1;
        private const int HtCaption = 0x0002;
        private const int DwmWindowCornerPreference = 33;
        private const int DwmWindowCornerRound = 2;
        private readonly DialogChrome chrome;
        private bool layoutCompleted;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr window,
            int attribute,
            ref int value,
            int valueSize);

        protected StyledDialogForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            Padding = new Padding(1);
            DoubleBuffered = true;

            chrome = new DialogChrome();
            chrome.CloseRequested += delegate
            {
                if (Modal)
                {
                    DialogResult = DialogResult.Cancel;
                }
                else
                {
                    Close();
                }
            };
            chrome.DragRequested += delegate
            {
                ReleaseCapture();
                SendMessage(Handle, WmNcLButtonDown, new IntPtr(HtCaption), IntPtr.Zero);
            };
            Controls.Add(chrome);
        }

        protected void CompleteDialogLayout()
        {
            if (layoutCompleted)
            {
                return;
            }

            layoutCompleted = true;
            SuspendLayout();
            foreach (Control control in Controls)
            {
                if (!ReferenceEquals(control, chrome))
                {
                    control.Top += ChromeHeight;
                }
            }

            ClientSize = new Size(ClientSize.Width, ClientSize.Height + ChromeHeight);
            chrome.SetBounds(1, 1, ClientSize.Width - 2, ChromeHeight);
            chrome.BringToFront();
            ResumeLayout(false);
            UpdateWindowRegion();
        }

        protected override void OnHandleCreated(EventArgs eventArgs)
        {
            base.OnHandleCreated(eventArgs);
            ApplyNativeWindowCorners();
            UpdateWindowRegion();
        }

        protected override void OnSizeChanged(EventArgs eventArgs)
        {
            base.OnSizeChanged(eventArgs);
            if (chrome != null)
            {
                chrome.Width = Math.Max(0, ClientSize.Width - 2);
            }

            UpdateWindowRegion();
        }

        protected override void OnTextChanged(EventArgs eventArgs)
        {
            base.OnTextChanged(eventArgs);
            if (chrome != null)
            {
                chrome.Text = Text;
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
            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(bounds, WindowCornerRadius * scale))
            using (var pen = new Pen(UiTheme.Border, 1F))
            {
                eventArgs.Graphics.DrawPath(pen, path);
            }
        }

        private void UpdateWindowRegion()
        {
            if (!IsHandleCreated || Width <= 1 || Height <= 1)
            {
                return;
            }

            float scale;
            using (Graphics graphics = CreateGraphics())
            {
                scale = UiDrawing.GetScale(graphics);
            }

            using (GraphicsPath path = UiDrawing.CreateRoundedRectangle(
                new RectangleF(0F, 0F, Width, Height),
                WindowCornerRadius * scale))
            {
                Region oldRegion = Region;
                Region = new Region(path);
                if (oldRegion != null)
                {
                    oldRegion.Dispose();
                }
            }
        }

        private void ApplyNativeWindowCorners()
        {
            try
            {
                int preference = DwmWindowCornerRound;
                DwmSetWindowAttribute(
                    Handle,
                    DwmWindowCornerPreference,
                    ref preference,
                    sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private sealed class DialogChrome : Control
        {
            private const int CloseWidth = 46;
            private bool closeHovered;
            private bool closePressed;

            public DialogChrome()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw |
                    ControlStyles.UserPaint,
                    true);
                BackColor = UiTheme.Header;
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
                ForeColor = UiTheme.Ink;
                AccessibleRole = AccessibleRole.TitleBar;
            }

            public event EventHandler CloseRequested;
            public event EventHandler DragRequested;

            protected override void OnMouseMove(MouseEventArgs eventArgs)
            {
                base.OnMouseMove(eventArgs);
                bool hovered = GetCloseBounds().Contains(eventArgs.Location);
                if (hovered != closeHovered)
                {
                    closeHovered = hovered;
                    Invalidate(GetCloseBounds());
                }
            }

            protected override void OnMouseLeave(EventArgs eventArgs)
            {
                base.OnMouseLeave(eventArgs);
                closeHovered = false;
                closePressed = false;
                Invalidate();
            }

            protected override void OnMouseDown(MouseEventArgs eventArgs)
            {
                base.OnMouseDown(eventArgs);
                if (eventArgs.Button != MouseButtons.Left)
                {
                    return;
                }

                if (GetCloseBounds().Contains(eventArgs.Location))
                {
                    closePressed = true;
                    Invalidate(GetCloseBounds());
                }
                else if (DragRequested != null)
                {
                    DragRequested(this, EventArgs.Empty);
                }
            }

            protected override void OnMouseUp(MouseEventArgs eventArgs)
            {
                bool shouldClose = closePressed && GetCloseBounds().Contains(eventArgs.Location);
                closePressed = false;
                base.OnMouseUp(eventArgs);
                Invalidate(GetCloseBounds());
                if (shouldClose && CloseRequested != null)
                {
                    CloseRequested(this, EventArgs.Empty);
                }
            }

            protected override void OnPaint(PaintEventArgs eventArgs)
            {
                eventArgs.Graphics.Clear(UiTheme.Header);
                Rectangle closeBounds = GetCloseBounds();
                if (closeHovered || closePressed)
                {
                    using (var brush = new SolidBrush(closePressed ? UiTheme.SelectionStrong : UiTheme.Hover))
                    {
                        eventArgs.Graphics.FillRectangle(brush, closeBounds);
                    }
                }

                TextRenderer.DrawText(
                    eventArgs.Graphics,
                    Text,
                    Font,
                    new Rectangle(16, 0, Math.Max(0, Width - CloseWidth - 24), Height),
                    ForeColor,
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.Left |
                    TextFormatFlags.NoPadding |
                    TextFormatFlags.SingleLine |
                    TextFormatFlags.VerticalCenter);

                UiDrawing.ConfigureQuality(eventArgs.Graphics);
                int centerX = closeBounds.Left + (closeBounds.Width / 2);
                int centerY = closeBounds.Top + (closeBounds.Height / 2);
                using (var pen = new Pen(UiTheme.Muted, 1.4F))
                {
                    eventArgs.Graphics.DrawLine(pen, centerX - 5, centerY - 5, centerX + 5, centerY + 5);
                    eventArgs.Graphics.DrawLine(pen, centerX + 5, centerY - 5, centerX - 5, centerY + 5);
                }

                using (var dividerPen = new Pen(UiTheme.Border, 1F))
                {
                    eventArgs.Graphics.DrawLine(dividerPen, 0, Height - 1, Width, Height - 1);
                }
            }

            private Rectangle GetCloseBounds()
            {
                return new Rectangle(Math.Max(0, Width - CloseWidth), 0, CloseWidth, Height - 1);
            }
        }
    }
}
