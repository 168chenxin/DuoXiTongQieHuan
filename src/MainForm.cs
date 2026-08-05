using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class MainForm : Form
    {
        private readonly DataGridView bootEntriesGrid;
        private readonly Label currentDefaultNameLabel;
        private readonly Label currentDefaultDeviceLabel;
        private readonly Label entryCountLabel;
        private readonly Label actionStatusLabel;
        private readonly ToolTip interfaceToolTip;
        private readonly Button refreshButton;
        private readonly Button setDefaultButton;
        private readonly Button setDefaultAndRestartButton;
        private Icon applicationIcon;

        public MainForm()
        {
            Text = "双系统快速切换";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(780, 478);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;

            LoadApplicationIcon();
            interfaceToolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 400,
                ReshowDelay = 100,
                ShowAlways = true
            };
            CreateHeader();

            var defaultBand = new Panel
            {
                BackColor = UiTheme.Surface,
                Location = new Point(28, 106),
                Size = new Size(724, 86)
            };

            var defaultLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(18, 13),
                Text = "当前默认启动"
            };

            currentDefaultNameLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 35),
                Size = new Size(520, 32),
                Text = "正在读取启动项..."
            };

            currentDefaultDeviceLabel = new Label
            {
                AutoSize = false,
                BackColor = UiTheme.AccentSoft,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Location = new Point(564, 27),
                Size = new Size(140, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            defaultBand.Controls.Add(defaultLabel);
            defaultBand.Controls.Add(currentDefaultNameLabel);
            defaultBand.Controls.Add(currentDefaultDeviceLabel);

            var bootMenuLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(28, 213),
                Text = "启动菜单"
            };

            entryCountLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(590, 215),
                Size = new Size(162, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            bootEntriesGrid = CreateBootEntriesGrid();
            bootEntriesGrid.Location = new Point(28, 240);
            bootEntriesGrid.Size = new Size(724, 126);
            bootEntriesGrid.SelectionChanged += OnSelectedEntryChanged;

            var divider = new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(28, 389),
                Size = new Size(724, 1)
            };

            actionStatusLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(28, 401),
                Size = new Size(338, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "正在读取 Windows 启动菜单..."
            };

            refreshButton = CreateButton("刷新启动项", 126, false);
            refreshButton.Location = new Point(28, 426);
            refreshButton.Click += delegate { LoadBootEntries(); };

            setDefaultButton = CreateButton("仅设为默认", 132, false);
            setDefaultButton.Location = new Point(430, 426);
            setDefaultButton.Click += delegate { SetDefault(false); };

            setDefaultAndRestartButton = CreateButton("切换并重启", 160, true);
            setDefaultAndRestartButton.Location = new Point(592, 426);
            setDefaultAndRestartButton.Click += delegate { SetDefault(true); };

            Controls.Add(defaultBand);
            Controls.Add(bootMenuLabel);
            Controls.Add(entryCountLabel);
            Controls.Add(bootEntriesGrid);
            Controls.Add(divider);
            Controls.Add(actionStatusLabel);
            Controls.Add(refreshButton);
            Controls.Add(setDefaultButton);
            Controls.Add(setDefaultAndRestartButton);

            SetActionButtonsEnabled(false);
            Shown += delegate { LoadBootEntries(); };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && applicationIcon != null)
            {
                applicationIcon.Dispose();
            }

            if (disposing && interfaceToolTip != null)
            {
                interfaceToolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        private void LoadApplicationIcon()
        {
            applicationIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (applicationIcon != null)
            {
                Icon = applicationIcon;
            }
        }

        private void CreateHeader()
        {
            var header = new Panel
            {
                BackColor = UiTheme.Header,
                Location = new Point(0, 0),
                Size = new Size(780, 78)
            };

            var logo = new PictureBox
            {
                BackColor = UiTheme.Header,
                Location = new Point(28, 18),
                Size = new Size(42, 42),
                SizeMode = PictureBoxSizeMode.Zoom
            };

            if (applicationIcon != null)
            {
                logo.Image = applicationIcon.ToBitmap();
            }

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Canvas,
                Location = new Point(84, 18),
                Text = "双系统快速切换"
            };

            var subtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.HeaderMuted,
                Location = new Point(86, 47),
                Text = "WINDOWS BOOT MENU"
            };

            var bcdLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.HeaderMuted,
                Location = new Point(618, 30),
                Size = new Size(134, 20),
                Text = "WINDOWS BCD",
                TextAlign = ContentAlignment.MiddleRight
            };

            var accentLine = new Panel
            {
                BackColor = UiTheme.Accent,
                Location = new Point(0, 75),
                Size = new Size(780, 3)
            };

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(bcdLabel);
            header.Controls.Add(accentLine);
            Controls.Add(header);
        }

        private static DataGridView CreateBootEntriesGrid()
        {
            var grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = UiTheme.Canvas,
                BorderStyle = BorderStyle.FixedSingle,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                MultiSelect = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                RowTemplate = { Height = 42 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                ShowCellErrors = false,
                ShowCellToolTips = true,
                ShowEditingIcon = false,
                ShowRowErrors = false
            };

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = UiTheme.Surface,
                SelectionForeColor = UiTheme.Muted
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = UiTheme.Canvas,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = UiTheme.Selection,
                SelectionForeColor = UiTheme.Ink
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(251, 252, 251),
                SelectionBackColor = UiTheme.Selection,
                SelectionForeColor = UiTheme.Ink
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "启动系统",
                Name = "system",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 330
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "分区",
                Name = "device",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 150
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                HeaderText = "状态",
                Name = "status",
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            return grid;
        }

        private static Button CreateButton(string text, int width, bool isPrimary)
        {
            var button = new Button
            {
                BackColor = isPrimary ? UiTheme.Primary : UiTheme.Canvas,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = isPrimary ? UiTheme.Canvas : UiTheme.Ink,
                Size = new Size(width, 40),
                Text = text,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderColor = isPrimary ? UiTheme.Primary : UiTheme.Border;
            button.FlatAppearance.MouseDownBackColor = isPrimary ? UiTheme.PrimaryHover : UiTheme.Surface;
            button.FlatAppearance.MouseOverBackColor = isPrimary ? UiTheme.PrimaryHover : UiTheme.Surface;
            return button;
        }

        private void LoadBootEntries()
        {
            UseWaitCursor = true;
            refreshButton.Enabled = false;
            bootEntriesGrid.Enabled = false;
            SetActionButtonsEnabled(false);
            bootEntriesGrid.Rows.Clear();
            currentDefaultNameLabel.Text = "正在读取启动项...";
            currentDefaultDeviceLabel.Visible = false;
            entryCountLabel.Text = string.Empty;
            actionStatusLabel.Text = "正在读取 Windows 启动菜单...";
            interfaceToolTip.SetToolTip(currentDefaultDeviceLabel, string.Empty);
            interfaceToolTip.SetToolTip(actionStatusLabel, string.Empty);

            try
            {
                List<BootEntry> bootEntries = BcdService.LoadEntries();
                BootEntry defaultEntry = null;
                DataGridViewRow firstSwitchableRow = null;

                foreach (BootEntry entry in bootEntries)
                {
                    int rowIndex = bootEntriesGrid.Rows.Add(
                        entry.Description,
                        entry.Device,
                        entry.IsDefault ? "当前默认" : "可切换");
                    DataGridViewRow row = bootEntriesGrid.Rows[rowIndex];
                    row.Tag = entry;
                    row.Cells[0].ToolTipText = entry.Description;
                    row.Cells[1].ToolTipText = entry.Device;

                    if (entry.IsDefault)
                    {
                        row.DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
                        row.Cells[2].Style.ForeColor = UiTheme.Primary;
                        defaultEntry = entry;
                    }
                    else if (firstSwitchableRow == null)
                    {
                        firstSwitchableRow = row;
                    }
                }

                entryCountLabel.Text = bootEntries.Count + " 个可用系统";
                SetCurrentDefault(defaultEntry);
                SelectInitialTarget(firstSwitchableRow);
            }
            catch (Exception exception)
            {
                currentDefaultNameLabel.Text = "无法读取启动项";
                currentDefaultDeviceLabel.Visible = false;
                actionStatusLabel.Text = "请检查管理员权限和 Windows 引导配置";
                MessageBox.Show(
                    "读取 Windows 引导配置失败。请确认程序已获得管理员权限。\r\n\r\n" + exception.Message,
                    Text,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                refreshButton.Enabled = true;
                bootEntriesGrid.Enabled = true;
                UseWaitCursor = false;
                UpdateActionButtons();
            }
        }

        private void SetCurrentDefault(BootEntry defaultEntry)
        {
            if (defaultEntry == null)
            {
                currentDefaultNameLabel.Text = "未识别默认系统";
                currentDefaultDeviceLabel.Visible = false;
                interfaceToolTip.SetToolTip(currentDefaultDeviceLabel, string.Empty);
                return;
            }

            currentDefaultNameLabel.Text = defaultEntry.Description;
            currentDefaultDeviceLabel.Text = defaultEntry.Device;
            currentDefaultDeviceLabel.Visible = true;
            interfaceToolTip.SetToolTip(currentDefaultDeviceLabel, defaultEntry.Device);
        }

        private void SelectInitialTarget(DataGridViewRow firstSwitchableRow)
        {
            bootEntriesGrid.ClearSelection();

            if (firstSwitchableRow != null)
            {
                firstSwitchableRow.Selected = true;
                bootEntriesGrid.CurrentCell = firstSwitchableRow.Cells[0];
                return;
            }

            if (bootEntriesGrid.Rows.Count > 0)
            {
                bootEntriesGrid.Rows[0].Selected = true;
                bootEntriesGrid.CurrentCell = bootEntriesGrid.Rows[0].Cells[0];
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

            if (selectedEntry == null)
            {
                return;
            }

            actionStatusLabel.Text = selectedEntry.IsDefault
                ? "当前系统已是默认启动项"
                : "已选择 " + selectedEntry.Device + "，将在下次启动时生效";
            interfaceToolTip.SetToolTip(actionStatusLabel, actionStatusLabel.Text);
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
                button.BackColor = isPrimary ? UiTheme.Primary : UiTheme.Canvas;
                button.ForeColor = isPrimary ? UiTheme.Canvas : UiTheme.Ink;
                button.FlatAppearance.BorderColor = isPrimary ? UiTheme.Primary : UiTheme.Border;
                button.FlatAppearance.MouseDownBackColor = isPrimary ? UiTheme.PrimaryHover : UiTheme.Surface;
                button.FlatAppearance.MouseOverBackColor = isPrimary ? UiTheme.PrimaryHover : UiTheme.Surface;
                return;
            }

            button.BackColor = UiTheme.Disabled;
            button.ForeColor = UiTheme.DisabledText;
            button.FlatAppearance.BorderColor = UiTheme.Border;
            button.FlatAppearance.MouseDownBackColor = UiTheme.Disabled;
            button.FlatAppearance.MouseOverBackColor = UiTheme.Disabled;
        }

        private BootEntry GetSelectedEntry()
        {
            if (bootEntriesGrid.SelectedRows.Count != 1)
            {
                return null;
            }

            return bootEntriesGrid.SelectedRows[0].Tag as BootEntry;
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
                "确认切换",
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
