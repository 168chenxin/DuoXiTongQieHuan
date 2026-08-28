using System.Drawing;

namespace SysSwitch
{
    internal static class UiTheme
    {
        public const int SurfaceCornerRadius = 12;
        public const int WorkspaceCornerRadius = 12;
        public const int InspectorCornerRadius = 12;
        public const int ControlCornerRadius = 8;
        public const int BadgeCornerRadius = 8;
        public const int MotionFrameInterval = 16;
        public const int PressMotionDuration = 90;
        public const int StateMotionDuration = 160;
        public const int DashboardHeaderHeight = 58;
        public const int DashboardSummaryBandHeight = 88;
        public const int DashboardStackBreakpoint = 780;

        public static readonly Color Canvas = Color.FromArgb(248, 250, 252);
        public static readonly Color Surface = Color.FromArgb(255, 255, 255);
        public static readonly Color Header = Color.FromArgb(255, 255, 255);
        public static readonly Color Vibrancy = Color.FromArgb(248, 250, 252);
        public static readonly Color Inspector = Color.FromArgb(248, 250, 252);
        public static readonly Color HeaderMuted = Color.FromArgb(71, 85, 105);
        public static readonly Color Ink = Color.FromArgb(15, 23, 42);
        public static readonly Color Muted = Color.FromArgb(71, 85, 105);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);
        public static readonly Color Brand = Color.FromArgb(37, 99, 235);
        public static readonly Color Secondary = Color.FromArgb(96, 165, 250);
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color PrimaryHover = Color.FromArgb(29, 78, 216);
        public static readonly Color PrimaryPressed = Color.FromArgb(30, 64, 175);
        public static readonly Color Selection = Color.FromArgb(239, 246, 255);
        public static readonly Color SelectionStrong = Color.FromArgb(219, 234, 254);
        public static readonly Color Hover = Color.FromArgb(248, 250, 252);
        public static readonly Color Accent = Color.FromArgb(37, 99, 235);
        public static readonly Color AccentSoft = Color.FromArgb(239, 246, 255);
        public static readonly Color Success = Color.FromArgb(22, 163, 74);
        public static readonly Color SuccessSoft = Color.FromArgb(240, 253, 244);
        public static readonly Color Warning = Color.FromArgb(190, 73, 45);
        public static readonly Color WarningSoft = Color.FromArgb(255, 247, 237);
        public static readonly Color Disabled = Color.FromArgb(241, 245, 249);
        public static readonly Color DisabledText = Color.FromArgb(148, 163, 184);
    }
}
