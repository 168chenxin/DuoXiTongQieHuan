using System.Drawing;
using System.Windows.Forms;

namespace SysSwitch
{
    internal sealed class RenameDialog : StyledDialogForm
    {
        private readonly AntdUI.Input nameInput;

        public RenameDialog(string systemName, string initialName, bool canRestore)
        {
            Text = "重命名启动项";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 258);
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
                Location = new Point(18, 16),
                Size = new Size(464, 158)
            };
            surface.Controls.Add(new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 14),
                Size = new Size(428, 22),
                Text = "为“" + systemName + "”修改启动菜单名称"
            });
            surface.Controls.Add(new Label
            {
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 40),
                Size = new Size(428, 30),
                Text = "仅修改 Windows BCD 中的显示名称，不会改变分区、标识符或启动逻辑。"
            });
            nameInput = new AntdUI.Input
            {
                AccessibleName = "启动项名称",
                AccessibleDescription = "输入 Windows 启动菜单显示的名称",
                BackColor = UiTheme.Surface,
                BorderActive = UiTheme.Primary,
                BorderColor = UiTheme.Border,
                BorderHover = UiTheme.Secondary,
                BorderWidth = 1F,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                Location = new Point(18, 82),
                MaxLength = 80,
                Radius = UiTheme.ControlCornerRadius,
                Size = new Size(428, 42),
                Text = initialName ?? string.Empty
            };
            surface.Controls.Add(nameInput);

            var cancelButton = UiFactory.CreateButton("取消", 100, false);
            cancelButton.Location = new Point(198, 194);
            cancelButton.Size = new Size(100, 36);
            cancelButton.DialogResult = DialogResult.Cancel;
            var saveButton = UiFactory.CreateButton("保存名称", 108, true);
            saveButton.Location = new Point(310, 194);
            saveButton.Size = new Size(108, 36);
            saveButton.DialogResult = DialogResult.OK;
            Controls.Add(surface);
            Controls.Add(cancelButton);
            Controls.Add(saveButton);
            if (canRestore)
            {
                var restoreButton = UiFactory.CreateButton("恢复原名称", 112, false);
                restoreButton.Location = new Point(18, 194);
                restoreButton.Size = new Size(112, 36);
                restoreButton.Click += delegate
                {
                    RestoreRequested = true;
                    DialogResult = DialogResult.Retry;
                };
                Controls.Add(restoreButton);
            }
            AcceptButton = saveButton;
            CancelButton = cancelButton;
            Shown += delegate
            {
                nameInput.Focus();
                nameInput.SelectAll();
            };
            CompleteDialogLayout();
        }

        public string NameValue
        {
            get { return nameInput.Text.Trim(); }
        }

        public bool RestoreRequested { get; private set; }
    }
}
