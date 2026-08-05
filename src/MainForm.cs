using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal sealed class MainForm : Form
    {
        private readonly AnimatedDataGridView bootEntriesGrid;
        private readonly AnimatedLabel currentDefaultNameLabel;
        private readonly RoundedLabel currentDefaultDeviceLabel;
        private readonly AnimatedLabel entryCountLabel;
        private readonly AnimatedLabel actionStatusLabel;
        private readonly ToolTip interfaceToolTip;
        private readonly AnimatedButton refreshButton;
        private readonly AnimatedButton editRemarkButton;
        private readonly AnimatedButton setDefaultButton;
        private readonly AnimatedButton setDefaultAndRestartButton;
        private Icon applicationIcon;
        private Image applicationLogo;

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

            LoadApplicationAssets();
            interfaceToolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 400,
                ReshowDelay = 100,
                ShowAlways = true
            };
            CreateHeader();

            var defaultBand = new RoundedPanel
            {
                FillColor = UiTheme.Surface,
                CornerRadius = UiTheme.SurfaceCornerRadius,
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

            currentDefaultNameLabel = new AnimatedLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackdropColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(18, 35),
                Size = new Size(520, 32),
                Text = "正在读取启动项..."
            };

            currentDefaultDeviceLabel = new RoundedLabel
            {
                AutoSize = false,
                AutoEllipsis = true,
                BackdropColor = UiTheme.Surface,
                CornerRadius = UiTheme.BadgeCornerRadius,
                FillColor = UiTheme.AccentSoft,
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

            entryCountLabel = new AnimatedLabel
            {
                AutoSize = false,
                BackdropColor = UiTheme.Canvas,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(590, 215),
                Size = new Size(162, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            bootEntriesGrid = CreateBootEntriesGrid();
            bootEntriesGrid.Location = new Point(28, 240);
            bootEntriesGrid.Size = new Size(724, 126);
            bootEntriesGrid.AccessibleName = "Windows 启动系统列表";
            bootEntriesGrid.AccessibleDescription = "选择要设为下次默认启动的 Windows 系统";
            bootEntriesGrid.SelectionChanged += OnSelectedEntryChanged;
            bootEntriesGrid.CellDoubleClick += OnBootEntryDoubleClick;

            var divider = new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(28, 389),
                Size = new Size(724, 1)
            };

            actionStatusLabel = new AnimatedLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackdropColor = UiTheme.Canvas,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(28, 401),
                Size = new Size(338, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "正在读取 Windows 启动菜单..."
            };

            refreshButton = CreateButton("刷新启动项", 126, false);
            refreshButton.Location = new Point(28, 426);
            refreshButton.AccessibleName = "刷新启动项";
            refreshButton.Click += delegate { LoadBootEntries(); };

            editRemarkButton = CreateButton("编辑备注", 116, false);
            editRemarkButton.Location = new Point(162, 426);
            editRemarkButton.AccessibleName = "编辑启动项备注";
            editRemarkButton.Click += delegate { EditSelectedRemark(); };
            interfaceToolTip.SetToolTip(editRemarkButton, "为选中的启动系统设置用途备注");

            setDefaultButton = CreateButton("仅设为默认", 132, false);
            setDefaultButton.Location = new Point(430, 426);
            setDefaultButton.AccessibleName = "仅将选中系统设为默认";
            setDefaultButton.Click += delegate { SetDefault(false); };

            setDefaultAndRestartButton = CreateButton("切换并重启", 160, true);
            setDefaultAndRestartButton.Location = new Point(592, 426);
            setDefaultAndRestartButton.AccessibleName = "将选中系统设为默认并重启";
            setDefaultAndRestartButton.Click += delegate { SetDefault(true); };

            Controls.Add(defaultBand);
            Controls.Add(bootMenuLabel);
            Controls.Add(entryCountLabel);
            Controls.Add(bootEntriesGrid);
            Controls.Add(divider);
            Controls.Add(actionStatusLabel);
            Controls.Add(refreshButton);
            Controls.Add(editRemarkButton);
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

            if (disposing && applicationLogo != null)
            {
                applicationLogo.Dispose();
            }

            if (disposing && interfaceToolTip != null)
            {
                interfaceToolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        private void LoadApplicationAssets()
        {
            applicationIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (applicationIcon != null)
            {
                Icon = applicationIcon;
            }

            using (Stream logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
                "DualBootSwitcher.Logo.png"))
            {
                if (logoStream != null)
                {
                    using (Image sourceImage = Image.FromStream(logoStream))
                    {
                        applicationLogo = new Bitmap(sourceImage);
                    }
                }
            }

            if (applicationLogo == null && applicationIcon != null)
            {
                applicationLogo = applicationIcon.ToBitmap();
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

            var logo = new HighQualityImageControl
            {
                BackColor = UiTheme.Header,
                Image = applicationLogo,
                Location = new Point(28, 16),
                Size = new Size(46, 46)
            };

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(88, 18),
                Text = "双系统快速切换"
            };

            var subtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.HeaderMuted,
                Location = new Point(90, 47),
                Text = "WINDOWS BOOT MENU"
            };

            var bcdLabel = new RoundedLabel
            {
                AutoSize = false,
                BackdropColor = UiTheme.Header,
                CornerRadius = UiTheme.BadgeCornerRadius,
                FillColor = UiTheme.AccentSoft,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Location = new Point(630, 24),
                Size = new Size(122, 30),
                Text = "管理员模式",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var accentLine = new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(0, 77),
                Size = new Size(780, 1)
            };

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(bcdLabel);
            header.Controls.Add(accentLine);
            Controls.Add(header);
        }

        private static AnimatedDataGridView CreateBootEntriesGrid()
        {
            var grid = new AnimatedDataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                BackgroundColor = UiTheme.Surface,
                BackdropColor = UiTheme.Canvas,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                ColumnHeadersHeight = 32,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                EnableHeadersVisualStyles = false,
                GridColor = UiTheme.Border,
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
                BackColor = UiTheme.Canvas,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = UiTheme.Canvas,
                SelectionForeColor = UiTheme.Muted
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Padding = new Padding(10, 0, 0, 0),
                SelectionBackColor = UiTheme.Selection,
                SelectionForeColor = UiTheme.Ink
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = UiTheme.Canvas,
                SelectionBackColor = UiTheme.Selection,
                SelectionForeColor = UiTheme.Ink
            };

            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point)
                },
                HeaderText = "启动系统",
                Name = "system",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 260
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point)
                },
                HeaderText = "分区",
                Name = "device",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 110
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = "备注",
                Name = "remark",
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = 210
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point)
                },
                HeaderText = "状态",
                Name = "status",
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            return grid;
        }

        private static AnimatedButton CreateButton(string text, int width, bool isPrimary)
        {
            var button = new AnimatedButton(isPrimary)
            {
                Font = new Font(
                    "Segoe UI",
                    9F,
                    isPrimary ? FontStyle.Bold : FontStyle.Regular,
                    GraphicsUnit.Point),
                Size = new Size(width, 40),
                Text = text
            };
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
                    string remark = GetEntryRemark(entry);
                    int rowIndex = bootEntriesGrid.Rows.Add(
                        entry.Description,
                        entry.Device,
                        GetRemarkDisplay(remark),
                        entry.IsDefault ? "当前默认" : "可切换");
                    DataGridViewRow row = bootEntriesGrid.Rows[rowIndex];
                    row.Tag = entry;
                    row.Cells[0].ToolTipText = entry.Description;
                    row.Cells[1].ToolTipText = entry.Device;
                    row.Cells[2].ToolTipText = remark;
                    if (string.IsNullOrWhiteSpace(remark))
                    {
                        row.Cells[2].Style.ForeColor = UiTheme.Muted;
                    }

                    if (entry.IsDefault)
                    {
                        row.Cells[3].Style.ForeColor = UiTheme.Primary;
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
                interfaceToolTip.SetToolTip(currentDefaultNameLabel, string.Empty);
                interfaceToolTip.SetToolTip(currentDefaultDeviceLabel, string.Empty);
                return;
            }

            string displayName = GetEntryDisplayName(defaultEntry);
            currentDefaultNameLabel.Text = displayName;
            currentDefaultDeviceLabel.Text = defaultEntry.Device;
            currentDefaultDeviceLabel.Visible = true;
            interfaceToolTip.SetToolTip(currentDefaultNameLabel, displayName);
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
            setDefaultButton.Enabled = canSetDefault;
            setDefaultAndRestartButton.Enabled = canSetDefault;
            editRemarkButton.Enabled = selectedEntry != null;

            if (selectedEntry == null)
            {
                return;
            }

            actionStatusLabel.Text = selectedEntry.IsDefault
                ? "当前系统已是默认启动项"
                : "已选择 " + selectedEntry.Device + "，确认后将在下次启动时生效";
            interfaceToolTip.SetToolTip(actionStatusLabel, actionStatusLabel.Text);
        }

        private void SetActionButtonsEnabled(bool enabled)
        {
            editRemarkButton.Enabled = enabled;
            setDefaultButton.Enabled = enabled;
            setDefaultAndRestartButton.Enabled = enabled;
        }

        private void OnBootEntryDoubleClick(object sender, DataGridViewCellEventArgs eventArgs)
        {
            if (eventArgs.RowIndex < 0 || eventArgs.RowIndex >= bootEntriesGrid.Rows.Count)
            {
                return;
            }

            bootEntriesGrid.ClearSelection();
            bootEntriesGrid.Rows[eventArgs.RowIndex].Selected = true;
            bootEntriesGrid.CurrentCell = bootEntriesGrid.Rows[eventArgs.RowIndex].Cells[0];
            EditSelectedRemark();
        }

        private void EditSelectedRemark()
        {
            BootEntry selectedEntry = GetSelectedEntry();
            if (selectedEntry == null)
            {
                return;
            }

            using (var dialog = new RemarkDialog(selectedEntry.Description, GetEntryRemark(selectedEntry)))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    BootRemarkStore.Set(selectedEntry.Identifier, dialog.Remark);
                    UpdateRemarkRow(selectedEntry);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(
                        exception.Message,
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void UpdateRemarkRow(BootEntry entry)
        {
            foreach (DataGridViewRow row in bootEntriesGrid.Rows)
            {
                if (row.Tag != entry)
                {
                    continue;
                }

                string remark = GetEntryRemark(entry);
                row.Cells[2].Value = GetRemarkDisplay(remark);
                row.Cells[2].ToolTipText = remark;
                row.Cells[2].Style.ForeColor = string.IsNullOrWhiteSpace(remark)
                    ? UiTheme.Muted
                    : UiTheme.Ink;
                break;
            }

            if (entry.IsDefault)
            {
                SetCurrentDefault(entry);
            }

            UpdateActionButtons();
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
                "确认将“" + GetEntryConfirmationName(selectedEntry) + "”" + action + "吗？" + consequence,
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
                    "已将“" + GetEntryConfirmationName(selectedEntry) + "”设为默认启动系统。",
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

        private static string GetEntryRemark(BootEntry entry)
        {
            return entry == null ? string.Empty : BootRemarkStore.Get(entry.Identifier);
        }

        private static string GetRemarkDisplay(string remark)
        {
            return string.IsNullOrWhiteSpace(remark) ? "未设置" : remark;
        }

        private static string GetEntryDisplayName(BootEntry entry)
        {
            string remark = GetEntryRemark(entry);
            return string.IsNullOrWhiteSpace(remark)
                ? entry.Description
                : entry.Description + " · " + remark;
        }

        private static string GetEntryConfirmationName(BootEntry entry)
        {
            string displayName = GetEntryDisplayName(entry);
            return string.IsNullOrWhiteSpace(entry.Device)
                ? displayName
                : displayName + "（" + entry.Device + "）";
        }
    }
}
