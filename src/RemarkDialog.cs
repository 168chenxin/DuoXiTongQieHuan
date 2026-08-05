using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class RemarkDialog : Form
    {
        private readonly TextBox remarkTextBox;

        public RemarkDialog(string systemName, string initialRemark)
        {
            Text = "编辑启动项备注";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = new Size(420, 196);
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
                Size = new Size(388, 126)
            };

            var titleLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 14),
                Size = new Size(350, 22),
                Text = "为“" + systemName + "”设置备注"
            };

            var hintLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 40),
                Text = "例如：工作、游戏、测试。留空可清除备注。"
            };

            remarkTextBox = new TextBox
            {
                AccessibleName = "启动项备注",
                AccessibleDescription = "输入此 Windows 启动项的用途备注",
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(18, 70),
                MaxLength = 48,
                Size = new Size(350, 28),
                Text = initialRemark ?? string.Empty
            };

            surface.Controls.Add(titleLabel);
            surface.Controls.Add(hintLabel);
            surface.Controls.Add(remarkTextBox);

            var cancelButton = new AnimatedButton(false)
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(194, 154),
                Size = new Size(102, 36),
                Text = "取消",
                DialogResult = DialogResult.Cancel
            };

            var saveButton = new AnimatedButton(true)
            {
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                Location = new Point(302, 154),
                Size = new Size(102, 36),
                Text = "保存备注",
                DialogResult = DialogResult.OK
            };

            Controls.Add(surface);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += delegate
            {
                remarkTextBox.Focus();
                remarkTextBox.SelectAll();
            };
        }

        public string Remark
        {
            get { return remarkTextBox.Text.Trim(); }
        }
    }
}
