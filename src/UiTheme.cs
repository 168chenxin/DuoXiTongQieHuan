using System.Drawing;

namespace DualBootSwitcher
{
    internal static class UiTheme
    {
        public const int SurfaceCornerRadius = 8;
        public const int ControlCornerRadius = 10;
        public const int BadgeCornerRadius = 9;
        public const int PressMotionDuration = 100;
        public const int StateMotionDuration = 180;

        public static readonly Color Canvas = Color.FromArgb(248, 250, 252);
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);
        public static readonly Color Header = Color.FromArgb(255, 255, 255);
        public static readonly Color HeaderMuted = Color.FromArgb(100, 116, 139);
        public static readonly Color Ink = Color.FromArgb(30, 41, 59);
        public static readonly Color Muted = Color.FromArgb(100, 116, 139);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color Brand = Color.FromArgb(99, 102, 241);
        public static readonly Color Secondary = Color.FromArgb(129, 140, 248);
        public static readonly Color Primary = Color.FromArgb(98, 101, 240);
        public static readonly Color PrimaryHover = Color.FromArgb(79, 70, 229);
        public static readonly Color PrimaryPressed = Color.FromArgb(67, 56, 202);
        public static readonly Color Selection = Color.FromArgb(224, 231, 255);
        public static readonly Color Accent = Color.FromArgb(79, 70, 229);
        public static readonly Color AccentSoft = Color.FromArgb(224, 231, 255);
        public static readonly Color Disabled = Color.FromArgb(241, 245, 249);
        public static readonly Color DisabledText = Color.FromArgb(100, 116, 139);
    }
}
