using System.Drawing;
using System.Windows.Forms;
using AntPanel = AntdUI.Panel;
using AntTag = AntdUI.Tag;

namespace SysSwitch
{
    internal sealed class ApplicationDialog : StyledDialogForm
    {
        public ApplicationDialog(
            Form owner,
            string title,
            string message,
            string actionText,
            bool requiresConfirmation,
            DialogKind kind)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(530, 304);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = owner == null ? null : owner.Icon;

            AntTag kindTag = CreateKindTag(kind);
            kindTag.Location = new Point(24, 23);

            var titleLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(132, 24),
                Size = new Size(374, 32),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var contentPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
                Location = new Point(24, 72),
                Size = new Size(482, 156)
            };
            var messageBox = new TextBox
            {
                BackColor = UiTheme.Surface,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 18),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(446, 120),
                TabStop = false,
                Text = message,
                WordWrap = true
            };
            contentPanel.Controls.Add(messageBox);

            AntdUI.Button cancelButton = null;
            if (requiresConfirmation)
            {
                cancelButton = UiFactory.CreateButton("取消", 108, false);
                cancelButton.Location = new Point(274, 248);
                cancelButton.Size = new Size(108, 40);
                cancelButton.DialogResult = DialogResult.Cancel;
                Controls.Add(cancelButton);
            }

            AntdUI.Button actionButton = UiFactory.CreateButton(actionText, 112, true);
            actionButton.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            actionButton.Location = new Point(requiresConfirmation ? 394 : 394, 246);
            actionButton.Size = new Size(112, 44);
            actionButton.AccessibleName = actionText;
            actionButton.DialogResult = DialogResult.OK;

            Controls.Add(kindTag);
            Controls.Add(titleLabel);
            Controls.Add(contentPanel);
            Controls.Add(actionButton);
            if (cancelButton != null)
            {
                CancelButton = cancelButton;
                Shown += delegate { cancelButton.Focus(); };
            }
            else
            {
                AcceptButton = actionButton;
            }

            CompleteDialogLayout();
        }

        private static AntTag CreateKindTag(DialogKind kind)
        {
            var tag = new AntTag
            {
                AutoSize = false,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                Radius = UiTheme.BadgeCornerRadius,
                Size = new Size(96, 34),
                TextAlign = ContentAlignment.MiddleCenter
            };

            if (kind == DialogKind.Error)
            {
                tag.Text = "操作失败";
                tag.BackColor = UiTheme.WarningSoft;
                tag.ForeColor = UiTheme.Warning;
                tag.Type = AntdUI.TTypeMini.Error;
            }
            else if (kind == DialogKind.Warning)
            {
                tag.Text = "需要确认";
                tag.BackColor = UiTheme.WarningSoft;
                tag.ForeColor = UiTheme.Warning;
                tag.Type = AntdUI.TTypeMini.Warn;
            }
            else
            {
                tag.Text = "操作提示";
                tag.BackColor = UiTheme.AccentSoft;
                tag.ForeColor = UiTheme.Accent;
                tag.Type = AntdUI.TTypeMini.Primary;
            }

            return tag;
        }
    }

    internal enum DialogKind
    {
        Info,
        Warning,
        Error
    }
}
