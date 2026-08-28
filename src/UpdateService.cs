using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace SysSwitch
{
    [DataContract]
    internal sealed class GitHubRelease
    {
        [DataMember(Name = "tag_name")]
        public string TagName { get; set; }

        [DataMember(Name = "draft")]
        public bool Draft { get; set; }

        [DataMember(Name = "prerelease")]
        public bool Prerelease { get; set; }

        [DataMember(Name = "html_url")]
        public string HtmlUrl { get; set; }

        [DataMember(Name = "assets")]
        public GitHubReleaseAsset[] Assets { get; set; }
    }

    [DataContract]
    internal sealed class GitHubReleaseAsset
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "browser_download_url")]
        public string DownloadUrl { get; set; }

        [DataMember(Name = "digest")]
        public string Digest { get; set; }
    }

    internal sealed class UpdateInfo
    {
        public UpdateInfo(Version version, string tag, string pageUrl, GitHubReleaseAsset asset)
        {
            Version = version;
            Tag = tag;
            PageUrl = pageUrl;
            Asset = asset;
        }

        public Version Version { get; private set; }

        public string Tag { get; private set; }

        public string PageUrl { get; private set; }

        public GitHubReleaseAsset Asset { get; private set; }
    }

    [DataContract]
    internal sealed class GitHubContentFile
    {
        [DataMember(Name = "content")]
        public string Content { get; set; }

        [DataMember(Name = "encoding")]
        public string Encoding { get; set; }
    }

    internal sealed class AnnouncementInfo
    {
        public AnnouncementInfo(string content, bool isRemote)
        {
            Content = content;
            IsRemote = isRemote;
        }

        public string Content { get; private set; }

        public bool IsRemote { get; private set; }
    }

    internal static class UpdateService
    {
        internal const string Repository = "168chenxin/SysSwitch-Master";
        internal const string RepositoryUrl = "https://github.com/" + Repository;
        internal const string ExecutableName = "SysSwitch.exe";
        internal const string AnnouncementUrl = "https://api.github.com/repos/" + Repository + "/contents/ANNOUNCEMENT.md";
        internal const string DefaultAnnouncement = "# 软件公告\r\n\r\n欢迎使用系统切换大师。\r\n\r\n## 当前通知\r\n\r\n- 请始终从项目主页的 Releases 下载正式版本。";
        private const int NetworkTimeoutMilliseconds = 12000;

        public static UpdateInfo FindUpdate(string json, Version currentVersion)
        {
            if (string.IsNullOrWhiteSpace(json) || currentVersion == null)
            {
                return null;
            }

            GitHubRelease release;
            try
            {
                release = DeserializeRelease(json);
            }
            catch (SerializationException)
            {
                return null;
            }

            if (release == null)
            {
                return null;
            }

            Version releaseVersion = ParseVersion(release.TagName);
            if (release.Draft || release.Prerelease || releaseVersion == null || releaseVersion <= currentVersion)
            {
                return null;
            }

            GitHubReleaseAsset asset = FindExecutableAsset(release.Assets);
            string expectedDigest;
            if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl) ||
                !IsHttpsUrl(asset.DownloadUrl) ||
                !TryParseDigest(asset.Digest, out expectedDigest))
            {
                return null;
            }

            return new UpdateInfo(releaseVersion, release.TagName, release.HtmlUrl, asset);
        }

        public static UpdateInfo CheckLatest(Version currentVersion)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            string json = GetText("https://api.github.com/repos/" + Repository + "/releases/latest");
            return FindUpdate(json, currentVersion);
        }

        public static AnnouncementInfo LoadAnnouncement()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            try
            {
                return new AnnouncementInfo(
                    NormalizeAnnouncement(ParseAnnouncementContent(GetText(AnnouncementUrl))),
                    true);
            }
            catch (WebException exception)
            {
                var response = exception.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new AnnouncementInfo(DefaultAnnouncement, false);
                }

                throw;
            }
        }

        internal static string NormalizeAnnouncement(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return "当前没有新的公告。";
            }

            return content.Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        }

        internal static string ParseAnnouncementContent(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new InvalidDataException("公告服务返回了空内容。");
            }

            var serializer = new DataContractJsonSerializer(typeof(GitHubContentFile));
            GitHubContentFile file;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                file = (GitHubContentFile)serializer.ReadObject(stream);
            }

            if (file == null || !string.Equals(file.Encoding, "base64", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(file.Content))
            {
                throw new InvalidDataException("公告服务返回的内容格式无效。");
            }

            string encodedContent = file.Content.Replace("\r", string.Empty).Replace("\n", string.Empty);
            return Encoding.UTF8.GetString(Convert.FromBase64String(encodedContent));
        }

        public static string DownloadAndVerify(UpdateInfo update, CancellationToken cancellationToken)
        {
            if (update == null || update.Asset == null)
            {
                throw new ArgumentNullException("update");
            }

            string temporaryPath = Path.Combine(
                Path.GetTempPath(),
                "SysSwitch-" + update.Tag + "-" + Guid.NewGuid().ToString("N") + ".exe");
            try
            {
                DownloadFile(update.Asset.DownloadUrl, temporaryPath, cancellationToken);
                if (!VerifyDigest(temporaryPath, update.Asset.Digest))
                {
                    throw new InvalidDataException("下载文件校验失败，文件可能已损坏或被篡改。");
                }

                return temporaryPath;
            }
            catch
            {
                TryDelete(temporaryPath);
                throw;
            }
        }

        public static void ReplaceAndRestart(string downloadedPath)
        {
            if (string.IsNullOrWhiteSpace(downloadedPath) || !File.Exists(downloadedPath))
            {
                throw new FileNotFoundException("更新文件不存在。", downloadedPath);
            }

            string targetPath = Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath);
            string scriptPath = Path.Combine(
                Path.GetTempPath(),
                "SysSwitch-update-" + Guid.NewGuid().ToString("N") + ".cmd");
            string backupPath = targetPath + ".previous";
            string script = BuildUpdateScript(downloadedPath, targetPath, backupPath);
            File.WriteAllText(scriptPath, script, new UTF8Encoding(false));

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                Arguments = "/d /c start \"\" /min \"" + scriptPath + "\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
            };
            System.Diagnostics.Process.Start(startInfo);
        }

        internal static Version ParseVersion(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return null;
            }

            string value = tag.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                value = value.Substring(1);
            }

            Version version;
            return Version.TryParse(value, out version) ? version : null;
        }

        internal static bool VerifyDigest(string path, string digest)
        {
            string expected;
            if (!File.Exists(path) || !TryParseDigest(digest, out expected))
            {
                return false;
            }

            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
            {
                string actual = ToLowerHex(sha256.ComputeHash(stream));
                return string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static GitHubRelease DeserializeRelease(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(GitHubRelease));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                return (GitHubRelease)serializer.ReadObject(stream);
            }
        }

        private static GitHubReleaseAsset FindExecutableAsset(GitHubReleaseAsset[] assets)
        {
            if (assets == null)
            {
                return null;
            }

            foreach (GitHubReleaseAsset asset in assets)
            {
                if (asset != null && string.Equals(asset.Name, ExecutableName, StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }

            return null;
        }

        private static bool IsHttpsUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseDigest(string digest, out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(digest))
            {
                return false;
            }

            string[] parts = digest.Split(new[] { ':' }, 2);
            if (parts.Length != 2 || !string.Equals(parts[0], "sha256", StringComparison.OrdinalIgnoreCase) ||
                parts[1].Length != 64)
            {
                return false;
            }

            for (int index = 0; index < parts[1].Length; index++)
            {
                if (!Uri.IsHexDigit(parts[1][index]))
                {
                    return false;
                }
            }

            value = parts[1].ToLowerInvariant();
            return true;
        }

        private static string GetText(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "SysSwitch/" + typeof(UpdateService).Assembly.GetName().Version;
            request.Accept = "application/vnd.github+json";
            request.Timeout = NetworkTimeoutMilliseconds;
            request.ReadWriteTimeout = NetworkTimeoutMilliseconds;
            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void DownloadFile(string url, string destination, CancellationToken cancellationToken)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "SysSwitch/" + typeof(UpdateService).Assembly.GetName().Version;
            request.Timeout = NetworkTimeoutMilliseconds;
            request.ReadWriteTimeout = NetworkTimeoutMilliseconds;
            using (WebResponse response = request.GetResponse())
            using (Stream input = response.GetResponseStream())
            using (FileStream output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                byte[] buffer = new byte[81920];
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    output.Write(buffer, 0, read);
                }
            }
        }

        private static string BuildUpdateScript(string source, string target, string backup)
        {
            return "@echo off\r\n" +
                "chcp 65001>nul\r\n" +
                "set /a attempts=0\r\n" +
                ":replace\r\n" +
                "set /a attempts+=1\r\n" +
                "move /y \"" + target + "\" \"" + backup + "\" >nul 2>&1\r\n" +
                "if errorlevel 1 goto wait\r\n" +
                "move /y \"" + source + "\" \"" + target + "\" >nul 2>&1\r\n" +
                "if not errorlevel 1 goto launch\r\n" +
                "move /y \"" + backup + "\" \"" + target + "\" >nul 2>&1\r\n" +
                ":wait\r\n" +
                "if %attempts% geq 15 goto cleanup\r\n" +
                "timeout /t 1 /nobreak>nul\r\n" +
                "goto replace\r\n" +
                ":launch\r\n" +
                "start \"\" \"" + target + "\"\r\n" +
                "del /f /q \"" + backup + "\" >nul 2>&1\r\n" +
                ":cleanup\r\n" +
                "del /f /q \"%~f0\" >nul 2>&1\r\n";
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (byte value in bytes)
            {
                builder.Append(value.ToString("x2"));
            }

            return builder.ToString();
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
