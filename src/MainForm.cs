using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
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
        private AntTable bootEntriesTable;
        private AppleBootList appleBootList;
        private AnimatedLabel currentDefaultNameLabel;
        private AntTag currentDefaultDeviceTag;
        private AnimatedLabel entryCountLabel;
        private AnimatedLabel actionStatusLabel;
        private AnimatedLabel selectedNameLabel;
        private AnimatedLabel selectedRemarkLabel;
        private AntTag selectedDeviceTag;
        private AntTag selectedStateTag;
        private readonly ToolTip interfaceToolTip;
        private AntButton timeoutButton;
        private AntButton refreshButton;
        private AntButton editRemarkButton;
        private AntButton setDefaultButton;
        private AntButton setDefaultAndRestartButton;
        private AntButton announcementButton;
        private AntButton updateButton;
        private AntPanel defaultBand;
        private AntPanel selectionPanel;
        private Panel inspectorActionPanel;
        private AntPanel systemsWorkspacePanel;
        private Label bootMenuLabel;
        private Panel systemsTopDivider;
        private Panel systemsBottomDivider;
        private Panel legacyHeader;
        private PageTransitionPanel pageHost;
        private Panel systemsPage;
        private Panel settingsPage;
        private Panel announcementPage;
        private AntButton systemsNavigation;
        private AntButton settingsNavigation;
        private AntButton announcementNavigation;
        private Panel dashboardContent;
        private AntPanel systemListCard;
        private AntdUI.Input inlineRemarkInput;
        private BootRowViewModel inlineRemarkRow;
        private Label announcementTitleLabel;
        private Label announcementDateLabel;
        private Label updateStatusLabel;
        private RoundedLabel dashboardDefaultDeviceBadge;
        private readonly List<BootRowViewModel> bootRows = new List<BootRowViewModel>();
        private Icon applicationIcon;
        private Image applicationLogo;
        private BootRowViewModel selectedRow;
        private int currentTimeoutSeconds = -1;
        private int selectionGeneration;
        private bool isLoadingBootEntries;
        private UpdateInfo availableUpdate;
        private bool isCheckingForUpdates;
        private bool isClosing;

        private const int DwmWindowCornerPreference = 33;
        private const int DwmWindowCornerRound = 2;
        private const int DwmSystemBackdropType = 38;
        private const int DwmBackdropTransient = 3;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr window,
            int attribute,
            ref int value,
            int valueSize);

        public MainForm()
        {
            Text = "双系统快速切换";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1080, 700);
            MinimumSize = new Size(980, 660);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
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

            defaultBand = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
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
                Radius = UiTheme.BadgeCornerRadius,
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
            timeoutButton.Location = new Point(510, 212);
            timeoutButton.Size = new Size(140, 36);
            timeoutButton.AccessibleName = "修改启动菜单等待时间";
            timeoutButton.Enabled = false;
            timeoutButton.Click += delegate { EditBootTimeout(); };

            refreshButton = UiFactory.CreateButton("刷新启动项", 160, false);
            refreshButton.Location = new Point(658, 212);
            refreshButton.Size = new Size(160, 36);
            refreshButton.AccessibleName = "刷新启动项";
            refreshButton.Click += async delegate { await LoadBootEntriesAsync(); };

            announcementButton = UiFactory.CreateButton("公告", 66, false);
            announcementButton.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            announcementButton.Location = new Point(826, 212);
            announcementButton.Size = new Size(66, 36);
            announcementButton.AccessibleName = "查看公告";
            announcementButton.Click += delegate { ShowAnnouncement(); };
            interfaceToolTip.SetToolTip(announcementButton, "查看官方公告和使用提醒");

            bootEntriesTable = CreateBootEntriesTable();
            bootEntriesTable.Location = new Point(28, 258);
            bootEntriesTable.Size = new Size(570, 246);
            bootEntriesTable.AccessibleName = "Windows 启动系统列表";
            bootEntriesTable.AccessibleDescription = "选择要设为下次默认启动的 Windows 系统";
            bootEntriesTable.CellClick += OnBootEntryClick;
            bootEntriesTable.CellDoubleClick += OnBootEntryDoubleClick;
            bootEntriesTable.SelectIndexChanged += OnBootSelectionChanged;

            selectionPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
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
                Radius = UiTheme.BadgeCornerRadius,
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
                Radius = UiTheme.BadgeCornerRadius,
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
                Location = new Point(260, 565),
                Size = new Size(300, 42),
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
            Controls.Add(announcementButton);
            Controls.Add(entryCountLabel);
            Controls.Add(bootEntriesTable);
            Controls.Add(selectionPanel);
            Controls.Add(divider);
            Controls.Add(actionStatusLabel);
            Controls.Add(setDefaultButton);
            Controls.Add(setDefaultAndRestartButton);

            ApplyAppleDashboard();

            SetActionButtonsEnabled(false);
            FormClosing += delegate(object sender, FormClosingEventArgs eventArgs)
            {
                isClosing = true;
            };
            Shown += async delegate
            {
                ApplyNativeWindowCorners();
                await LoadBootEntriesAsync();
                if (isClosing || IsDisposed)
                {
                    return;
                }

                Task updateTask = CheckForUpdatesAsync();
                Task announcementTask = LoadAnnouncementSummaryAsync();
                await Task.WhenAll(updateTask, announcementTask);
            };
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
                Radius = UiTheme.BadgeCornerRadius,
                Location = new Point(766, 25),
                Size = new Size(126, 32),
                Text = "管理员权限已启用",
                TextAlign = ContentAlignment.MiddleCenter
            };

            updateButton = UiFactory.CreateButton("检查更新", 148, false);
            updateButton.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
            updateButton.Location = new Point(604, 25);
            updateButton.Size = new Size(148, 32);
            updateButton.AccessibleName = "检查云端更新";
            updateButton.Click += async delegate { await CheckOrApplyUpdateAsync(); };

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
            header.Controls.Add(updateButton);
            header.Controls.Add(accentLine);
            Controls.Add(header);
            legacyHeader = header;
        }

        private void ApplyAppleDashboard()
        {
            SuspendLayout();
            if (legacyHeader != null)
            {
                legacyHeader.Visible = false;
            }

            dashboardContent = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                    AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = false,
                BackColor = UiTheme.Canvas,
                Location = new Point(0, 0),
                Size = ClientSize
            };
            Controls.Add(dashboardContent);

            BuildDashboardContent();
            HideUnusedLegacyRootControls();
            dashboardContent.BringToFront();
            dashboardContent.Resize += delegate { LayoutAppleDashboard(); };
            LayoutAppleDashboard();
            ResumeLayout(true);
        }

        private void HideUnusedLegacyRootControls()
        {
            foreach (Control control in Controls)
            {
                if (!ReferenceEquals(control, dashboardContent) && control.Parent == this)
                {
                    control.Visible = false;
                }
            }
        }

        private void BuildDashboardContent()
        {
            BuildHeaderBanner();
            MoveCoreDashboardControls();
        }

        private void BuildHeaderBanner()
        {
            systemsWorkspacePanel = new AntPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Back = UiTheme.BannerStart,
                BackExtend = "135deg, " + ColorTranslator.ToHtml(UiTheme.BannerStart) + ", " +
                    ColorTranslator.ToHtml(UiTheme.BannerEnd),
                BorderWidth = 0F,
                Location = new Point(24, 20),
                Radius = UiTheme.WorkspaceCornerRadius,
                Shadow = 8,
                ShadowColor = Color.FromArgb(55, 85, 125),
                ShadowOffsetY = 2,
                ShadowOpacity = 0.08F,
                Size = new Size(832, 140)
            };

            defaultBand.Location = new Point(20, 18);
            defaultBand.Size = new Size(460, 104);
            defaultBand.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            defaultBand.Back = UiTheme.BannerStart;
            defaultBand.BorderWidth = 0F;
            defaultBand.Radius = 0;
            currentDefaultNameLabel.BackdropColor = UiTheme.BannerStart;
            currentDefaultNameLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            currentDefaultNameLabel.Location = new Point(0, 42);
            currentDefaultNameLabel.Size = new Size(350, 38);
            currentDefaultDeviceTag.Location = new Point(0, 72);
            currentDefaultDeviceTag.Size = new Size(92, 28);
            currentDefaultDeviceTag.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            currentDefaultDeviceTag.BackColor = Color.FromArgb(220, 234, 252);
            currentDefaultDeviceTag.Visible = false;
            dashboardDefaultDeviceBadge = new RoundedLabel
            {
                BackdropColor = UiTheme.BannerStart,
                FillColor = Color.FromArgb(220, 234, 252),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Location = new Point(250, 49),
                Size = new Size(64, 27),
                Text = "--"
            };
            defaultBand.Controls.Add(dashboardDefaultDeviceBadge);

            var bannerDivider = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
                BackColor = Color.FromArgb(207, 220, 237),
                Location = new Point(510, 18),
                Size = new Size(1, 104)
            };
            var announcementDot = new RoundedLabel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackdropColor = UiTheme.BannerEnd,
                FillColor = Color.FromArgb(255, 69, 58),
                Location = new Point(536, 25),
                Size = new Size(10, 10),
                Text = string.Empty
            };
            var announcementLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoSize = true,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(554, 20),
                Text = "项目动态"
            };
            announcementTitleLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(536, 54),
                Size = new Size(270, 25),
                Text = "正在同步最新公告...",
                TextAlign = ContentAlignment.MiddleLeft
            };
            announcementDateLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(536, 82),
                Size = new Size(270, 20),
                Text = "正在同步公告"
            };
            announcementTitleLabel.Click += delegate { ShowAnnouncement(); };
            announcementDateLabel.Click += delegate { ShowAnnouncement(); };

            announcementButton.Text = "查看公告";
            announcementButton.Size = new Size(96, 30);
            announcementButton.Visible = true;
            announcementButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            updateStatusLabel = new Label
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Size = new Size(190, 22),
                Text = "v" + version.ToString(3) + "  ·  正在检查更新",
                TextAlign = ContentAlignment.MiddleLeft
            };
            updateStatusLabel.Click += async delegate { await CheckOrApplyUpdateAsync(); };
            interfaceToolTip.SetToolTip(updateStatusLabel, "点击检查更新或安装可用版本");
            var announcementDivider = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(214, 225, 238),
                Location = new Point(536, 105),
                Size = new Size(260, 1)
            };

            systemsWorkspacePanel.Controls.Add(defaultBand);
            systemsWorkspacePanel.Controls.Add(bannerDivider);
            systemsWorkspacePanel.Controls.Add(announcementDot);
            systemsWorkspacePanel.Controls.Add(announcementLabel);
            systemsWorkspacePanel.Controls.Add(announcementTitleLabel);
            systemsWorkspacePanel.Controls.Add(announcementDateLabel);
            systemsWorkspacePanel.Controls.Add(announcementButton);
            systemsWorkspacePanel.Controls.Add(announcementDivider);
            systemsWorkspacePanel.Controls.Add(updateStatusLabel);
            dashboardContent.Controls.Add(systemsWorkspacePanel);
        }

        private void MoveCoreDashboardControls()
        {
            bootMenuLabel = FindLabel("选择下次启动系统");
            if (bootMenuLabel != null)
            {
                bootMenuLabel.Text = "引导系统";
                bootMenuLabel.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            }

            bootEntriesTable.Columns.Clear();
            var systemColumn = new AntdUI.Column("SystemName", "系统")
            {
                Ellipsis = true,
                Width = "48%",
                Style = new AntTable.CellStyleInfo { FontBold = true }
            };
            var remarkColumn = new AntdUI.Column("Remark", "用途")
            {
                Ellipsis = true,
                Width = "30%"
            };
            remarkColumn.Render = delegate(object value, object record, int rowIndex)
            {
                var row = record as BootRowViewModel;
                return row == null
                    ? value
                    : (object)new AntdUI.CellTag(row.Remark)
                    {
                        Back = row.HasRemark ? UiTheme.AccentSoft : UiTheme.Disabled,
                        BorderWidth = 0F,
                        Fore = row.HasRemark ? UiTheme.Accent : UiTheme.Muted
                    };
            };
            var statusColumn = new AntdUI.Column("Status", "状态", AntdUI.ColumnAlign.Center)
            {
                Width = "22%"
            };
            statusColumn.Render = delegate(object value, object record, int rowIndex)
            {
                var row = record as BootRowViewModel;
                return row == null
                    ? value
                    : (object)new AntdUI.CellText((row.IsDefault ? "●  " : "○  ") + row.Status)
                    {
                        Fore = row.IsDefault ? UiTheme.Success : UiTheme.Muted
                    };
            };
            bootEntriesTable.Columns.Add(systemColumn);
            bootEntriesTable.Columns.Add(remarkColumn);
            bootEntriesTable.Columns.Add(statusColumn);
            bootEntriesTable.ColumnBack = UiTheme.Surface;
            bootEntriesTable.BorderWidth = 0F;
            bootEntriesTable.RowHeight = 52;
            bootEntriesTable.RowHeightHeader = 34;
            bootEntriesTable.RowHoverBg = UiTheme.Hover;
            bootEntriesTable.RowSelectedBg = UiTheme.SelectionStrong;
            bootEntriesTable.CellDoubleClick -= OnBootEntryDoubleClick;
            bootEntriesTable.CellDoubleClick += OnInlineRemarkDoubleClick;

            appleBootList = new AppleBootList
            {
                AccessibleName = "Windows 启动系统列表",
                AccessibleDescription = "单击选择系统，双击编辑用途备注",
                BackColor = UiTheme.Surface
            };
            appleBootList.ItemSelected += OnAppleBootItemSelected;
            appleBootList.ItemDoubleClicked += OnAppleBootItemDoubleClicked;
            bootEntriesTable.Visible = false;

            inlineRemarkInput = new AntdUI.Input
            {
                BackColor = UiTheme.Surface,
                BorderActive = UiTheme.Primary,
                BorderColor = UiTheme.Border,
                BorderHover = UiTheme.Secondary,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
                MaxLength = 48,
                Radius = UiTheme.ControlCornerRadius,
                Size = new Size(210, 36),
                Visible = false
            };
            inlineRemarkInput.KeyDown += OnInlineRemarkKeyDown;
            inlineRemarkInput.LostFocus += delegate { CancelInlineRemarkEdit(); };

            selectionPanel.Back = UiTheme.Surface;
            selectionPanel.BorderWidth = 0F;
            selectionPanel.Radius = UiTheme.WorkspaceCornerRadius;
            selectionPanel.Shadow = 8;
            selectionPanel.ShadowColor = Color.FromArgb(55, 85, 125);
            selectionPanel.ShadowOffsetY = 2;
            selectionPanel.ShadowOpacity = 0.07F;
            SetSelectionPanelBackdrop(UiTheme.Surface);
            selectedNameLabel.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            selectedDeviceTag.TextAlign = ContentAlignment.MiddleCenter;
            editRemarkButton.Text = "编辑备注";
            editRemarkButton.Visible = false;
            setDefaultAndRestartButton.Text = "切换并重启";
            setDefaultAndRestartButton.Padding = Padding.Empty;
            setDefaultAndRestartButton.Text = "切换并重启";
            setDefaultAndRestartButton.IconSvg = null;

            var diskIcon = new Label
            {
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe MDL2 Assets", 25F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Primary,
                Location = new Point(20, 18),
                Size = new Size(46, 46),
                Text = "\uE7F4",
                TextAlign = ContentAlignment.MiddleCenter
            };
            selectionPanel.Controls.Add(diskIcon);

            systemListCard = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderWidth = 0F,
                Radius = UiTheme.WorkspaceCornerRadius,
                Shadow = 8,
                ShadowColor = Color.FromArgb(55, 85, 125),
                ShadowOffsetY = 2,
                ShadowOpacity = 0.07F
            };
            systemListCard.Controls.Add(bootMenuLabel);
            systemListCard.Controls.Add(entryCountLabel);
            systemListCard.Controls.Add(timeoutButton);
            systemListCard.Controls.Add(refreshButton);
            systemListCard.Controls.Add(appleBootList);
            systemListCard.Controls.Add(inlineRemarkInput);
            selectionPanel.Controls.Add(actionStatusLabel);
            inspectorActionPanel = new Panel
            {
                BackColor = UiTheme.Surface
            };
            inspectorActionPanel.Paint += delegate(object sender, PaintEventArgs eventArgs)
            {
                using (var pen = new Pen(UiTheme.Border, 1F))
                {
                    eventArgs.Graphics.DrawLine(pen, 0, 0, inspectorActionPanel.Width, 0);
                }
            };
            selectionPanel.Controls.Remove(actionStatusLabel);
            inspectorActionPanel.Controls.Add(actionStatusLabel);
            inspectorActionPanel.Controls.Add(setDefaultButton);
            inspectorActionPanel.Controls.Add(setDefaultAndRestartButton);
            selectionPanel.Controls.Add(inspectorActionPanel);

            dashboardContent.Controls.Add(systemListCard);
            dashboardContent.Controls.Add(selectionPanel);

            if (legacyHeader != null)
            {
                legacyHeader.Visible = false;
            }
            defaultBand.Visible = true;
            timeoutButton.Visible = true;
            updateButton.Visible = false;
        }

        private void LayoutAppleDashboard()
        {
            if (dashboardContent == null || dashboardContent.ClientSize.Width <= 0)
            {
                return;
            }

            int width = Math.Max(760, dashboardContent.ClientSize.Width);
            int usableWidth = width - 48;
            int cardGap = 16;
            int bannerHeight = 140;
            int coreTop = 20 + bannerHeight + cardGap;
            int availableCoreHeight = dashboardContent.ClientSize.Height - coreTop - 20;
            int coreHeight = Math.Max(296, availableCoreHeight);
            int leftWidth = (int)Math.Round((usableWidth - cardGap) * 0.64);
            int rightWidth = usableWidth - cardGap - leftWidth;

            dashboardContent.AutoScroll = availableCoreHeight < 296;
            dashboardContent.AutoScrollMinSize = dashboardContent.AutoScroll
                ? new Size(0, coreTop + 316)
                : Size.Empty;
            systemsWorkspacePanel.Location = new Point(24, 20);
            systemsWorkspacePanel.Size = new Size(usableWidth, bannerHeight);
            LayoutDashboardBanner(usableWidth);

            systemListCard.Location = new Point(24, coreTop);
            systemListCard.Size = new Size(leftWidth, coreHeight);
            bootMenuLabel.Location = new Point(20, 16);
            entryCountLabel.Location = new Point(20, 43);
            entryCountLabel.Size = new Size(110, 24);
            entryCountLabel.BackdropColor = UiTheme.Surface;
            refreshButton.Text = "刷新";
            refreshButton.IconSvg = null;
            refreshButton.Padding = Padding.Empty;
            timeoutButton.Location = new Point(leftWidth - 222, 20);
            timeoutButton.Size = new Size(122, 38);
            refreshButton.Location = new Point(leftWidth - 90, 20);
            refreshButton.Size = new Size(70, 38);
            appleBootList.Location = new Point(18, 76);
            appleBootList.Size = new Size(leftWidth - 36, coreHeight - 92);

            selectionPanel.Location = new Point(24 + leftWidth + cardGap, coreTop);
            selectionPanel.Size = new Size(rightWidth, coreHeight);
            LayoutDashboardInspector(rightWidth, coreHeight);
        }

        private void LayoutDashboardBanner(int width)
        {
            int announcementWidth = Math.Max(400, (int)Math.Round(width * 0.45));
            int dividerX = width - announcementWidth;
            defaultBand.Width = Math.Max(320, dividerX - 40);
            currentDefaultNameLabel.Width = Math.Max(220, defaultBand.Width - 20);
            string currentName = currentDefaultNameLabel.Text ?? string.Empty;
            int nameWidth = TextRenderer.MeasureText(
                currentName,
                currentDefaultNameLabel.Font,
                Size.Empty,
                TextFormatFlags.NoPadding).Width;
            int badgeLeft = Math.Min(defaultBand.Width - 76, Math.Max(190, nameWidth + 14));
            dashboardDefaultDeviceBadge.Location = new Point(badgeLeft, 49);
            currentDefaultNameLabel.Width = Math.Max(160, badgeLeft - 8);
            foreach (Control control in systemsWorkspacePanel.Controls)
            {
                if (control.Width == 1)
                {
                    bool isSectionDivider = control.Height > 1;
                    if (isSectionDivider)
                    {
                        control.Location = new Point(dividerX, 18);
                        control.Height = systemsWorkspacePanel.Height - 36;
                    }
                }
            }

            int sectionLeft = dividerX + 28;
            int sectionRight = width - 20;
            foreach (Control control in systemsWorkspacePanel.Controls)
            {
                var rounded = control as RoundedLabel;
                if (rounded != null && rounded.Size == new Size(10, 10))
                {
                    rounded.Location = new Point(sectionLeft, 24);
                }
                var label = control as Label;
                if (label != null && label.Text == "项目动态")
                {
                    label.Location = new Point(sectionLeft + 18, 19);
                }
                if (control is Panel && control.Height == 1)
                {
                    control.Location = new Point(sectionLeft, 105);
                    control.Width = Math.Max(180, sectionRight - sectionLeft);
                }
            }
            announcementButton.Location = new Point(sectionRight - announcementButton.Width, 15);
            announcementTitleLabel.Location = new Point(sectionLeft, 51);
            announcementTitleLabel.Size = new Size(Math.Max(200, sectionRight - sectionLeft), 24);
            announcementDateLabel.Location = new Point(sectionLeft, 79);
            announcementDateLabel.Size = new Size(Math.Max(200, sectionRight - sectionLeft), 20);
            updateStatusLabel.Location = new Point(sectionLeft, 110);
            updateStatusLabel.Size = new Size(Math.Max(180, sectionRight - sectionLeft), 21);
        }

        private void LayoutDashboardInspector(int width, int height)
        {
            foreach (Control control in selectionPanel.Controls)
            {
                var label = control as Label;
                if (label != null && label.Text == "当前选择")
                {
                    label.Location = new Point(76, 16);
                }
                if (label != null && label.Text == "用途备注")
                {
                    label.Location = new Point(20, 139);
                }
            }

            selectedNameLabel.Location = new Point(72, 39);
            selectedNameLabel.Size = new Size(Math.Max(120, width - 92), 32);
            selectedDeviceTag.Location = new Point(20, 84);
            selectedStateTag.Location = new Point(106, 84);
            foreach (Control control in selectionPanel.Controls)
            {
                if (control is Panel && control.Height == 1)
                {
                    control.Location = new Point(20, 126);
                    control.Width = width - 40;
                }
            }

            selectedRemarkLabel.Location = new Point(20, 158);
            selectedRemarkLabel.Size = new Size(width - 40, 26);
            inspectorActionPanel.Location = new Point(14, height - 100);
            inspectorActionPanel.Size = new Size(width - 28, 88);
            inspectorActionPanel.BringToFront();
            actionStatusLabel.BackdropColor = UiTheme.Surface;
            actionStatusLabel.Location = new Point(6, 5);
            actionStatusLabel.Size = new Size(inspectorActionPanel.Width - 12, 23);
            actionStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            setDefaultButton.Location = new Point(6, 34);
            setDefaultButton.Size = new Size(Math.Max(96, (inspectorActionPanel.Width - 24) / 2), 40);
            setDefaultAndRestartButton.Location = new Point(setDefaultButton.Right + 12, 34);
            setDefaultAndRestartButton.Size = new Size(
                inspectorActionPanel.Width - setDefaultButton.Right - 18,
                40);
        }

        private async Task LoadAnnouncementSummaryAsync()
        {
            try
            {
                AnnouncementInfo announcement = await Task.Run((Func<AnnouncementInfo>)UpdateService.LoadAnnouncement);
                if (isClosing || IsDisposed)
                {
                    return;
                }

                string title = ExtractAnnouncementTitle(announcement.Content);
                announcementTitleLabel.Text = title;
                announcementDateLabel.Text = ExtractAnnouncementDate(announcement.Content) ??
                    (announcement.IsRemote
                        ? "已从项目主页同步"
                        : "当前显示内置公告");
                interfaceToolTip.SetToolTip(announcementTitleLabel, title + "，点击查看完整公告");
            }
            catch
            {
                if (!isClosing && !IsDisposed)
                {
                    announcementTitleLabel.Text = "公告暂时无法同步";
                    announcementDateLabel.Text = "当前显示内置公告";
                }
            }
        }

        private static string ExtractAnnouncementTitle(string markdown)
        {
            List<AnnouncementBlock> blocks = AnnouncementParser.Parse(markdown);
            foreach (AnnouncementBlock block in blocks)
            {
                if ((block.Kind == AnnouncementBlockKind.Subtitle ||
                    block.Kind == AnnouncementBlockKind.Title) &&
                    !string.IsNullOrWhiteSpace(block.Text) &&
                    !string.Equals(block.Text, "软件公告", StringComparison.OrdinalIgnoreCase))
                {
                    return block.Text;
                }
            }

            return "查看最新项目动态";
        }

        private static string ExtractAnnouncementDate(string markdown)
        {
            Match match = Regex.Match(markdown ?? string.Empty, @"\b20\d{2}[-/.]\d{1,2}[-/.]\d{1,2}\b");
            return match.Success ? "发布于 " + match.Value : null;
        }

        private void OnInlineRemarkDoubleClick(object sender, AntdUI.TableClickEventArgs eventArgs)
        {
            var row = eventArgs.Record as BootRowViewModel;
            if (row == null)
            {
                return;
            }

            SelectBootRow(row, false);
            inlineRemarkRow = row;
            inlineRemarkInput.Text = GetEntryRemark(row.Entry);
            int rowIndex = Math.Max(0, bootRows.IndexOf(row));
            int x = bootEntriesTable.Left + (int)Math.Round(bootEntriesTable.Width * 0.48);
            int rowHeight = bootEntriesTable.RowHeight ?? 52;
            int y = bootEntriesTable.Top + 34 + (rowIndex * rowHeight) + 8;
            inlineRemarkInput.Location = new Point(x, y);
            inlineRemarkInput.Width = Math.Max(120, (int)Math.Round(bootEntriesTable.Width * 0.30) - 10);
            inlineRemarkInput.Visible = true;
            inlineRemarkInput.BringToFront();
            inlineRemarkInput.Focus();
            inlineRemarkInput.SelectAll();
        }

        private void OnAppleBootItemSelected(object sender, AppleBootListEventArgs eventArgs)
        {
            var row = eventArgs.Item == null ? null : eventArgs.Item.Tag as BootRowViewModel;
            if (row != null)
            {
                SelectBootRow(row, false);
            }
        }

        private void OnAppleBootItemDoubleClicked(object sender, AppleBootListEventArgs eventArgs)
        {
            var row = eventArgs.Item == null ? null : eventArgs.Item.Tag as BootRowViewModel;
            if (row == null)
            {
                return;
            }

            SelectBootRow(row, false);
            BeginInlineRemarkEdit(row, eventArgs.Index);
        }

        private void BeginInlineRemarkEdit(BootRowViewModel row, int rowIndex)
        {
            inlineRemarkRow = row;
            inlineRemarkInput.Text = GetEntryRemark(row.Entry);
            Rectangle remarkBounds = appleBootList.GetRemarkBounds(rowIndex);
            inlineRemarkInput.Location = new Point(
                appleBootList.Left + remarkBounds.Left,
                appleBootList.Top + remarkBounds.Top);
            inlineRemarkInput.Size = remarkBounds.Size;
            inlineRemarkInput.Visible = true;
            inlineRemarkInput.BringToFront();
            inlineRemarkInput.Focus();
            inlineRemarkInput.SelectAll();
        }

        private void OnInlineRemarkKeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                SaveInlineRemarkEdit();
                eventArgs.SuppressKeyPress = true;
            }
            else if (eventArgs.KeyCode == Keys.Escape)
            {
                CancelInlineRemarkEdit();
                eventArgs.SuppressKeyPress = true;
            }
        }

        private void SaveInlineRemarkEdit()
        {
            if (inlineRemarkRow == null)
            {
                CancelInlineRemarkEdit();
                return;
            }

            BootEntry entry = inlineRemarkRow.Entry;
            try
            {
                BootRemarkStore.Set(entry.Identifier, inlineRemarkInput.Text.Trim());
                CancelInlineRemarkEdit();
                UpdateRemarkRow(entry);
            }
            catch (Exception exception)
            {
                UiDialogs.ShowError(this, "保存备注失败", exception.Message);
            }
        }

        private void CancelInlineRemarkEdit()
        {
            if (inlineRemarkInput == null)
            {
                return;
            }

            inlineRemarkInput.Visible = false;
            inlineRemarkRow = null;
        }

        private void ApplyOrbienShell()
        {
            SuspendLayout();
            if (legacyHeader != null)
            {
                legacyHeader.Visible = false;
            }

            systemsPage = CreatePage("\uE7F4", "系统切换", "选择下次启动的 Windows 系统");
            settingsPage = CreatePage("\uE713", "设置", "启动菜单与软件更新");
            announcementPage = CreatePage("\uE8A5", "公告", "软件动态与版本信息");

            MoveSystemsControls();
            BuildSettingsPage();
            BuildAnnouncementPage();

            pageHost = new PageTransitionPanel
            {
                BackColor = UiTheme.Canvas,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                    AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(72, 0),
                Size = new Size(ClientSize.Width - 72, ClientSize.Height)
            };
            pageHost.Controls.Add(systemsPage);
            pageHost.Controls.Add(settingsPage);
            pageHost.Controls.Add(announcementPage);

            Panel sidebar = BuildSidebar();
            Controls.Add(pageHost);
            Controls.Add(sidebar);
            sidebar.BringToFront();
            pageHost.ShowPage(systemsPage);
            SetActiveNavigation(systemsNavigation);
            ResumeLayout(true);
        }

        private Panel CreatePage(string glyph, string title, string subtitle)
        {
            var page = new Panel
            {
                BackColor = UiTheme.Canvas,
                Size = new Size(1008, 700),
                Visible = false
            };
            var titleIcon = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe MDL2 Assets", 14F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Primary,
                Location = new Point(24, 19),
                Size = new Size(26, 27),
                Text = glyph,
                TextAlign = ContentAlignment.MiddleCenter
            };
            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Primary,
                Location = new Point(56, 20),
                Text = title
            };
            var subtitleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(57, 51),
                Text = subtitle
            };
            page.Controls.Add(titleIcon);
            page.Controls.Add(titleLabel);
            page.Controls.Add(subtitleLabel);
            return page;
        }

        private Panel BuildSidebar()
        {
            var sidebar = new Panel
            {
                BackColor = UiTheme.Header,
                Dock = DockStyle.Left,
                Padding = new Padding(14, 14, 14, 12),
                Width = 72
            };
            sidebar.Paint += delegate(object sender, PaintEventArgs eventArgs)
            {
                using (var pen = new Pen(UiTheme.Border, 1F))
                {
                    eventArgs.Graphics.DrawLine(pen, sidebar.ClientSize.Width - 1, 0,
                        sidebar.ClientSize.Width - 1, sidebar.ClientSize.Height);
                }
            };

            var logo = new HighQualityImageControl
            {
                BackColor = UiTheme.Header,
                Image = applicationLogo,
                Location = new Point(19, 14),
                Size = new Size(34, 34)
            };
            systemsNavigation = CreateNavigationButton("\uE7F4", "系统切换", 76);
            settingsNavigation = CreateNavigationButton("\uE713", "设置", 180);
            announcementNavigation = CreateNavigationButton("\uE8A5", "公告", 232);

            systemsNavigation.Click += delegate { NavigateTo(systemsPage, systemsNavigation); };
            settingsNavigation.Click += delegate { NavigateTo(settingsPage, settingsNavigation); };
            announcementNavigation.Click += delegate { NavigateTo(announcementPage, announcementNavigation); };

            var adminTag = new RoundedLabel
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                BackdropColor = UiTheme.Header,
                FillColor = UiTheme.AccentSoft,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Location = new Point(14, ClientSize.Height - 62),
                Size = new Size(44, 28),
                Text = "管理"
            };
            interfaceToolTip.SetToolTip(adminTag, "管理员权限已启用");

            sidebar.Controls.Add(logo);
            sidebar.Controls.Add(systemsNavigation);
            sidebar.Controls.Add(settingsNavigation);
            sidebar.Controls.Add(announcementNavigation);
            sidebar.Controls.Add(adminTag);
            return sidebar;
        }

        private AntButton CreateNavigationButton(
            string glyph,
            string accessibleName,
            int top)
        {
            var button = new AntButton
            {
                AccessibleName = accessibleName,
                AutoEllipsis = false,
                BackActive = UiTheme.SelectionStrong,
                BackColor = UiTheme.Header,
                BackHover = UiTheme.AccentSoft,
                BorderWidth = 0F,
                Font = new Font("Segoe MDL2 Assets", 13F, FontStyle.Regular, GraphicsUnit.Point),
                ForeActive = UiTheme.Accent,
                ForeColor = UiTheme.Muted,
                ForeHover = UiTheme.Accent,
                Location = new Point(14, top),
                Radius = UiTheme.ControlCornerRadius,
                Size = new Size(44, 44),
                Text = glyph,
                WaveSize = 0
            };
            interfaceToolTip.SetToolTip(button, accessibleName);
            return button;
        }

        private void NavigateTo(Panel page, AntButton navigation)
        {
            pageHost.ShowPage(page);
            SetActiveNavigation(navigation);
        }

        private void SetActiveNavigation(AntButton activeNavigation)
        {
            ApplyNavigationState(systemsNavigation, ReferenceEquals(activeNavigation, systemsNavigation));
            ApplyNavigationState(settingsNavigation, ReferenceEquals(activeNavigation, settingsNavigation));
            ApplyNavigationState(announcementNavigation, ReferenceEquals(activeNavigation, announcementNavigation));
        }

        private static void ApplyNavigationState(AntButton navigation, bool active)
        {
            navigation.BackColor = active ? UiTheme.AccentSoft : UiTheme.Header;
            navigation.ForeColor = active ? UiTheme.Accent : UiTheme.Muted;
        }

        private void MoveSystemsControls()
        {
            systemsWorkspacePanel = new AntPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                    AnchorStyles.Left | AnchorStyles.Right,
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Location = new Point(24, 82),
                Radius = UiTheme.WorkspaceCornerRadius,
                Shadow = 8,
                ShadowColor = Color.FromArgb(30, 58, 95),
                ShadowOffsetY = 2,
                ShadowOpacity = 0.06F,
                Size = new Size(960, 594)
            };

            defaultBand.Location = new Point(20, 16);
            defaultBand.Size = new Size(920, 78);
            defaultBand.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            defaultBand.Back = UiTheme.Surface;
            defaultBand.BorderWidth = 0F;
            defaultBand.Radius = 0;
            currentDefaultNameLabel.Location = new Point(0, 31);
            currentDefaultNameLabel.Size = new Size(690, 34);
            currentDefaultDeviceTag.Location = new Point(784, 25);
            currentDefaultDeviceTag.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            currentDefaultDeviceTag.Radius = UiTheme.BadgeCornerRadius;
            defaultBand.Resize += delegate
            {
                currentDefaultNameLabel.Width = Math.Max(
                    180,
                    currentDefaultDeviceTag.Left - currentDefaultNameLabel.Left - 20);
            };

            bootMenuLabel = FindLabel("选择下次启动系统");
            if (bootMenuLabel != null)
            {
                bootMenuLabel.BackColor = UiTheme.Surface;
                bootMenuLabel.Location = new Point(20, 119);
                bootMenuLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
                systemsWorkspacePanel.Controls.Add(bootMenuLabel);
            }

            systemsTopDivider = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = UiTheme.Border,
                Location = new Point(20, 104),
                Size = new Size(920, 1)
            };

            entryCountLabel.Location = new Point(514, 117);
            entryCountLabel.Size = new Size(150, 24);
            entryCountLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            entryCountLabel.BackdropColor = UiTheme.Surface;

            refreshButton.Location = new Point(780, 111);
            refreshButton.Size = new Size(140, 36);
            refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            bootEntriesTable.Location = new Point(20, 160);
            bootEntriesTable.Size = new Size(620, 350);
            bootEntriesTable.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            bootEntriesTable.Radius = UiTheme.SurfaceCornerRadius;

            selectionPanel.Location = new Point(656, 160);
            selectionPanel.Size = new Size(284, 350);
            selectionPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            selectionPanel.Back = UiTheme.Header;
            selectionPanel.BorderWidth = 0F;
            selectionPanel.Radius = UiTheme.InspectorCornerRadius;
            SetSelectionPanelBackdrop(UiTheme.Header);
            ResizeSelectionPanel();
            selectionPanel.Resize += delegate { ResizeSelectionPanel(); };

            systemsBottomDivider = new Panel
            {
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = UiTheme.Border,
                Location = new Point(20, 524),
                Size = new Size(920, 1)
            };

            actionStatusLabel.Location = new Point(20, 536);
            actionStatusLabel.Size = new Size(460, 48);
            actionStatusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            actionStatusLabel.BackdropColor = UiTheme.Surface;

            setDefaultButton.Location = new Point(646, 539);
            setDefaultButton.Size = new Size(136, 42);
            setDefaultButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            setDefaultAndRestartButton.Location = new Point(794, 539);
            setDefaultAndRestartButton.Size = new Size(146, 42);
            setDefaultAndRestartButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            systemsWorkspacePanel.Controls.Add(defaultBand);
            systemsWorkspacePanel.Controls.Add(systemsTopDivider);
            systemsWorkspacePanel.Controls.Add(entryCountLabel);
            systemsWorkspacePanel.Controls.Add(refreshButton);
            systemsWorkspacePanel.Controls.Add(bootEntriesTable);
            systemsWorkspacePanel.Controls.Add(selectionPanel);
            systemsWorkspacePanel.Controls.Add(systemsBottomDivider);
            systemsWorkspacePanel.Controls.Add(actionStatusLabel);
            systemsWorkspacePanel.Controls.Add(setDefaultButton);
            systemsWorkspacePanel.Controls.Add(setDefaultAndRestartButton);
            systemsPage.Controls.Add(systemsWorkspacePanel);
            systemsWorkspacePanel.Resize += delegate { LayoutSystemsWorkspace(); };
            LayoutSystemsWorkspace();

            HideLegacySystemControls();
        }

        private Label FindLabel(string text)
        {
            foreach (Control control in Controls)
            {
                var label = control as Label;
                if (label != null && label.Text == text)
                {
                    return label;
                }
            }

            return null;
        }

        private void HideLegacySystemControls()
        {
            foreach (Control control in Controls)
            {
                if (ReferenceEquals(control, legacyHeader) ||
                    ReferenceEquals(control, announcementButton) ||
                    ReferenceEquals(control, timeoutButton) ||
                    ReferenceEquals(control, updateButton))
                {
                    continue;
                }

                if (control.Parent == this)
                {
                    control.Visible = false;
                }
            }
        }

        private void ResizeSelectionPanel()
        {
            int width = selectionPanel.ClientSize.Width;
            int height = selectionPanel.ClientSize.Height;
            selectedNameLabel.Size = new Size(Math.Max(120, width - 40), 30);
            selectedRemarkLabel.Size = new Size(Math.Max(120, width - 40), 40);
            editRemarkButton.Location = new Point(20, Math.Max(198, height - 54));
            editRemarkButton.Size = new Size(Math.Max(120, width - 40), 34);
        }

        private void LayoutSystemsWorkspace()
        {
            if (systemsWorkspacePanel == null || systemsWorkspacePanel.ClientSize.Width <= 0)
            {
                return;
            }

            int width = systemsWorkspacePanel.ClientSize.Width;
            int height = systemsWorkspacePanel.ClientSize.Height;
            int innerWidth = Math.Max(480, width - 40);
            int detailWidth = Math.Min(300, Math.Max(264, innerWidth / 3));
            int tableWidth = Math.Max(300, innerWidth - detailWidth - 16);
            int contentHeight = Math.Max(220, height - 244);

            defaultBand.Width = innerWidth;
            systemsTopDivider.Width = innerWidth;
            systemsBottomDivider.Location = new Point(20, height - 70);
            systemsBottomDivider.Width = innerWidth;
            entryCountLabel.Location = new Point(width - 446, 117);
            refreshButton.Location = new Point(width - 160, 111);
            bootEntriesTable.Size = new Size(tableWidth, contentHeight);
            selectionPanel.Location = new Point(20 + tableWidth + 16, 160);
            selectionPanel.Size = new Size(detailWidth, contentHeight);
            actionStatusLabel.Location = new Point(20, height - 58);
            actionStatusLabel.Width = Math.Max(220, width - 500);
            setDefaultButton.Location = new Point(width - 314, height - 55);
            setDefaultAndRestartButton.Location = new Point(width - 166, height - 55);
        }

        private void SetSelectionPanelBackdrop(Color color)
        {
            selectedNameLabel.BackdropColor = color;
            selectedRemarkLabel.BackdropColor = color;
            foreach (Control control in selectionPanel.Controls)
            {
                if (control is Label)
                {
                    control.BackColor = color;
                }
            }
        }

        private void ApplyNativeWindowCorners()
        {
            if (!IsHandleCreated)
            {
                return;
            }

            try
            {
                int preference = DwmWindowCornerRound;
                DwmSetWindowAttribute(
                    Handle,
                    DwmWindowCornerPreference,
                    ref preference,
                    sizeof(int));
                int backdrop = DwmBackdropTransient;
                DwmSetWindowAttribute(
                    Handle,
                    DwmSystemBackdropType,
                    ref backdrop,
                    sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        private void BuildSettingsPage()
        {
            var bootPanel = CreateFeaturePanel(
                "启动菜单",
                "修改开机时等待选择系统的时间",
                82,
                154);
            timeoutButton.Location = new Point(760, 55);
            timeoutButton.Size = new Size(176, 40);
            timeoutButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bootPanel.Controls.Add(timeoutButton);

            var updatePanel = CreateFeaturePanel(
                "软件更新",
                "自动检查正式版本，也可随时手动检查",
                252,
                154);
            updateButton.Location = new Point(760, 55);
            updateButton.Size = new Size(176, 40);
            updateButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            updatePanel.Controls.Add(updateButton);

            settingsPage.Controls.Add(bootPanel);
            settingsPage.Controls.Add(updatePanel);
        }

        private void BuildAnnouncementPage()
        {
            var panel = CreateFeaturePanel(
                "软件公告",
                "从项目主页同步文字、图片和版本动态",
                82,
                154);
            announcementButton.Text = "查看公告";
            announcementButton.Location = new Point(784, 55);
            announcementButton.Size = new Size(152, 40);
            announcementButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel.Controls.Add(announcementButton);
            announcementPage.Controls.Add(panel);
        }

        private AntPanel CreateFeaturePanel(string title, string subtitle, int top, int height)
        {
            var panel = new AntPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Location = new Point(24, top),
                Radius = UiTheme.WorkspaceCornerRadius,
                Shadow = 8,
                ShadowColor = Color.FromArgb(30, 58, 95),
                ShadowOffsetY = 2,
                ShadowOpacity = 0.06F,
                Size = new Size(960, height)
            };
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(22, 24),
                Text = title
            });
            panel.Controls.Add(new Label
            {
                AutoSize = true,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(23, 56),
                Text = subtitle
            });
            return panel;
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
                Radius = UiTheme.SurfaceCornerRadius,
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

        private async Task CheckOrApplyUpdateAsync()
        {
            if (availableUpdate == null)
            {
                await CheckForUpdatesAsync(true);
                return;
            }

            DialogResult confirmation = UiDialogs.Confirm(
                this,
                "安装云端更新",
                "发现新版本 " + availableUpdate.Tag + "。下载完成并校验后，程序将自动关闭、替换并重新启动。",
                "下载并更新");
            if (confirmation != DialogResult.OK)
            {
                return;
            }

            updateButton.Enabled = false;
            updateButton.Loading = true;
            updateButton.Text = "正在下载 " + availableUpdate.Tag;
            try
            {
                UpdateInfo update = availableUpdate;
                string downloadedPath = await Task.Run(
                    () => UpdateService.DownloadAndVerify(update, CancellationToken.None));
                updateButton.Text = "正在安装更新";
                UpdateService.ReplaceAndRestart(downloadedPath);
                Close();
            }
            catch (Exception exception)
            {
                updateButton.Loading = false;
                updateButton.Enabled = true;
                updateButton.Text = "发现 " + availableUpdate.Tag + " 更新";
                UiDialogs.ShowError(
                    this,
                    "云端更新失败",
                    "没有修改当前程序。请检查网络连接后重试。\r\n\r\n" + exception.Message);
            }
        }

        private async Task CheckForUpdatesAsync(bool showResult = false)
        {
            if (isCheckingForUpdates)
            {
                return;
            }

            isCheckingForUpdates = true;
            updateButton.Enabled = false;
            updateButton.Loading = true;
            updateButton.Text = "正在检查更新";
            if (updateStatusLabel != null)
            {
                updateStatusLabel.Text = "正在静默检查更新";
            }
            try
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                availableUpdate = await Task.Run(() => UpdateService.CheckLatest(currentVersion));
                if (isClosing || IsDisposed)
                {
                    return;
                }

                updateButton.Loading = false;
                updateButton.Enabled = true;
                if (availableUpdate == null)
                {
                    updateButton.Text = "检查更新";
                    if (updateStatusLabel != null)
                    {
                        updateStatusLabel.Text = "v" + currentVersion.ToString(3) + "  ·  已是最新版本";
                    }
                    interfaceToolTip.SetToolTip(
                        updateButton,
                        "已自动检查，当前版本 v" + currentVersion.ToString(3) + "。点击可立即手动检查。");
                    if (showResult)
                    {
                        UiDialogs.ShowInfo(this, "检查更新", "当前已是最新正式版本。");
                    }
                }
                else
                {
                    updateButton.Text = "发现 " + availableUpdate.Tag + " 更新";
                    if (updateStatusLabel != null)
                    {
                        updateStatusLabel.Text = "发现 " + availableUpdate.Tag + " 更新  ·  点击安装";
                        updateStatusLabel.ForeColor = UiTheme.Accent;
                        updateStatusLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
                    }
                    interfaceToolTip.SetToolTip(updateButton, "点击下载并安装 " + availableUpdate.Tag);
                }
            }
            catch (Exception exception)
            {
                if (isClosing || IsDisposed)
                {
                    return;
                }

                updateButton.Loading = false;
                updateButton.Enabled = true;
                updateButton.Text = "检查更新";
                if (updateStatusLabel != null)
                {
                    updateStatusLabel.Text = "更新检查失败  ·  点击重试";
                }
                interfaceToolTip.SetToolTip(updateButton, "连接 GitHub 检查最新正式版本");
                if (showResult)
                {
                    UiDialogs.ShowError(
                        this,
                        "检查更新失败",
                        "无法连接更新服务器。当前程序可以继续正常使用。\r\n\r\n" + exception.Message);
                }
            }
            finally
            {
                isCheckingForUpdates = false;
            }
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
            if (appleBootList != null)
            {
                appleBootList.Enabled = false;
            }
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
                if (isClosing || IsDisposed)
                {
                    return;
                }

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
                UpdateAppleBootListItems();
                entryCountLabel.Text = bootEntries.Count + " 个可用系统";
                SetCurrentDefault(defaultEntry);
                SelectInitialTarget(firstSwitchableRow);
            }
            catch (Exception exception)
            {
                if (isClosing || IsDisposed)
                {
                    return;
                }

                currentDefaultNameLabel.Text = "无法读取启动项";
                currentDefaultDeviceTag.Visible = false;
                timeoutButton.Text = "启动等待：读取失败";
                timeoutButton.Enabled = false;
                actionStatusLabel.Text = "请检查 Windows 引导配置或系统接口兼容性";
                UiDialogs.ShowError(
                    this,
                    "读取启动配置失败",
                    "无法解析 Windows 引导配置。程序已请求管理员权限；请检查引导存储或系统接口兼容性。\r\n\r\n" + exception.Message);
            }
            finally
            {
                isLoadingBootEntries = false;
                if (!isClosing && !IsDisposed)
                {
                    refreshButton.Loading = false;
                    refreshButton.Enabled = true;
                    bootEntriesTable.Enabled = true;
                    if (appleBootList != null)
                    {
                        appleBootList.Enabled = true;
                    }
                    UseWaitCursor = false;
                    UpdateActionButtons();
                }
            }
        }

        private void ShowAnnouncement()
        {
            using (var dialog = new AnnouncementDialog(this))
            {
                dialog.ShowDialog(this);
            }
        }

        private void SetCurrentDefault(BootEntry defaultEntry)
        {
            if (defaultEntry == null)
            {
                currentDefaultNameLabel.Text = "未识别默认系统";
                currentDefaultDeviceTag.Visible = false;
                if (dashboardDefaultDeviceBadge != null)
                {
                    dashboardDefaultDeviceBadge.Visible = false;
                }
                interfaceToolTip.SetToolTip(currentDefaultNameLabel, string.Empty);
                interfaceToolTip.SetToolTip(currentDefaultDeviceTag, string.Empty);
                return;
            }

            string displayName = GetEntryDisplayName(defaultEntry);
            currentDefaultNameLabel.Text = displayName;
            currentDefaultDeviceTag.Text = defaultEntry.Device;
            currentDefaultDeviceTag.Visible = dashboardDefaultDeviceBadge == null;
            if (dashboardDefaultDeviceBadge != null)
            {
                dashboardDefaultDeviceBadge.Text = defaultEntry.Device;
                dashboardDefaultDeviceBadge.Visible = true;
                LayoutDashboardBanner(systemsWorkspacePanel.Width);
            }
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
                if (appleBootList != null)
                {
                    appleBootList.SelectedIndex = rowIndex;
                }
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

            interfaceToolTip.SetToolTip(
                setDefaultAndRestartButton,
                selectedEntry.IsDefault
                    ? "当前已在此系统下运行"
                    : "将选中系统设为默认，并在确认后立即重启");

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
            UpdateAppleBootListItems();
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

        private void UpdateAppleBootListItems()
        {
            if (appleBootList == null)
            {
                return;
            }

            var items = new List<AppleBootListItem>();
            foreach (BootRowViewModel row in bootRows)
            {
                items.Add(new AppleBootListItem
                {
                    Tag = row,
                    Name = row.SystemName,
                    Remark = row.HasRemark ? row.Remark : string.Empty,
                    Status = row.Status,
                    IsDefault = row.IsDefault
                });
            }

            appleBootList.SetItems(items);
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
