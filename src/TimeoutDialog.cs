using System;
using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class TimeoutDialog : Form
    {
        private readonly NumericUpDown timeoutInput;

        public TimeoutDialog(int currentSeconds)
        {
            Text = "修改启动等待时间";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(420, 210);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;

            var surface = new RoundedPanel
            {
                FillColor = UiTheme.Surface,
                CornerRadius = UiTheme.SurfaceCornerRadius,
                Location = new Point(16, 16),
                Size = new Size(388, 140)
            };

            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 14),
                Text = "设置启动菜单等待时间"
            };

            var hintLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 40),
                Text = "电脑开机时，启动菜单会等待你选择系统。"
            };

            var inputLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 78),
                Text = "等待时间"
            };

            timeoutInput = new NumericUpDown
            {
                AccessibleName = "启动菜单等待秒数",
                AccessibleDescription = "输入 0 到 999 秒，0 秒会直接启动默认系统",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point(104, 72),
                Maximum = 999,
                Minimum = 0,
                Size = new Size(118, 28),
                TextAlign = HorizontalAlignment.Center,
                Value = Math.Max(0, Math.Min(999, currentSeconds))
            };

            var secondsLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(230, 78),
                Text = "秒"
            };

            var warningLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 108),
                Text = "0 秒会直接启动默认系统；保存前还会再次确认。"
            };

            surface.Controls.Add(titleLabel);
            surface.Controls.Add(hintLabel);
            surface.Controls.Add(inputLabel);
            surface.Controls.Add(timeoutInput);
            surface.Controls.Add(secondsLabel);
            surface.Controls.Add(warningLabel);

            var cancelButton = new AnimatedButton(false)
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(194, 168),
                Size = new Size(102, 36),
                Text = "取消",
                DialogResult = DialogResult.Cancel
            };

            var continueButton = new AnimatedButton(true)
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point(302, 168),
                Size = new Size(102, 36),
                Text = "继续修改",
                DialogResult = DialogResult.OK
            };

            Controls.Add(surface);
            Controls.Add(cancelButton);
            Controls.Add(continueButton);
            AcceptButton = continueButton;
            CancelButton = cancelButton;

            Shown += delegate
            {
                timeoutInput.Focus();
                timeoutInput.Select(0, timeoutInput.Text.Length);
            };
        }

        public int TimeoutSeconds
        {
            get { return Decimal.ToInt32(timeoutInput.Value); }
        }
    }
}
