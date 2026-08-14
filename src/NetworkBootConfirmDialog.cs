using System.Drawing;
using System.Windows.Forms;
using AntPanel = AntdUI.Panel;

namespace DualBootSwitcher
{
    internal sealed class NetworkBootConfirmDialog : StyledDialogForm
    {
        private readonly ToolTip detailToolTip;

        public NetworkBootConfirmDialog(Form owner, FirmwareBootEntry entry)
        {
            Text = "确认启动网维无盘";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(580, 380);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = owner == null ? null : owner.Icon;
            detailToolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true
            };

            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(26, 24),
                Text = "确认启动网维无盘"
            };

            var descriptionLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(27, 55),
                Size = new Size(526, 38),
                Text = "将为下一次启动设置以下 UEFI 网络启动项，然后立即重启电脑。"
            };

            var detailPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
                Location = new Point(26, 104),
                Size = new Size(528, 164)
            };
            AddDetailRow(detailPanel, "启动项", entry.DisplayName, 18);
            AddDetailRow(detailPanel, "网络类型", entry.NetworkType, 60);
            AddDetailRow(detailPanel, "固件 GUID", entry.Identifier, 102);

            var safetyLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Warning,
                Location = new Point(28, 282),
                Size = new Size(526, 20),
                Text = "此操作不会修改 BIOS 的永久启动顺序。"
            };

            AntdUI.Button cancelButton = UiFactory.CreateButton("取消", 112, false);
            cancelButton.Location = new Point(304, 326);
            cancelButton.Size = new Size(112, 40);
            cancelButton.DialogResult = DialogResult.Cancel;

            AntdUI.Button confirmButton = UiFactory.CreateButton("设置并重启", 126, true);
            confirmButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            confirmButton.Location = new Point(428, 323);
            confirmButton.Size = new Size(126, 44);
            confirmButton.AccessibleName = "确认设置网维启动并重启";
            confirmButton.DialogResult = DialogResult.OK;

            Controls.Add(titleLabel);
            Controls.Add(descriptionLabel);
            Controls.Add(detailPanel);
            Controls.Add(safetyLabel);
            Controls.Add(cancelButton);
            Controls.Add(confirmButton);
            CancelButton = cancelButton;
            Shown += delegate { cancelButton.Focus(); };
            CompleteDialogLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                detailToolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        private void AddDetailRow(AntPanel panel, string label, string value, int top)
        {
            var nameLabel = new Label
            {
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, top),
                Size = new Size(94, 25),
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var valueLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(120, top),
                Size = new Size(388, 25),
                Text = value,
                TextAlign = ContentAlignment.MiddleLeft
            };
            detailToolTip.SetToolTip(valueLabel, value);
            panel.Controls.Add(nameLabel);
            panel.Controls.Add(valueLabel);
        }
    }
}
