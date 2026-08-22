using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal static class AntdUiTheme
    {
        public static void Configure()
        {
            AntdUI.Config.Animation = UiMotion.IsEnabled;
            AntdUI.Config.FocusBorderEnabled = true;
            AntdUI.Config.ShadowEnabled = true;

            AntdUI.Style.Set(AntdUI.Colour.Primary, UiTheme.Primary);
            AntdUI.Style.Set(AntdUI.Colour.PrimaryColor, UiTheme.Primary);
            AntdUI.Style.Set(AntdUI.Colour.PrimaryHover, UiTheme.PrimaryHover);
            AntdUI.Style.Set(AntdUI.Colour.PrimaryActive, UiTheme.PrimaryPressed);
            AntdUI.Style.Set(AntdUI.Colour.PrimaryBg, UiTheme.AccentSoft);
            AntdUI.Style.Set(AntdUI.Colour.PrimaryBgHover, UiTheme.SelectionStrong);
            AntdUI.Style.Set(AntdUI.Colour.TextBase, UiTheme.Ink);
            AntdUI.Style.Set(AntdUI.Colour.Text, UiTheme.Ink);
            AntdUI.Style.Set(AntdUI.Colour.TextSecondary, UiTheme.Muted);
            AntdUI.Style.Set(AntdUI.Colour.BgBase, UiTheme.Surface);
            AntdUI.Style.Set(AntdUI.Colour.BgContainer, UiTheme.Surface);
            AntdUI.Style.Set(AntdUI.Colour.BgLayout, UiTheme.Canvas);
            AntdUI.Style.Set(AntdUI.Colour.BorderColor, UiTheme.Border);
            AntdUI.Style.Set(AntdUI.Colour.HoverBg, UiTheme.Hover);
        }
    }

    internal static class UiFactory
    {
        public static AntdUI.Button CreateButton(string text, int width, bool isPrimary)
        {
            return new AntdUI.Button
            {
                AutoEllipsis = true,
                BackActive = UiTheme.PrimaryPressed,
                BackColor = isPrimary ? UiTheme.Primary : UiTheme.Surface,
                BackHover = UiTheme.PrimaryHover,
                BorderWidth = isPrimary ? 0F : 1F,
                DefaultBorderColor = isPrimary ? Color.Transparent : UiTheme.Border,
                Font = new Font(
                    "Segoe UI",
                    9F,
                    isPrimary ? FontStyle.Bold : FontStyle.Regular,
                    GraphicsUnit.Point),
                ForeActive = isPrimary ? Color.White : UiTheme.Ink,
                ForeColor = isPrimary ? Color.White : UiTheme.Ink,
                ForeHover = isPrimary ? Color.White : UiTheme.Ink,
                Radius = UiTheme.ControlCornerRadius,
                Size = new Size(width, 40),
                Text = text,
                Type = isPrimary ? AntdUI.TTypeMini.Primary : AntdUI.TTypeMini.Default,
                WaveSize = UiMotion.IsEnabled ? 4 : 0
            };
        }
    }

    internal static class UiDialogs
    {
        public static DialogResult Confirm(
            Form owner,
            string title,
            string message,
            string confirmText)
        {
            using (var dialog = new ApplicationDialog(
                owner,
                title,
                message,
                confirmText,
                true,
                DialogKind.Warning))
            {
                return dialog.ShowDialog(owner);
            }
        }

        public static void ShowError(Form owner, string title, string message)
        {
            Show(owner, title, message, DialogKind.Error);
        }

        public static void ShowInfo(Form owner, string title, string message)
        {
            Show(owner, title, message, DialogKind.Info);
        }

        private static void Show(Form owner, string title, string message, DialogKind kind)
        {
            using (var dialog = new ApplicationDialog(
                owner,
                title,
                message,
                "知道了",
                false,
                kind))
            {
                dialog.ShowDialog(owner);
            }
        }
    }
}
