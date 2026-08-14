using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AntPanel = AntdUI.Panel;
using AntTag = AntdUI.Tag;

namespace DualBootSwitcher
{
    internal sealed class AnnouncementDialog : StyledDialogForm
    {
        private readonly AntTag statusTag;
        private readonly FlowLayoutPanel contentFlow;
        private bool isLoading;
        private bool isClosing;

        public AnnouncementDialog(Form owner)
        {
            Text = "公告";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(620, 522);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BackColor = UiTheme.Canvas;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = owner == null ? null : owner.Icon;

            var titleLabel = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Ink,
                Location = new Point(24, 22),
                Text = "软件公告"
            };
            statusTag = new AntTag
            {
                AutoSize = false,
                BackColor = UiTheme.AccentSoft,
                BorderWidth = 0F,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = UiTheme.Accent,
                Location = new Point(472, 20),
                Radius = UiTheme.BadgeCornerRadius,
                Size = new Size(124, 32),
                Text = "准备读取",
                TextAlign = ContentAlignment.MiddleCenter,
                Type = AntdUI.TTypeMini.Primary
            };
            var descriptionLabel = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = UiTheme.Muted,
                Location = new Point(25, 53),
                Size = new Size(570, 22),
                Text = "公告会在打开时自动从软件官方 GitHub 仓库读取最新内容。"
            };
            var repositoryLink = new LinkLabel
            {
                AutoEllipsis = true,
                AutoSize = false,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point),
                LinkColor = UiTheme.Accent,
                Location = new Point(25, 76),
                Size = new Size(570, 18),
                Text = "项目主页：" + UpdateService.RepositoryUrl,
                TextAlign = ContentAlignment.MiddleLeft,
                UseCompatibleTextRendering = true,
                VisitedLinkColor = UiTheme.Accent
            };
            repositoryLink.AccessibleName = "打开项目主页";
            repositoryLink.LinkClicked += delegate { OpenRepositoryPage(); };
            var contentPanel = new AntPanel
            {
                Back = UiTheme.Surface,
                BorderColor = UiTheme.Border,
                BorderWidth = 1F,
                Radius = UiTheme.SurfaceCornerRadius,
                Location = new Point(24, 104),
                Size = new Size(572, 345)
            };
            contentFlow = new FlowLayoutPanel
            {
                AutoScroll = true,
                BackColor = UiTheme.Surface,
                FlowDirection = FlowDirection.TopDown,
                Location = new Point(16, 14),
                Padding = new Padding(2, 0, 2, 0),
                Size = new Size(540, 317),
                TabStop = false,
                WrapContents = false
            };
            contentPanel.Controls.Add(contentFlow);

            AntdUI.Button closeButton = UiFactory.CreateButton("关闭", 108, false);
            closeButton.Location = new Point(488, 464);
            closeButton.Size = new Size(108, 40);
            closeButton.DialogResult = DialogResult.Cancel;

            Controls.Add(titleLabel);
            Controls.Add(statusTag);
            Controls.Add(descriptionLabel);
            Controls.Add(repositoryLink);
            Controls.Add(contentPanel);
            Controls.Add(closeButton);
            CancelButton = closeButton;
            FormClosing += delegate { isClosing = true; };
            Shown += async delegate { await LoadAsync(); };
            CompleteDialogLayout();
        }

        private async Task LoadAsync()
        {
            if (isLoading)
            {
                return;
            }

            isLoading = true;
            statusTag.Text = "正在读取";
            statusTag.BackColor = UiTheme.AccentSoft;
            statusTag.ForeColor = UiTheme.Accent;
            statusTag.Type = AntdUI.TTypeMini.Primary;
            RenderLoading();
            try
            {
                AnnouncementInfo announcement = await Task.Run((Func<AnnouncementInfo>)UpdateService.LoadAnnouncement);
                if (isClosing || IsDisposed)
                {
                    return;
                }

                RenderAnnouncement(announcement.Content);
                if (announcement.IsRemote)
                {
                    statusTag.Text = "已同步";
                    statusTag.BackColor = UiTheme.SuccessSoft;
                    statusTag.ForeColor = UiTheme.Success;
                    statusTag.Type = AntdUI.TTypeMini.Success;
                }
                else
                {
                    statusTag.Text = "内置公告";
                    statusTag.BackColor = UiTheme.AccentSoft;
                    statusTag.ForeColor = UiTheme.Accent;
                    statusTag.Type = AntdUI.TTypeMini.Primary;
                }
            }
            catch (Exception exception)
            {
                if (isClosing || IsDisposed)
                {
                    return;
                }

                RenderAnnouncement(
                    UpdateService.DefaultAnnouncement + "\r\n\r\n---\r\n## 公告读取失败\r\n下次打开公告时将自动重试。\r\n" + exception.Message);
                statusTag.Text = "读取失败";
                statusTag.BackColor = UiTheme.WarningSoft;
                statusTag.ForeColor = UiTheme.Warning;
                statusTag.Type = AntdUI.TTypeMini.Error;
            }
            finally
            {
                isLoading = false;
            }
        }

        private static void OpenRepositoryPage()
        {
            OpenUrl(UpdateService.RepositoryUrl);
        }

        private void RenderLoading()
        {
            ClearAnnouncementControls();
            contentFlow.Controls.Add(CreateTextBlock("正在从官方公告页读取内容...", 9.5F, FontStyle.Regular, UiTheme.Muted, 28));
        }

        private void RenderAnnouncement(string markdown)
        {
            contentFlow.SuspendLayout();
            ClearAnnouncementControls();
            List<AnnouncementBlock> blocks = AnnouncementParser.Parse(markdown);
            foreach (AnnouncementBlock block in blocks)
            {
                Control control = CreateBlock(block);
                if (control != null)
                {
                    contentFlow.Controls.Add(control);
                }
            }

            contentFlow.ResumeLayout();
        }

        private Control CreateBlock(AnnouncementBlock block)
        {
            if (block.Kind == AnnouncementBlockKind.Title)
            {
                return CreateTextBlock(block.Text, 15F, FontStyle.Bold, UiTheme.Ink, 38);
            }

            if (block.Kind == AnnouncementBlockKind.Subtitle)
            {
                return CreateTextBlock(block.Text, 11F, FontStyle.Bold, UiTheme.Accent, 30);
            }

            if (block.Kind == AnnouncementBlockKind.Bullet)
            {
                return CreateTextBlock("•  " + block.Text, 9.5F, FontStyle.Regular, UiTheme.Ink, 28);
            }

            if (block.Kind == AnnouncementBlockKind.Link)
            {
                var link = new LinkLabel
                {
                    AutoEllipsis = true,
                    AutoSize = false,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
                    LinkColor = UiTheme.Accent,
                    Margin = new Padding(0, 0, 0, 8),
                    Size = new Size(512, 28),
                    Text = block.Text,
                    TextAlign = ContentAlignment.MiddleLeft,
                    UseCompatibleTextRendering = true,
                    VisitedLinkColor = UiTheme.Accent
                };
                link.AccessibleName = "打开公告链接：" + block.Text;
                link.LinkClicked += delegate { OpenUrl(block.ImageUrl); };
                return link;
            }

            if (block.Kind == AnnouncementBlockKind.Divider)
            {
                return new Panel { BackColor = UiTheme.Border, Margin = new Padding(0, 8, 0, 8), Size = new Size(512, 1) };
            }

            if (block.Kind == AnnouncementBlockKind.Image)
            {
                var image = new PictureBox
                {
                    BackColor = UiTheme.Canvas,
                    ImageLocation = block.ImageUrl,
                    Margin = new Padding(0, 6, 0, 6),
                    Size = new Size(512, 220),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    TabStop = false
                };
                image.LoadCompleted += delegate(object sender, System.ComponentModel.AsyncCompletedEventArgs eventArgs)
                {
                    if (eventArgs.Error != null && !image.IsDisposed)
                    {
                        image.ImageLocation = null;
                        image.BackColor = UiTheme.Disabled;
                        image.Controls.Add(new Label
                        {
                            AutoSize = false,
                            BackColor = UiTheme.Disabled,
                            Dock = DockStyle.Fill,
                            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
                            ForeColor = UiTheme.Muted,
                            Text = "图片暂时加载失败，下次打开公告时会自动重试。",
                            TextAlign = ContentAlignment.MiddleCenter
                        });
                    }
                };
                image.LoadAsync();
                return image;
            }

            return CreateTextBlock(block.Text, 9.5F, FontStyle.Regular, UiTheme.Ink, 0);
        }

        private static Label CreateTextBlock(string text, float size, FontStyle style, Color color, int minimumHeight)
        {
            var label = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", size, style, GraphicsUnit.Point),
                ForeColor = color,
                Margin = new Padding(0, 0, 0, 8),
                MaximumSize = new Size(512, 0),
                Size = new Size(512, minimumHeight),
                Text = text,
                TextAlign = ContentAlignment.TopLeft
            };
            Size preferred = TextRenderer.MeasureText(text, label.Font, new Size(512, 0), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl);
            label.Height = Math.Max(minimumHeight, preferred.Height + 4);
            return label;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "无法打开链接。\r\n\r\n" + url + "\r\n\r\n" + exception.Message,
                    "打开链接失败",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ClearAnnouncementControls()
        {
            while (contentFlow.Controls.Count > 0)
            {
                Control control = contentFlow.Controls[0];
                contentFlow.Controls.RemoveAt(0);
                control.Dispose();
            }
        }
    }
}
