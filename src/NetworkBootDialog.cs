using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntButton = AntdUI.Button;
using AntPanel = AntdUI.Panel;
using AntTag = AntdUI.Tag;

namespace DualBootSwitcher
{
    internal sealed class NetworkBootDialog : StyledDialogForm
    {
        private readonly AntTag statusTag;
        private readonly Label statusDescriptionLabel;
        private readonly Label nameValueLabel;
        private readonly Label typeValueLabel;
        private readonly Label identifierValueLabel;
        private readonly Label deviceValueLabel;
        private readonly Label pathValueLabel;
        private readonly AntButton detectButton;
        private readonly AntButton startButton;
        private readonly ToolTip detailToolTip;
        private FirmwareBootEntry selectedEntry;
        private bool isDetecting;
        private bool isClosing;
        private bool isApplyingNetworkBoot;

        public NetworkBootDialog(Form owner)
        {
            Text = "检测网维无盘";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 596);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = owner == null ? null : owner.Icon;

            detailToolTip = new ToolTip
            {
                AutoPopDelay = 12000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true
            };

            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(28, 24),
                Text = "网维无盘启动"
            };

            var subtitleLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(29, 55),
                Size = new Size(660, 22),
                Text = "手动读取当前电脑的 UEFI 网络启动项，确认参数后再启动。"
            };

            var statusPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
                Location = new Point(28, 91),
                Size = new Size(664, 78)
            };

            statusTag = new AntTag
            {
                AutoSize = false,
                BackColor = UiTheme.AccentSoft,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Radius = UiTheme.BadgeCornerRadius,
                Location = new Point(18, 19),
                Size = new Size(112, 34),
                Text = "准备检测",
                TextAlign = ContentAlignment.MiddleCenter,
                Type = AntdUI.TTypeMini.Primary
            };

            statusDescriptionLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(148, 18),
                Size = new Size(492, 38),
                Text = "正在等待读取固件启动信息",
                TextAlign = ContentAlignment.MiddleLeft
            };

            statusPanel.Controls.Add(statusTag);
            statusPanel.Controls.Add(statusDescriptionLabel);

            var detailPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
                Location = new Point(28, 183),
                Size = new Size(664, 310)
            };

            AddDetailRow(detailPanel, "启动项名称", 18, out nameValueLabel);
            AddDetailRow(detailPanel, "网络类型", 66, out typeValueLabel);
            AddDetailRow(detailPanel, "固件 GUID", 114, out identifierValueLabel);
            AddDetailRow(detailPanel, "设备参数", 162, out deviceValueLabel);
            AddDetailRow(detailPanel, "EFI 路径", 210, out pathValueLabel);

            var safetyLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, 270),
                Size = new Size(624, 20),
                Text = "仅设置下一次 UEFI 启动项，不修改 BIOS 永久启动顺序。"
            };
            detailPanel.Controls.Add(safetyLabel);

            detectButton = UiFactory.CreateButton("重新检测", 126, false);
            detectButton.Location = new Point(28, 526);
            detectButton.Size = new Size(126, 42);
            detectButton.AccessibleName = "重新检测网维无盘";
            detectButton.Click += async delegate { await DetectAsync(); };

            var closeButton = UiFactory.CreateButton("关闭", 104, false);
            closeButton.Location = new Point(418, 526);
            closeButton.Size = new Size(104, 42);
            closeButton.DialogResult = DialogResult.Cancel;

            startButton = UiFactory.CreateButton("启动网维无盘", 158, true);
            startButton.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            startButton.Location = new Point(534, 524);
            startButton.Size = new Size(158, 46);
            startButton.AccessibleName = "启动网维无盘";
            startButton.Enabled = false;
            startButton.Click += async delegate { await StartNetworkBootAsync(); };
            detailToolTip.SetToolTip(startButton, "检测成功并确认参数后，设置下一次 UEFI 启动并立即重启");

            Controls.Add(titleLabel);
            Controls.Add(subtitleLabel);
            Controls.Add(statusPanel);
            Controls.Add(detailPanel);
            Controls.Add(detectButton);
            Controls.Add(closeButton);
            Controls.Add(startButton);
            CancelButton = closeButton;

            ResetDetails();
            FormClosing += delegate(object sender, FormClosingEventArgs eventArgs)
            {
                if (isApplyingNetworkBoot)
                {
                    eventArgs.Cancel = true;
                    return;
                }

                isClosing = true;
            };
            Shown += async delegate { await DetectAsync(); };
            CompleteDialogLayout();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                detailToolTip.Dispose();
            }

            base.Dispose(disposing);
        }

        private void AddDetailRow(AntPanel panel, string title, int top, out Label valueLabel)
        {
            var titleLabel = new Label
            {
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(20, top),
                Size = new Size(100, 30),
                Text = title,
                TextAlign = ContentAlignment.MiddleLeft
            };

            valueLabel = new Label
            {
                AutoEllipsis = true,
                AutoSize = false,
                BackColor = UiTheme.Surface,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(132, top),
                Size = new Size(512, 30),
                Text = "等待检测",
                TextAlign = ContentAlignment.MiddleLeft
            };

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(valueLabel);
        }

        private async Task DetectAsync()
        {
            if (isDetecting)
            {
                return;
            }

            isDetecting = true;
            selectedEntry = null;
            startButton.Enabled = false;
            detectButton.Enabled = false;
            detectButton.Loading = true;
            statusTag.Text = "正在检测";
            statusTag.Type = AntdUI.TTypeMini.Primary;
            statusTag.BackColor = UiTheme.AccentSoft;
            statusTag.ForeColor = UiTheme.Accent;
            statusDescriptionLabel.Text = "正在读取这台电脑的 UEFI 固件启动项...";
            ResetDetails();

            try
            {
                List<FirmwareBootEntry> entries = await Task.Run(
                    (Func<List<FirmwareBootEntry>>)BcdService.LoadFirmwareEntries);
                if (isClosing || IsDisposed)
                {
                    return;
                }

                selectedEntry = FirmwareBootParser.FindBestNetworkBootEntry(entries);
                if (selectedEntry == null)
                {
                    ShowNotFound();
                    return;
                }

                ShowDetectedEntry(selectedEntry);
            }
            catch (Exception exception)
            {
                if (isClosing || IsDisposed)
                {
                    return;
                }

                statusTag.Text = "检测失败";
                statusTag.Type = AntdUI.TTypeMini.Error;
                statusTag.BackColor = UiTheme.WarningSoft;
                statusTag.ForeColor = UiTheme.Warning;
                statusDescriptionLabel.Text = "无法读取 UEFI 固件启动项，请检查主板启动模式和管理员权限。";
                detailToolTip.SetToolTip(statusDescriptionLabel, exception.Message);
            }
            finally
            {
                isDetecting = false;
                if (!isClosing && !IsDisposed)
                {
                    detectButton.Loading = false;
                    detectButton.Enabled = true;
                }
            }
        }

        private void ShowDetectedEntry(FirmwareBootEntry entry)
        {
            statusTag.Text = "检测成功";
            statusTag.Type = AntdUI.TTypeMini.Success;
            statusTag.BackColor = UiTheme.SuccessSoft;
            statusTag.ForeColor = UiTheme.Success;
            statusDescriptionLabel.Text = "已识别网络启动项，请核对下面的参数详情。";
            SetDetailValue(nameValueLabel, entry.DisplayName);
            SetDetailValue(typeValueLabel, entry.NetworkType);
            SetDetailValue(identifierValueLabel, entry.Identifier);
            SetDetailValue(deviceValueLabel, FormatFirmwareValue(entry.Device));
            SetDetailValue(pathValueLabel, FormatFirmwareValue(entry.Path));
            startButton.Enabled = true;
            startButton.Focus();
        }

        private void ShowNotFound()
        {
            statusTag.Text = "未检测到";
            statusTag.Type = AntdUI.TTypeMini.Warn;
            statusTag.BackColor = UiTheme.WarningSoft;
            statusTag.ForeColor = UiTheme.Warning;
            statusDescriptionLabel.Text = "固件中没有发现 PXE、IPv4、IPv6 或明确的网络启动项。";
            startButton.Enabled = false;
        }

        private async Task StartNetworkBootAsync()
        {
            if (selectedEntry == null)
            {
                return;
            }

            DialogResult confirmation;
            using (var confirmationDialog = new NetworkBootConfirmDialog(this, selectedEntry))
            {
                confirmation = confirmationDialog.ShowDialog(this);
            }
            if (confirmation != DialogResult.OK)
            {
                return;
            }

            detectButton.Enabled = false;
            startButton.Enabled = false;
            startButton.Loading = true;
            startButton.Text = "正在设置并重启";
            isApplyingNetworkBoot = true;
            bool networkBootWasSet = false;
            try
            {
                FirmwareBootEntry entry = selectedEntry;
                await Task.Run(() => BcdService.SetNextFirmwareBoot(entry));
                networkBootWasSet = true;
                BcdService.RestartComputer();
                isApplyingNetworkBoot = false;
                Close();
            }
            catch (Exception exception)
            {
                isApplyingNetworkBoot = false;
                startButton.Loading = false;
                startButton.Text = "启动网维无盘";
                startButton.Enabled = true;
                detectButton.Enabled = true;
                UiDialogs.ShowError(
                    this,
                    networkBootWasSet ? "自动重启失败" : "设置网维启动失败",
                    (networkBootWasSet
                        ? "网维无盘已设置为下一次启动项，但自动重启失败。请保存工作后手动重启电脑。"
                        : "没有修改永久 BIOS 启动顺序。请确认主板支持从 Windows 设置下一次固件启动项。") +
                    "\r\n\r\n" + exception.Message);
            }
        }

        private void ResetDetails()
        {
            SetDetailValue(nameValueLabel, "等待检测");
            SetDetailValue(typeValueLabel, "等待检测");
            SetDetailValue(identifierValueLabel, "等待检测");
            SetDetailValue(deviceValueLabel, "等待检测");
            SetDetailValue(pathValueLabel, "等待检测");
        }

        private void SetDetailValue(Label label, string value)
        {
            label.Text = value;
            detailToolTip.SetToolTip(label, value);
        }

        private static string FormatFirmwareValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "固件未提供" : value;
        }
    }
}
