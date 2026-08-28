using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace SysSwitch
{
    internal static class UiMotionExtensions
    {
        private const string OverlayName = "__UiMotionOverlay";
        private static readonly ConditionalWeakTable<Control, PaintOffsetState> OffsetStates =
            new ConditionalWeakTable<Control, PaintOffsetState>();

        public static PointF GetPaintOffset(this Control control)
        {
            if (control == null)
            {
                return PointF.Empty;
            }

            PaintOffsetState state;
            return OffsetStates.TryGetValue(control, out state) ? state.Offset : PointF.Empty;
        }

        public static int AnimatePaintOffset(
            this Control control,
            float targetX,
            float targetY,
            int durationMilliseconds)
        {
            if (control == null)
            {
                throw new ArgumentNullException("control");
            }

            PaintOffsetState state = OffsetStates.GetOrCreateValue(control);
            if (state.DisposedHandler == null)
            {
                state.DisposedHandler = delegate
                {
                    UiMotion.Stop(state.AnimationToken);
                    state.AnimationToken = 0;
                    OffsetStates.Remove(control);
                };
                control.Disposed += state.DisposedHandler;
            }
            UiMotion.Stop(state.AnimationToken);
            PointF start = state.Offset;
            int token = UiMotion.Start(
                delegate(float progress)
                {
                    state.Offset = new PointF(
                        UiMotion.Lerp(start.X, targetX, progress),
                        UiMotion.Lerp(start.Y, targetY, progress));
                    if (!control.IsDisposed)
                    {
                        control.Invalidate();
                    }
                },
                durationMilliseconds,
                delegate { state.AnimationToken = 0; },
                UiMotion.EaseOutQuart);
            state.AnimationToken = token;
            return token;
        }

        public static int AnimateOverlayColor(
            this Control parent,
            Color targetColor,
            int durationMilliseconds,
            bool keepOverlay)
        {
            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            parent.ClearOverlayAnimations();
            var overlay = new MotionOverlay
            {
                Bounds = parent.ClientRectangle,
                Name = OverlayName,
                OverlayColor = Color.FromArgb(0, targetColor),
                TabStop = false
            };
            EventHandler resizeHandler = delegate
            {
                if (!overlay.IsDisposed)
                {
                    overlay.Bounds = parent.ClientRectangle;
                }
            };
            EventHandler disposedHandler = null;
            disposedHandler = delegate
            {
                UiMotion.Stop(overlay.AnimationToken);
                parent.SizeChanged -= resizeHandler;
                parent.Disposed -= disposedHandler;
                if (!overlay.IsDisposed)
                {
                    overlay.Dispose();
                }
            };
            overlay.ResizeHandler = resizeHandler;
            overlay.ParentDisposedHandler = disposedHandler;
            parent.SizeChanged += resizeHandler;
            parent.Disposed += disposedHandler;
            parent.Controls.Add(overlay);
            overlay.BringToFront();

            int token = UiMotion.Start(
                delegate(float progress)
                {
                    if (!overlay.IsDisposed)
                    {
                        overlay.OverlayColor = UiMotion.Blend(
                            Color.FromArgb(0, targetColor),
                            targetColor,
                            progress);
                    }
                },
                durationMilliseconds,
                delegate
                {
                    overlay.AnimationToken = 0;
                    if (!keepOverlay)
                    {
                        RemoveOverlay(parent, overlay, resizeHandler, disposedHandler);
                    }
                },
                UiMotion.EaseOutQuart);
            overlay.AnimationToken = token;
            return token;
        }

        public static int AnimateOverlayColor(
            this Control parent,
            Color targetColor,
            int durationMilliseconds)
        {
            return AnimateOverlayColor(parent, targetColor, durationMilliseconds, false);
        }

        public static void ClearOverlayAnimations(this Control parent)
        {
            if (parent == null || parent.IsDisposed)
            {
                return;
            }

            for (int index = parent.Controls.Count - 1; index >= 0; index--)
            {
                MotionOverlay overlay = parent.Controls[index] as MotionOverlay;
                if (overlay == null || overlay.Name != OverlayName)
                {
                    continue;
                }

                UiMotion.Stop(overlay.AnimationToken);
                overlay.DetachHandlers();
                parent.Controls.RemoveAt(index);
                overlay.Dispose();
            }
        }

        private static void RemoveOverlay(
            Control parent,
            MotionOverlay overlay,
            EventHandler resizeHandler,
            EventHandler disposedHandler)
        {
            parent.SizeChanged -= resizeHandler;
            parent.Disposed -= disposedHandler;
            overlay.DetachHandlers();
            if (!parent.IsDisposed && parent.Controls.Contains(overlay))
            {
                parent.Controls.Remove(overlay);
            }

            if (!overlay.IsDisposed)
            {
                overlay.Dispose();
            }
        }

        private sealed class PaintOffsetState
        {
            public PointF Offset;
            public int AnimationToken;
            public EventHandler DisposedHandler;
        }

        private sealed class MotionOverlay : Control
        {
            private const int WindowMessageNcHitTest = 0x0084;
            private const int HitTestTransparent = -1;
            private Color overlayColor;

            public MotionOverlay()
            {
                SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.SupportsTransparentBackColor |
                    ControlStyles.UserPaint,
                    true);
                BackColor = Color.Transparent;
                Enabled = false;
            }

            public int AnimationToken { get; set; }

            public EventHandler ResizeHandler { get; set; }

            public EventHandler ParentDisposedHandler { get; set; }

            public Color OverlayColor
            {
                get { return overlayColor; }
                set
                {
                    overlayColor = value;
                    Invalidate();
                }
            }

            protected override void OnPaintBackground(PaintEventArgs eventArgs)
            {
            }

            protected override void OnPaint(PaintEventArgs eventArgs)
            {
                using (var brush = new SolidBrush(overlayColor))
                {
                    eventArgs.Graphics.FillRectangle(brush, ClientRectangle);
                }
            }

            protected override void WndProc(ref Message message)
            {
                if (message.Msg == WindowMessageNcHitTest)
                {
                    message.Result = new IntPtr(HitTestTransparent);
                    return;
                }

                base.WndProc(ref message);
            }

            public void DetachHandlers()
            {
                Control parent = Parent;
                if (parent != null)
                {
                    if (ResizeHandler != null)
                    {
                        parent.SizeChanged -= ResizeHandler;
                    }
                    if (ParentDisposedHandler != null)
                    {
                        parent.Disposed -= ParentDisposedHandler;
                    }
                }

                ResizeHandler = null;
                ParentDisposedHandler = null;
            }
        }
    }
}
