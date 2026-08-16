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

        public static readonly Color Canvas = Color.FromArgb(240, 245, 250);
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);
        public static readonly Color Header = Color.FromArgb(247, 250, 252);
        public static readonly Color Vibrancy = Color.FromArgb(244, 248, 252);
        public static readonly Color Inspector = Color.FromArgb(246, 248, 251);
        public static readonly Color BannerStart = Color.FromArgb(232, 242, 255);
        public static readonly Color BannerEnd = Color.FromArgb(248, 251, 255);
        public static readonly Color HeaderMuted = Color.FromArgb(71, 85, 105);
        public static readonly Color Ink = Color.FromArgb(30, 41, 59);
        public static readonly Color Muted = Color.FromArgb(71, 85, 105);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color Brand = Color.FromArgb(59, 130, 246);
        public static readonly Color Secondary = Color.FromArgb(96, 165, 250);
        public static readonly Color Primary = Color.FromArgb(59, 130, 246);
        public static readonly Color PrimaryHover = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryPressed = Color.FromArgb(29, 78, 216);
        public static readonly Color Selection = Color.FromArgb(239, 246, 255);
        public static readonly Color SelectionStrong = Color.FromArgb(219, 234, 254);
        public static readonly Color Hover = Color.FromArgb(247, 250, 252);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentSoft = Color.FromArgb(239, 246, 255);
        public static readonly Color Success = Color.FromArgb(22, 163, 74);
        public static readonly Color SuccessSoft = Color.FromArgb(240, 253, 244);
        public static readonly Color Warning = Color.FromArgb(190, 73, 45);
        public static readonly Color WarningSoft = Color.FromArgb(255, 247, 237);
        public static readonly Color FirmwareAction = Color.FromArgb(180, 83, 9);
        public static readonly Color FirmwareActionHover = Color.FromArgb(146, 64, 14);
        public static readonly Color FirmwareActionPressed = Color.FromArgb(120, 53, 15);
        public static readonly Color Disabled = Color.FromArgb(241, 245, 249);
        public static readonly Color DisabledText = Color.FromArgb(100, 116, 139);
    }
}
