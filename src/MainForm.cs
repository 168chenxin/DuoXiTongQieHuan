using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntButton = AntdUI.Button;
using AntPanel = AntdUI.Panel;
using AntTable = AntdUI.Table;
using AntTag = AntdUI.Tag;

namespace DualBootSwitcher
{
    internal sealed class MainForm : Form
    {
        private readonly AntTable bootEntriesTable;
        private readonly AnimatedLabel currentDefaultNameLabel;
        private readonly AntTag currentDefaultDeviceTag;
        private readonly AnimatedLabel entryCountLabel;
        private readonly AnimatedLabel actionStatusLabel;
        private readonly AnimatedLabel selectedNameLabel;
        private readonly AnimatedLabel selectedRemarkLabel;
        private readonly AntTag selectedDeviceTag;
        private readonly AntTag selectedStateTag;
        private readonly ToolTip interfaceToolTip;
        private readonly AntButton timeoutButton;
        private readonly AntButton refreshButton;
        private readonly AntButton editRemarkButton;
        private readonly AntButton setDefaultButton;
        private readonly AntButton setDefaultAndRestartButton;
        private readonly List<BootRowViewModel> bootRows = new List<BootRowViewModel>();
        private Icon applicationIcon;
        private Image applicationLogo;
        private BootRowViewModel selectedRow;
        private int currentTimeoutSeconds = -1;
        private int selectionGeneration;
        private bool isLoadingBootEntries;

        public MainForm()
        {
            Text = "双系统快速切换";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(920, 620);
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

            var defaultBand = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = 12,
                Location = new Point(28, 104),
                Size = new Size(864, 92)
            };

            var defaultLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, 15),
                Text = "当前默认启动"
            };

            currentDefaultNameLabel = new AnimatedLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackdropColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(20, 40),
                Size = new Size(650, 34),
                Text = "正在读取启动项..."
            };

            currentDefaultDeviceTag = new AntTag
            {
                AutoSize = false,
                BackColor = UiTheme.AccentSoft,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Type = AntdUI.TTypeMini.Primary,
                Radius = 9,
                Location = new Point(706, 29),
                Size = new Size(136, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            defaultBand.Controls.Add(defaultLabel);
            defaultBand.Controls.Add(currentDefaultNameLabel);
            defaultBand.Controls.Add(currentDefaultDeviceTag);

            var bootMenuLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(28, 221),
                Text = "选择下次启动系统"
            };

            entryCountLabel = new AnimatedLabel
            {
                AutoSize = false,
                BackdropColor = UiTheme.Canvas,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(356, 221),
                Size = new Size(146, 20),
                TextAlign = ContentAlignment.MiddleRight
            };

            timeoutButton = UiFactory.CreateButton("启动等待：读取中...", 158, false);
            timeoutButton.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            timeoutButton.Location = new Point(562, 212);
            timeoutButton.Size = new Size(158, 36);
            timeoutButton.AccessibleName = "修改启动菜单等待时间";
            timeoutButton.Enabled = false;
            timeoutButton.Click += delegate { EditBootTimeout(); };

            refreshButton = UiFactory.CreateButton("刷新启动项", 156, false);
            refreshButton.Location = new Point(736, 212);
            refreshButton.Size = new Size(156, 36);
            refreshButton.AccessibleName = "刷新启动项";
            refreshButton.Click += async delegate { await LoadBootEntriesAsync(); };

            bootEntriesTable = CreateBootEntriesTable();
            bootEntriesTable.Location = new Point(28, 258);
            bootEntriesTable.Size = new Size(570, 246);
            bootEntriesTable.AccessibleName = "Windows 启动系统列表";
            bootEntriesTable.AccessibleDescription = "选择要设为下次默认启动的 Windows 系统";
            bootEntriesTable.CellClick += OnBootEntryClick;
            bootEntriesTable.CellDoubleClick += OnBootEntryDoubleClick;
            bootEntriesTable.SelectIndexChanged += OnBootSelectionChanged;

            var selectionPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = 12,
                Location = new Point(616, 258),
                Size = new Size(276, 246)
            };

            var selectionLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, 17),
                Text = "当前选择"
            };

            selectedNameLabel = new AnimatedLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackdropColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(20, 42),
                Size = new Size(236, 30),
                Text = "请选择启动系统"
            };

            selectedDeviceTag = new AntTag
            {
                AutoSize = false,
                BackColor = UiTheme.Canvas,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Radius = 8,
                Location = new Point(20, 78),
                Size = new Size(76, 30),
                Text = "--",
                TextAlign = ContentAlignment.MiddleCenter
            };

            selectedStateTag = new AntTag
            {
                AutoSize = false,
                BackColor = UiTheme.AccentSoft,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Radius = 8,
                Location = new Point(106, 78),
                Size = new Size(116, 30),
                Text = "等待选择",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var selectionDivider = new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(20, 124),
                Size = new Size(236, 1)
            };

            var remarkLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, 139),
                Text = "用途备注"
            };

            selectedRemarkLabel = new AnimatedLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackdropColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Location = new Point(20, 160),
                Size = new Size(236, 28),
                Text = "未设置备注"
            };

            editRemarkButton = UiFactory.CreateButton("编辑用途备注", 236, false);
            editRemarkButton.Location = new Point(20, 198);
            editRemarkButton.Size = new Size(236, 34);
            editRemarkButton.AccessibleName = "编辑启动项备注";
            editRemarkButton.Click += delegate { EditSelectedRemark(); };

            selectionPanel.Controls.Add(selectionLabel);
            selectionPanel.Controls.Add(selectedNameLabel);
            selectionPanel.Controls.Add(selectedDeviceTag);
            selectionPanel.Controls.Add(selectedStateTag);
            selectionPanel.Controls.Add(selectionDivider);
            selectionPanel.Controls.Add(remarkLabel);
            selectionPanel.Controls.Add(selectedRemarkLabel);
            selectionPanel.Controls.Add(editRemarkButton);

            var divider = new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(28, 528),
                Size = new Size(864, 1)
            };

            actionStatusLabel = new AnimatedLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackdropColor = UiTheme.Canvas,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(28, 540),
                Size = new Size(520, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "正在读取 Windows 启动菜单..."
            };

            interfaceToolTip.SetToolTip(editRemarkButton, "为选中的启动系统设置用途备注");

            setDefaultButton = UiFactory.CreateButton("仅设为默认", 142, false);
            setDefaultButton.Location = new Point(576, 565);
            setDefaultButton.Size = new Size(142, 42);
            setDefaultButton.AccessibleName = "仅将选中系统设为默认";
            setDefaultButton.Click += delegate { SetDefault(false); };

            setDefaultAndRestartButton = UiFactory.CreateButton("切换并重启", 162, true);
            setDefaultAndRestartButton.Location = new Point(730, 565);
            setDefaultAndRestartButton.Size = new Size(162, 42);
            setDefaultAndRestartButton.AccessibleName = "将选中系统设为默认并重启";
            setDefaultAndRestartButton.Click += delegate { SetDefault(true); };

            Controls.Add(defaultBand);
            Controls.Add(bootMenuLabel);
            Controls.Add(timeoutButton);
            Controls.Add(refreshButton);
            Controls.Add(entryCountLabel);
            Controls.Add(bootEntriesTable);
            Controls.Add(selectionPanel);
            Controls.Add(divider);
            Controls.Add(actionStatusLabel);
            Controls.Add(setDefaultButton);
            Controls.Add(setDefaultAndRestartButton);

            SetActionButtonsEnabled(false);
            Shown += async delegate { await LoadBootEntriesAsync(); };
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
                Size = new Size(920, 84)
            };

            var logo = new HighQualityImageControl
            {
                BackColor = UiTheme.Header,
                Image = applicationLogo,
                Location = new Point(28, 19),
                Size = new Size(46, 46)
            };

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(94, 18),
                Text = "双系统快速切换"
            };

            var subtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.HeaderMuted,
                Location = new Point(96, 49),
                Text = "Windows 启动配置"
            };

            var bcdLabel = new AntTag
            {
                AutoSize = false,
                BackColor = UiTheme.AccentSoft,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Type = AntdUI.TTypeMini.Primary,
                Radius = 9,
                Location = new Point(754, 25),
                Size = new Size(138, 32),
                Text = "管理员权限已启用",
                TextAlign = ContentAlignment.MiddleCenter
            };

            var accentLine = new Panel
            {
                BackColor = UiTheme.Border,
                Location = new Point(0, 83),
                Size = new Size(920, 1)
            };

            header.Controls.Add(logo);
            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            header.Controls.Add(bcdLabel);
            header.Controls.Add(accentLine);
            Controls.Add(header);
        }

        private static AntTable CreateBootEntriesTable()
        {
            var table = new AntTable
            {
                AnimationTime = UiTheme.StateMotionDuration,
                BackColor = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Bordered = false,
                ColumnBack = UiTheme.Canvas,
                ColumnFore = UiTheme.Muted,
                ColumnFont = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                FixedHeader = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                GapCell = 14,
                LostFocusClearSelection = false,
                MultipleRows = false,
                Radius = 12,
                RowHeight = 48,
                RowHeightHeader = 38,
                RowHoverBg = UiTheme.Hover,
                RowSelectedBg = UiTheme.SelectionStrong,
                RowSelectedFore = UiTheme.Ink,
                ShowTip = true,
                VisibleHeader = true
            };

            var systemColumn = new AntdUI.Column("SystemName", "启动系统")
            {
                Ellipsis = true,
                Width = "37%",
                Style = new AntTable.CellStyleInfo { FontBold = true }
            };
            var deviceColumn = new AntdUI.Column("Device", "分区")
            {
                Width = "16%",
                Style = new AntTable.CellStyleInfo { FontBold = true }
            };
            var remarkColumn = new AntdUI.Column("Remark", "用途备注")
            {
                Ellipsis = true,
                Width = "29%"
            };
            var statusColumn = new AntdUI.Column("Status", "状态", AntdUI.ColumnAlign.Center)
            {
                Width = "18%"
            };

            var regularRemarkFont = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            var emphasizedRemarkFont = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            remarkColumn.Render = delegate(object value, object record, int rowIndex)
            {
                var row = record as BootRowViewModel;
                if (row == null)
                {
                    return value;
                }

                return new AntdUI.CellText(row.Remark)
                {
                    Font = row.HasRemark ? emphasizedRemarkFont : regularRemarkFont,
                    Fore = row.HasRemark ? UiTheme.Accent : UiTheme.Muted
                };
            };
            statusColumn.Render = delegate(object value, object record, int rowIndex)
            {
                var row = record as BootRowViewModel;
                if (row == null)
                {
                    return value;
                }

                return new AntdUI.CellTag(row.Status)
                {
                    Back = row.IsDefault ? UiTheme.SuccessSoft : UiTheme.AccentSoft,
                    BorderWidth = 0F,
                    Fore = row.IsDefault ? UiTheme.Success : UiTheme.Accent
                };
            };
            table.Disposed += delegate
            {
                regularRemarkFont.Dispose();
                emphasizedRemarkFont.Dispose();
            };

            table.Columns.Add(systemColumn);
            table.Columns.Add(deviceColumn);
            table.Columns.Add(remarkColumn);
            table.Columns.Add(statusColumn);

            return table;
        }

        private async Task LoadBootEntriesAsync()
        {
            if (isLoadingBootEntries)
            {
                return;
            }

            isLoadingBootEntries = true;
            UseWaitCursor = true;
            selectionGeneration++;
            refreshButton.Enabled = false;
            refreshButton.Loading = true;
            timeoutButton.Enabled = false;
            timeoutButton.Text = "启动等待：读取中...";
            currentTimeoutSeconds = -1;
            bootEntriesTable.Enabled = false;
            SetActionButtonsEnabled(false);
            bootRows.Clear();
            selectedRow = null;
            bootEntriesTable.DataSource = null;
            currentDefaultNameLabel.Text = "正在读取启动项...";
            currentDefaultDeviceTag.Visible = false;
            entryCountLabel.Text = string.Empty;
            selectedNameLabel.Text = "请选择启动系统";
            selectedDeviceTag.Text = "--";
            selectedStateTag.Text = "等待选择";
            selectedStateTag.Type = AntdUI.TTypeMini.Default;
            selectedStateTag.BackColor = UiTheme.Canvas;
            selectedStateTag.ForeColor = UiTheme.Muted;
            selectedRemarkLabel.Text = "未设置备注";
            selectedRemarkLabel.ForeColor = UiTheme.Muted;
            actionStatusLabel.Text = "正在读取 Windows 启动菜单...";
            interfaceToolTip.SetToolTip(currentDefaultDeviceTag, string.Empty);
            interfaceToolTip.SetToolTip(actionStatusLabel, string.Empty);

            try
            {
                BootConfiguration configuration = await Task.Run(
                    (Func<BootConfiguration>)BcdService.LoadConfiguration);
                List<BootEntry> bootEntries = configuration.Entries;
                BootEntry defaultEntry = null;
                BootRowViewModel firstSwitchableRow = null;
                SetTimeoutDisplay(configuration.TimeoutSeconds);

                foreach (BootEntry entry in bootEntries)
                {
                    string remark = GetEntryRemark(entry);
                    var row = new BootRowViewModel(entry, remark);
                    bootRows.Add(row);

                    if (entry.IsDefault)
                    {
                        defaultEntry = entry;
                    }
                    else if (firstSwitchableRow == null)
                    {
                        firstSwitchableRow = row;
                    }
                }

                bootEntriesTable.DataSource = bootRows.ToArray();
                entryCountLabel.Text = bootEntries.Count + " 个可用系统";
                SetCurrentDefault(defaultEntry);
                SelectInitialTarget(firstSwitchableRow);
            }
            catch (Exception exception)
            {
                currentDefaultNameLabel.Text = "无法读取启动项";
                currentDefaultDeviceTag.Visible = false;
                timeoutButton.Text = "启动等待：读取失败";
                timeoutButton.Enabled = false;
                actionStatusLabel.Text = "请检查管理员权限和 Windows 引导配置";
                UiDialogs.ShowError(
                    this,
                    "读取启动配置失败",
                    "读取 Windows 引导配置失败。请确认程序已获得管理员权限。\r\n\r\n" + exception.Message);
            }
            finally
            {
                isLoadingBootEntries = false;
                refreshButton.Loading = false;
                refreshButton.Enabled = true;
                bootEntriesTable.Enabled = true;
                UseWaitCursor = false;
                UpdateActionButtons();
            }
        }

        private void SetCurrentDefault(BootEntry defaultEntry)
        {
            if (defaultEntry == null)
            {
                currentDefaultNameLabel.Text = "未识别默认系统";
                currentDefaultDeviceTag.Visible = false;
                interfaceToolTip.SetToolTip(currentDefaultNameLabel, string.Empty);
                interfaceToolTip.SetToolTip(currentDefaultDeviceTag, string.Empty);
                return;
            }

            string displayName = GetEntryDisplayName(defaultEntry);
            currentDefaultNameLabel.Text = displayName;
            currentDefaultDeviceTag.Text = defaultEntry.Device;
            currentDefaultDeviceTag.Visible = true;
            interfaceToolTip.SetToolTip(currentDefaultNameLabel, displayName);
            interfaceToolTip.SetToolTip(currentDefaultDeviceTag, defaultEntry.Device);
        }

        private void SelectInitialTarget(BootRowViewModel firstSwitchableRow)
        {
            if (firstSwitchableRow != null)
            {
                SelectBootRow(firstSwitchableRow, true);
            }
            else if (bootRows.Count > 0)
            {
                SelectBootRow(bootRows[0], true);
            }
        }

        private void OnBootEntryClick(object sender, AntdUI.TableClickEventArgs eventArgs)
        {
            var row = eventArgs.Record as BootRowViewModel;
            if (row == null)
            {
                return;
            }

            SelectBootRow(row, false);
        }

        private void OnBootSelectionChanged(object sender, EventArgs eventArgs)
        {
            var focusedRow = bootEntriesTable.FocusedRow as BootRowViewModel;
            if (focusedRow == null ||
                focusedRow == selectedRow ||
                !bootRows.Contains(focusedRow))
            {
                return;
            }

            selectedRow = focusedRow;
            UpdateActionButtons();
        }

        private void SelectBootRow(BootRowViewModel row, bool deferVisualSelection)
        {
            selectedRow = row;
            UpdateActionButtons();

            int rowIndex = bootRows.IndexOf(row);
            if (rowIndex < 0)
            {
                return;
            }

            int expectedGeneration = selectionGeneration;
            MethodInvoker applySelection = delegate
            {
                if (IsDisposed ||
                    !IsHandleCreated ||
                    expectedGeneration != selectionGeneration ||
                    rowIndex >= bootRows.Count ||
                    !ReferenceEquals(bootRows[rowIndex], row))
                {
                    return;
                }

                bootEntriesTable.SelectedIndex = rowIndex + 1;
                selectedRow = row;
                UpdateActionButtons();
            };

            if (deferVisualSelection)
            {
                BeginInvoke(applySelection);
            }
            else
            {
                applySelection();
            }
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
                selectedNameLabel.Text = "请选择启动系统";
                selectedDeviceTag.Text = "--";
                selectedStateTag.Text = "等待选择";
                selectedStateTag.Type = AntdUI.TTypeMini.Default;
                selectedStateTag.BackColor = UiTheme.Canvas;
                selectedStateTag.ForeColor = UiTheme.Muted;
                selectedRemarkLabel.Text = "未设置备注";
                selectedRemarkLabel.ForeColor = UiTheme.Muted;
                return;
            }

            string remark = GetEntryRemark(selectedEntry);
            selectedNameLabel.Text = selectedEntry.Description;
            selectedDeviceTag.Text = selectedEntry.Device;
            selectedDeviceTag.Type = AntdUI.TTypeMini.Default;
            selectedStateTag.Text = selectedEntry.IsDefault ? "当前默认系统" : "可设为默认";
            selectedStateTag.Type = selectedEntry.IsDefault
                ? AntdUI.TTypeMini.Success
                : AntdUI.TTypeMini.Primary;
            selectedStateTag.BackColor = selectedEntry.IsDefault
                ? UiTheme.SuccessSoft
                : UiTheme.AccentSoft;
            selectedStateTag.ForeColor = selectedEntry.IsDefault
                ? UiTheme.Success
                : UiTheme.Accent;
            selectedRemarkLabel.Text = string.IsNullOrWhiteSpace(remark) ? "未设置备注" : remark;
            selectedRemarkLabel.ForeColor = string.IsNullOrWhiteSpace(remark)
                ? UiTheme.Muted
                : UiTheme.Accent;
            interfaceToolTip.SetToolTip(selectedNameLabel, selectedEntry.Description);
            interfaceToolTip.SetToolTip(selectedRemarkLabel, selectedRemarkLabel.Text);

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

        private void OnBootEntryDoubleClick(object sender, AntdUI.TableClickEventArgs eventArgs)
        {
            var row = eventArgs.Record as BootRowViewModel;
            if (row == null)
            {
                return;
            }

            SelectBootRow(row, false);
            EditSelectedRemark();
        }

        private void EditBootTimeout()
        {
            if (currentTimeoutSeconds < 0)
            {
                return;
            }

            try
            {
                var workflow = new BootTimeoutWorkflow(
                    RequestBootTimeout,
                    delegate(int seconds)
                    {
                        timeoutButton.Enabled = false;
                        BcdService.SetTimeout(seconds);
                    });
                BootTimeoutEditResult editResult = workflow.Run(currentTimeoutSeconds);

                if (editResult.Result == BootTimeoutChangeResult.Cancelled)
                {
                    return;
                }

                if (editResult.Result == BootTimeoutChangeResult.Unchanged)
                {
                    actionStatusLabel.Text = "启动等待时间没有变化";
                    interfaceToolTip.SetToolTip(actionStatusLabel, actionStatusLabel.Text);
                    return;
                }

                SetTimeoutDisplay(editResult.RequestedSeconds);
                actionStatusLabel.Text = "启动等待时间已设置为 " + editResult.RequestedSeconds + " 秒";
                interfaceToolTip.SetToolTip(actionStatusLabel, actionStatusLabel.Text);
            }
            catch (Exception exception)
            {
                timeoutButton.Enabled = currentTimeoutSeconds >= 0;
                UiDialogs.ShowError(
                    this,
                    "修改启动等待失败",
                    "修改启动等待时间失败。\r\n\r\n" + exception.Message);
            }
        }

        private int? RequestBootTimeout(int currentSeconds)
        {
            using (var dialog = new TimeoutDialog(currentSeconds))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return null;
                }

                return dialog.TimeoutSeconds;
            }
        }

        private void SetTimeoutDisplay(int seconds)
        {
            currentTimeoutSeconds = seconds;
            timeoutButton.Text = "启动等待：" + seconds + " 秒";
            timeoutButton.Enabled = true;
            interfaceToolTip.SetToolTip(
                timeoutButton,
                "当前启动菜单会等待 " + seconds + " 秒；点击可修改");
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
                    UiDialogs.ShowError(this, "保存备注失败", exception.Message);
                }
            }
        }

        private void UpdateRemarkRow(BootEntry entry)
        {
            foreach (BootRowViewModel row in bootRows)
            {
                if (row.Entry != entry)
                {
                    continue;
                }

                string remark = GetEntryRemark(entry);
                row.SetRemark(remark);
                break;
            }

            int selectedIndex = bootRows.IndexOf(selectedRow);
            bootEntriesTable.DataSource = bootRows.ToArray();
            if (selectedIndex >= 0)
            {
                SelectBootRow(bootRows[selectedIndex], true);
            }

            if (entry.IsDefault)
            {
                SetCurrentDefault(entry);
            }

            UpdateActionButtons();
        }

        private BootEntry GetSelectedEntry()
        {
            return selectedRow == null ? null : selectedRow.Entry;
        }

        private async void SetDefault(bool restartAfterSetting)
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
            DialogResult confirmation = UiDialogs.Confirm(
                this,
                "确认切换",
                "确认将“" + GetEntryConfirmationName(selectedEntry) + "”" + action + "吗？" + consequence,
                restartAfterSetting ? "切换并重启" : "设为默认");

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

                await LoadBootEntriesAsync();
                UiDialogs.ShowInfo(
                    this,
                    "默认启动系统已更新",
                    "已将“" + GetEntryConfirmationName(selectedEntry) + "”设为默认启动系统。");
            }
            catch (Exception exception)
            {
                string message = defaultWasSet
                    ? "默认启动系统已设置，但自动重启失败。请手动重启电脑。"
                    : "设置默认启动系统失败。";
                UiDialogs.ShowError(
                    this,
                    "切换失败",
                    message + "\r\n\r\n" + exception.Message);
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

        private sealed class BootRowViewModel
        {
            public BootRowViewModel(BootEntry entry, string remark)
            {
                Entry = entry;
                SystemName = entry.Description;
                Device = entry.Device;
                Status = entry.IsDefault ? "当前默认" : "可切换";
                SetRemark(remark);
            }

            public BootEntry Entry { get; private set; }

            public string SystemName { get; private set; }

            public string Device { get; private set; }

            public string Remark { get; set; }

            public bool HasRemark { get; private set; }

            public string Status { get; private set; }

            public bool IsDefault
            {
                get { return Entry.IsDefault; }
            }

            public void SetRemark(string remark)
            {
                HasRemark = !string.IsNullOrWhiteSpace(remark);
                Remark = HasRemark ? remark : "未设置";
            }
        }
    }
}
