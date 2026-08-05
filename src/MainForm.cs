using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class MainForm : Form
    {
        private readonly ListView bootEntriesList;
        private readonly Label defaultSystemLabel;
        private readonly Button setDefaultButton;
        private readonly Button setDefaultAndRestartButton;
        public MainForm()
        {
            Text = "双系统快速切换";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(700, 390);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(32, 41, 54),
                Location = new Point(24, 22),
                Text = "选择下次启动的系统"
            };

            var descriptionLabel = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(90, 100, 115),
                Location = new Point(27, 60),
                Text = "选择目标系统后，可直接设为默认并重启。"
            };

            defaultSystemLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(39, 79, 150),
                Location = new Point(27, 95),
                Size = new Size(645, 24),
                Text = "正在读取启动项..."
            };

            bootEntriesList = new ListView
            {
                Location = new Point(27, 127),
                Size = new Size(645, 165),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                HideSelection = false,
                MultiSelect = false,
                UseCompatibleStateImageBehavior = false
            };
            bootEntriesList.Columns.Add("系统", 300);
            bootEntriesList.Columns.Add("分区", 150);
            bootEntriesList.Columns.Add("状态", 160);
            bootEntriesList.SelectedIndexChanged += OnSelectedEntryChanged;

            var refreshButton = CreateButton("刷新", 100, false);
            refreshButton.Location = new Point(27, 322);
            refreshButton.Click += delegate { LoadBootEntries(); };

            setDefaultButton = CreateButton("仅设为默认", 130, false);
            setDefaultButton.Location = new Point(302, 322);
            setDefaultButton.Click += delegate { SetDefault(false); };

            setDefaultAndRestartButton = CreateButton("设为默认并重启", 165, true);
            setDefaultAndRestartButton.Location = new Point(438, 322);
            setDefaultAndRestartButton.Click += delegate { SetDefault(true); };

            Controls.Add(titleLabel);
            Controls.Add(descriptionLabel);
            Controls.Add(defaultSystemLabel);
            Controls.Add(bootEntriesList);
            Controls.Add(refreshButton);
            Controls.Add(setDefaultButton);
            Controls.Add(setDefaultAndRestartButton);

            Shown += delegate { LoadBootEntries(); };
        }

        private static Button CreateButton(string text, int width, bool isPrimary)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(width, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                BackColor = isPrimary ? Color.FromArgb(30, 100, 200) : Color.White,
                ForeColor = isPrimary ? Color.White : Color.FromArgb(45, 55, 70)
            };

            button.FlatAppearance.BorderColor = isPrimary
                ? Color.FromArgb(30, 100, 200)
                : Color.FromArgb(190, 198, 210);
            return button;
        }

        private void LoadBootEntries()
        {
            UseWaitCursor = true;
            bootEntriesList.Enabled = false;
            SetActionButtonsEnabled(false);
            bootEntriesList.Items.Clear();
            defaultSystemLabel.Text = "正在读取启动项...";

            try
            {
                List<BootEntry> bootEntries = BcdService.LoadEntries();

                BootEntry defaultEntry = null;
                foreach (BootEntry entry in bootEntries)
                {
                    var item = new ListViewItem(entry.Description);
                    item.SubItems.Add(entry.Device);
                    item.SubItems.Add(entry.IsDefault ? "当前默认" : "");
                    item.Tag = entry;
                    bootEntriesList.Items.Add(item);

                    if (entry.IsDefault)
                    {
                        defaultEntry = entry;
                    }
                }

                defaultSystemLabel.Text = defaultEntry == null
                    ? "当前默认系统：未识别"
                    : "当前默认系统：" + defaultEntry.DisplayName;

                if (bootEntriesList.Items.Count > 0)
                {
                    bootEntriesList.Items[0].Selected = true;
                }
            }
            catch (Exception exception)
            {
                defaultSystemLabel.Text = "无法读取启动项";
                MessageBox.Show(
                    "读取 Windows 引导配置失败。请确认程序已获得管理员权限。\r\n\r\n" + exception.Message,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                bootEntriesList.Enabled = true;
                UseWaitCursor = false;
                UpdateActionButtons();
            }
        }

        private void OnSelectedEntryChanged(object sender, EventArgs eventArgs)
        {
            UpdateActionButtons();
        }

        private void UpdateActionButtons()
        {
            BootEntry selectedEntry = GetSelectedEntry();
            bool canSetDefault = selectedEntry != null && !selectedEntry.IsDefault;
            SetActionButtonsEnabled(canSetDefault);
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            SetActionButtonState(setDefaultButton, enabled, false);
            SetActionButtonState(setDefaultAndRestartButton, enabled, true);
        }

        private static void SetActionButtonState(Button button, bool enabled, bool isPrimary)
        {
            button.Enabled = enabled;

            if (enabled)
            {
                button.BackColor = isPrimary ? Color.FromArgb(30, 100, 200) : Color.White;
                button.ForeColor = isPrimary ? Color.White : Color.FromArgb(45, 55, 70);
                button.FlatAppearance.BorderColor = isPrimary
                    ? Color.FromArgb(30, 100, 200)
                    : Color.FromArgb(190, 198, 210);
                return;
            }

            button.BackColor = Color.FromArgb(238, 241, 246);
            button.ForeColor = Color.FromArgb(135, 145, 160);
            button.FlatAppearance.BorderColor = Color.FromArgb(218, 224, 233);
        }

        private BootEntry GetSelectedEntry()
        {
            if (bootEntriesList.SelectedItems.Count != 1)
            {
                return null;
            }

            return bootEntriesList.SelectedItems[0].Tag as BootEntry;
        }

        private void SetDefault(bool restartAfterSetting)
        {
            BootEntry selectedEntry = GetSelectedEntry();
            if (selectedEntry == null)
            {
                return;
            }

            string action = restartAfterSetting ? "设为默认并立即重启" : "设为默认";
            string consequence = restartAfterSetting
                ? "\r\n\r\n当前打开的程序会因重启而关闭，请先保存正在进行的工作。"
                : "\r\n\r\n此操作只修改下次启动默认项，不会立即重启电脑。";
            DialogResult confirmation = MessageBox.Show(
                "确认将“" + selectedEntry.DisplayName + "”" + action + "吗？" + consequence,
                Text,
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirmation != DialogResult.OK)
            {
                return;
            }

            bool defaultWasSet = false;
            try
            {
                SetActionButtonsEnabled(false);
                BcdService.SetDefault(selectedEntry);
                defaultWasSet = true;

                if (restartAfterSetting)
                {
                    BcdService.RestartComputer();
                    Close();
                    return;
                }

                LoadBootEntries();
                MessageBox.Show(
                    "已将“" + selectedEntry.DisplayName + "”设为默认启动系统。",
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                string message = defaultWasSet
                    ? "默认启动系统已设置，但自动重启失败。请手动重启电脑。"
                    : "设置默认启动系统失败。";
                MessageBox.Show(
                    message + "\r\n\r\n" + exception.Message,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                UpdateActionButtons();
            }
        }
    }
}
