using System.Drawing;

namespace DualBootSwitcher
{
    internal static class UiTheme
    {
        public const int SurfaceCornerRadius = 12;
        public const int WorkspaceCornerRadius = 14;
        public const int InspectorCornerRadius = 14;
        public const int ControlCornerRadius = 10;
        public const int BadgeCornerRadius = 9;
        public const int MotionFrameInterval = 16;
        public const int PressMotionDuration = 90;
        public const int StateMotionDuration = 160;

        public static readonly Color Canvas = Color.FromArgb(238, 241, 244);
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);
        public static readonly Color Header = Color.FromArgb(247, 249, 250);
        public static readonly Color Vibrancy = Color.FromArgb(244, 248, 252);
        public static readonly Color Inspector = Color.FromArgb(246, 248, 251);
        public static readonly Color HeaderMuted = Color.FromArgb(71, 85, 105);
        public static readonly Color Ink = Color.FromArgb(23, 32, 42);
        public static readonly Color Muted = Color.FromArgb(102, 115, 124);
        public static readonly Color Border = Color.FromArgb(219, 228, 231);
        public static readonly Color Brand = Color.FromArgb(22, 140, 104);
        public static readonly Color Secondary = Color.FromArgb(76, 177, 143);
        public static readonly Color Primary = Color.FromArgb(22, 140, 104);
        public static readonly Color PrimaryHover = Color.FromArgb(15, 111, 82);
        public static readonly Color PrimaryPressed = Color.FromArgb(11, 90, 67);
        public static readonly Color Selection = Color.FromArgb(229, 244, 238);
        public static readonly Color SelectionStrong = Color.FromArgb(205, 236, 223);
        public static readonly Color Hover = Color.FromArgb(247, 250, 252);
        public static readonly Color Accent = Color.FromArgb(22, 140, 104);
        public static readonly Color AccentSoft = Color.FromArgb(229, 244, 238);
        public static readonly Color Success = Color.FromArgb(22, 163, 74);
        public static readonly Color SuccessSoft = Color.FromArgb(240, 253, 244);
        public static readonly Color Warning = Color.FromArgb(190, 73, 45);
        public static readonly Color WarningSoft = Color.FromArgb(255, 247, 237);
        public static readonly Color Disabled = Color.FromArgb(241, 245, 249);
        public static readonly Color DisabledText = Color.FromArgb(100, 116, 139);
    }
}
