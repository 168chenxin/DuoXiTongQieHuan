using System;
using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class TimeoutDialog : StyledDialogForm
    {
        private readonly AntdUI.InputNumber timeoutInput;

        public TimeoutDialog(int currentSeconds)
        {
            Text = "修改启动等待时间";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 246);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;

            var surface = new AntdUI.Panel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
                Location = new Point(16, 16),
                Size = new Size(428, 174)
            };

            var titleLabel = new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 14),
                Text = "设置启动菜单等待时间"
            };

            var hintLabel = new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 40),
                Text = "电脑开机时，启动菜单会等待你选择系统。"
            };

            var inputLabel = new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 78),
                Text = "等待时间"
            };

            timeoutInput = new AntdUI.InputNumber
            {
                AccessibleName = "启动菜单等待秒数",
                AccessibleDescription = "输入 0 到 999 秒，0 秒会直接启动默认系统",
                AlwaysShowControl = true,
                BackColor = UiTheme.Surface,
                BorderActive = UiTheme.Primary,
                BorderColor = UiTheme.Border,
                BorderHover = UiTheme.Secondary,
                BorderWidth = 1F,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point(108, 69),
                Maximum = 999,
                Minimum = 0,
                Radius = UiTheme.ControlCornerRadius,
                ShowControl = true,
                Size = new Size(132, 40),
                TextAlign = HorizontalAlignment.Center,
                Value = Math.Max(0, Math.Min(999, currentSeconds)),
                WheelModifyEnabled = false
            };

            var secondsLabel = new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(250, 81),
                Text = "秒"
            };

            var warningLabel = new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Warning,
                Location = new Point(18, 126),
                Text = "注意：0 秒会跳过系统选择，直接启动默认系统。"
            };

            surface.Controls.Add(titleLabel);
            surface.Controls.Add(hintLabel);
            surface.Controls.Add(inputLabel);
            surface.Controls.Add(timeoutInput);
            surface.Controls.Add(secondsLabel);
            surface.Controls.Add(warningLabel);

            AntdUI.Button cancelButton = UiFactory.CreateButton("取消", 104, false);
            cancelButton.Location = new Point(220, 202);
            cancelButton.Size = new Size(104, 36);
            cancelButton.DialogResult = DialogResult.Cancel;

            AntdUI.Button saveButton = UiFactory.CreateButton("保存修改", 104, true);
            saveButton.Location = new Point(340, 202);
            saveButton.Size = new Size(104, 36);
            saveButton.DialogResult = DialogResult.OK;

            Controls.Add(surface);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += delegate
            {
                timeoutInput.Focus();
                timeoutInput.Select(0, timeoutInput.Text.Length);
            };
            CompleteDialogLayout();
        }

        public int TimeoutSeconds
        {
            get { return Decimal.ToInt32(timeoutInput.Value); }
        }
    }
}
