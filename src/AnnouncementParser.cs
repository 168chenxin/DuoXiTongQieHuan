using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DualBootSwitcher
{
    internal static class AnnouncementParser
    {
        private static readonly Regex ImagePattern = new Regex(
            @"^!\[(?<text>[^\]]*)\]\((?<url>https://[^\s)]+)\)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex LinkPattern = new Regex(
            @"^\[(?<text>[^\]]+)\]\((?<url>https://[^\s)]+)\)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static List<AnnouncementBlock> Parse(string markdown)
        {
            var blocks = new List<AnnouncementBlock>();
            if (string.IsNullOrWhiteSpace(markdown))
            {
                blocks.Add(new AnnouncementBlock(AnnouncementBlockKind.Paragraph, "当前没有新的公告。", null));
                return blocks;
            }

            string[] lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            var paragraphLines = new List<string>();
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                Match imageMatch = ImagePattern.Match(line);
                Match linkMatch = LinkPattern.Match(line);
                if (line.Length == 0)
                {
                    FlushParagraph(blocks, paragraphLines);
                }
                else if (line.StartsWith("# ", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraphLines);
                    blocks.Add(new AnnouncementBlock(AnnouncementBlockKind.Title, line.Substring(2).Trim(), null));
                }
                else if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraphLines);
                    blocks.Add(new AnnouncementBlock(AnnouncementBlockKind.Subtitle, line.Substring(3).Trim(), null));
                }
                else if (line == "---" || line == "***")
                {
                    FlushParagraph(blocks, paragraphLines);
                    blocks.Add(new AnnouncementBlock(AnnouncementBlockKind.Divider, null, null));
                }
                else if (imageMatch.Success)
                {
                    FlushParagraph(blocks, paragraphLines);
                    blocks.Add(new AnnouncementBlock(
                        AnnouncementBlockKind.Image,
                        imageMatch.Groups["text"].Value,
                        imageMatch.Groups["url"].Value));
                }
                else if (linkMatch.Success)
                {
                    FlushParagraph(blocks, paragraphLines);
                    blocks.Add(new AnnouncementBlock(
                        AnnouncementBlockKind.Link,
                        linkMatch.Groups["text"].Value,
                        linkMatch.Groups["url"].Value));
                }
                else if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
                {
                    FlushParagraph(blocks, paragraphLines);
                    blocks.Add(new AnnouncementBlock(AnnouncementBlockKind.Bullet, line.Substring(2).Trim(), null));
                }
                else
                {
                    paragraphLines.Add(line);
                }
            }

            FlushParagraph(blocks, paragraphLines);
            return blocks;
        }

        private static void FlushParagraph(List<AnnouncementBlock> blocks, List<string> lines)
        {
            if (lines.Count == 0)
            {
                return;
            }

            blocks.Add(new AnnouncementBlock(AnnouncementBlockKind.Paragraph, string.Join("\r\n", lines.ToArray()), null));
            lines.Clear();
        }
    }
}
