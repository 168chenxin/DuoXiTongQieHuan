using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class RemarkDialog : StyledDialogForm
    {
        private readonly AntdUI.Input remarkInput;

        public RemarkDialog(string systemName, string initialRemark)
        {
            Text = "编辑启动项备注";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 230);
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
                Size = new Size(428, 158)
            };

            var titleLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 14),
                Size = new Size(388, 22),
                Text = "为“" + systemName + "”设置备注"
            };

            var hintLabel = new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 40),
                Text = "例如：工作、游戏、测试。留空可清除备注。"
            };

            remarkInput = new AntdUI.Input
            {
                AccessibleName = "启动项备注",
                AccessibleDescription = "输入此 Windows 启动项的用途备注",
                BackColor = UiTheme.Surface,
                BorderActive = UiTheme.Primary,
                BorderColor = UiTheme.Border,
                BorderHover = UiTheme.Secondary,
                BorderWidth = 1F,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(18, 76),
                MaxLength = 48,
                Radius = UiTheme.ControlCornerRadius,
                Size = new Size(388, 42),
                Text = initialRemark ?? string.Empty
            };

            surface.Controls.Add(titleLabel);
            surface.Controls.Add(hintLabel);
            surface.Controls.Add(remarkInput);

            AntdUI.Button cancelButton = UiFactory.CreateButton("取消", 104, false);
            cancelButton.Location = new Point(220, 186);
            cancelButton.Size = new Size(104, 36);
            cancelButton.DialogResult = DialogResult.Cancel;

            AntdUI.Button saveButton = UiFactory.CreateButton("保存备注", 104, true);
            saveButton.Location = new Point(340, 186);
            saveButton.Size = new Size(104, 36);
            saveButton.DialogResult = DialogResult.OK;

            Controls.Add(surface);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Shown += delegate
            {
                remarkInput.Focus();
                remarkInput.SelectAll();
            };
            CompleteDialogLayout();
        }

        public string Remark
        {
            get { return remarkInput.Text.Trim(); }
        }
    }
}
