using System;
using System.Collections.Generic;
using DualBootSwitcher;

internal static class AnnouncementParserTests
{
    private static int Main()
    {
        try
        {
            ParsesSupportedMarkdownBlocks();
            RejectsUnsupportedImageSources();
            Console.WriteLine("Announcement parser tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void ParsesSupportedMarkdownBlocks()
    {
        const string content = "# 大标题\n## 小标题\n正文内容\n- 列表内容\n---\n![公告图片](https://example.com/notice.png)\n[项目主页](https://github.com/168chenxin/DuoXiTongQieHuan)";
        List<AnnouncementBlock> blocks = AnnouncementParser.Parse(content);
        AssertEqual(7, blocks.Count, "All supported Markdown blocks should be preserved.");
        AssertEqual(AnnouncementBlockKind.Title, blocks[0].Kind, "A single hash should create a title.");
        AssertEqual(AnnouncementBlockKind.Subtitle, blocks[1].Kind, "Two hashes should create a subtitle.");
        AssertEqual(AnnouncementBlockKind.Image, blocks[5].Kind, "An HTTPS Markdown image should create an image block.");
        AssertEqual("https://example.com/notice.png", blocks[5].ImageUrl, "The image URL should be preserved.");
        AssertEqual(AnnouncementBlockKind.Link, blocks[6].Kind, "An HTTPS Markdown link should create a clickable link block.");
        AssertEqual("https://github.com/168chenxin/DuoXiTongQieHuan", blocks[6].ImageUrl, "The project URL should be preserved.");
    }

    private static void RejectsUnsupportedImageSources()
    {
        List<AnnouncementBlock> blocks = AnnouncementParser.Parse("![本地图片](file:///C:/notice.png)");
        AssertEqual(AnnouncementBlockKind.Paragraph, blocks[0].Kind, "Only HTTPS image URLs should be rendered as remote images.");
    }

    private static void AssertEqual(int expected, int actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertEqual(AnnouncementBlockKind expected, AnnouncementBlockKind actual, string message)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
