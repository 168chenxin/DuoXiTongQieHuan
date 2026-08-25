using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using DualBootSwitcher;

internal static class UpdateServiceTests
{
    private static int Main()
    {
        try
        {
            ParsesOnlyNewStableExecutableReleases();
            IgnoresMalformedReleaseResponses();
            RequiresSecureExecutableUrls();
            VerifiesSha256Digest();
            RejectsInvalidDigest();
            RejectsReleaseWithoutVerifiedExecutable();
            NormalizesAnnouncementContent();
            ParsesGitHubAnnouncementContent();
            ProvidesOfflineAnnouncementFallback();
            Console.WriteLine("Update service tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void ParsesOnlyNewStableExecutableReleases()
    {
        const string json = "{\"tag_name\":\"v1.4.0\",\"draft\":false,\"prerelease\":false,\"html_url\":\"https://github.com/example/release\",\"assets\":[{\"name\":\"DualBootSwitcher.exe\",\"browser_download_url\":\"https://github.com/example/app.exe\",\"digest\":\"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"}]}";
        UpdateInfo update = UpdateService.FindUpdate(json, new Version(1, 3, 1, 0));
        AssertTrue(update != null, "A newer stable executable release should be offered.");
        AssertEqual("v1.4.0", update.Tag, "The release tag should be preserved.");

        const string prereleaseJson = "{\"tag_name\":\"v2.0.0\",\"draft\":false,\"prerelease\":true,\"assets\":[]}";
        AssertTrue(
            UpdateService.FindUpdate(prereleaseJson, new Version(1, 3, 1, 0)) == null,
            "Prereleases must not be offered as automatic updates.");
    }

    private static void VerifiesSha256Digest()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "dual boot update", Encoding.UTF8);
            byte[] bytes = File.ReadAllBytes(path);
            string hash;
            using (SHA256 sha256 = SHA256.Create())
            {
                hash = ToHex(sha256.ComputeHash(bytes));
            }

            AssertTrue(
                UpdateService.VerifyDigest(path, "sha256:" + hash),
                "A matching SHA-256 digest should pass verification.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void IgnoresMalformedReleaseResponses()
    {
        AssertTrue(
            UpdateService.FindUpdate("{not-json", new Version(1, 3, 1, 0)) == null,
            "Malformed GitHub responses should not produce an update or crash the background check.");
        AssertTrue(
            UpdateService.FindUpdate("null", new Version(1, 3, 1, 0)) == null,
            "An empty GitHub release payload should be ignored.");
    }

    private static void RequiresSecureExecutableUrls()
    {
        const string json = "{\"tag_name\":\"v1.4.0\",\"draft\":false,\"prerelease\":false,\"html_url\":\"https://github.com/example/release\",\"assets\":[{\"name\":\"DualBootSwitcher.exe\",\"browser_download_url\":\"http://example.com/app.exe\",\"digest\":\"sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\"}]}";
        AssertTrue(
            UpdateService.FindUpdate(json, new Version(1, 3, 1, 0)) == null,
            "Insecure executable download URLs must not be offered as automatic updates.");
    }

    private static void RejectsInvalidDigest()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "dual boot update", Encoding.UTF8);
            AssertTrue(
                !UpdateService.VerifyDigest(path, "sha256:0000000000000000000000000000000000000000000000000000000000000000"),
                "A mismatching SHA-256 digest must fail verification.");
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void RejectsReleaseWithoutVerifiedExecutable()
    {
        const string json = "{\"tag_name\":\"v1.4.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"DualBootSwitcher.exe\",\"browser_download_url\":\"https://github.com/example/app.exe\",\"digest\":null}]}";
        AssertTrue(
            UpdateService.FindUpdate(json, new Version(1, 3, 1, 0)) == null,
            "An executable without a GitHub SHA-256 digest must not be offered.");
    }

    private static void NormalizesAnnouncementContent()
    {
        AssertEqual(
            "# 维护通知\n\n请使用正式版。",
            UpdateService.NormalizeAnnouncement("# 维护通知\r\n\r\n请使用正式版。\r\n"),
            "Announcement content should preserve Markdown titles and normalize line breaks.");
        AssertEqual(
            "当前没有新的公告。",
            UpdateService.NormalizeAnnouncement("  \r\n"),
            "An empty announcement should provide a clear fallback message.");
    }

    private static void ProvidesOfflineAnnouncementFallback()
    {
        AssertTrue(
            UpdateService.DefaultAnnouncement.Contains("多系统切换"),
            "An embedded announcement should be available when the online announcement cannot be read.");
    }

    private static void ParsesGitHubAnnouncementContent()
    {
        const string json = "{\"encoding\":\"base64\",\"content\":\"IyDlhazlkYoNCg0KaGVsbG8=\\n\"}";
        AssertEqual(
            "# 公告\n\nhello",
            UpdateService.ParseAnnouncementContent(json).Replace("\r\n", "\n"),
            "The GitHub Contents API base64 response should decode as UTF-8 Markdown.");
    }

    private static string ToHex(byte[] bytes)
    {
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (byte value in bytes)
        {
            builder.Append(value.ToString("x2"));
        }

        return builder.ToString();
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
