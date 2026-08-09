using System.Drawing;

namespace DualBootSwitcher
{
    internal static class UiTheme
    {
        public const int SurfaceCornerRadius = 8;
        public const int ControlCornerRadius = 10;
        public const int BadgeCornerRadius = 9;
        public const int MotionFrameInterval = 16;
        public const int PressMotionDuration = 110;
        public const int StateMotionDuration = 220;

        public static readonly Color Canvas = Color.FromArgb(246, 248, 252);
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);
        public static readonly Color Header = Color.FromArgb(249, 250, 255);
        public static readonly Color HeaderMuted = Color.FromArgb(100, 116, 139);
        public static readonly Color Ink = Color.FromArgb(28, 37, 54);
        public static readonly Color Muted = Color.FromArgb(91, 104, 124);
        public static readonly Color Border = Color.FromArgb(220, 226, 237);
        public static readonly Color Brand = Color.FromArgb(99, 102, 241);
        public static readonly Color Secondary = Color.FromArgb(129, 140, 248);
        public static readonly Color Primary = Color.FromArgb(91, 94, 232);
        public static readonly Color PrimaryHover = Color.FromArgb(79, 70, 229);
        public static readonly Color PrimaryPressed = Color.FromArgb(67, 56, 202);
        public static readonly Color Selection = Color.FromArgb(237, 239, 255);
        public static readonly Color SelectionStrong = Color.FromArgb(218, 222, 255);
        public static readonly Color Hover = Color.FromArgb(246, 247, 255);
        public static readonly Color Accent = Color.FromArgb(79, 70, 229);
        public static readonly Color AccentSoft = Color.FromArgb(237, 239, 255);
        public static readonly Color Success = Color.FromArgb(22, 122, 84);
        public static readonly Color SuccessSoft = Color.FromArgb(232, 246, 239);
        public static readonly Color Warning = Color.FromArgb(180, 83, 9);
        public static readonly Color WarningSoft = Color.FromArgb(255, 247, 230);
        public static readonly Color Disabled = Color.FromArgb(241, 245, 249);
        public static readonly Color DisabledText = Color.FromArgb(100, 116, 139);
    }
}
