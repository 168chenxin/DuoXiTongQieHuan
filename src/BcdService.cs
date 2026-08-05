using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DualBootSwitcher
{
    internal static class BcdService
    {
        public static List<BootEntry> LoadEntries()
        {
            return LoadConfiguration().Entries;
        }

        public static BootConfiguration LoadConfiguration()
        {
            string loaderOutput = RunBcdEdit("/enum osloader /v");
            string bootManagerOutput = RunBcdEdit("/enum {bootmgr} /v");
            string defaultIdentifier = BcdParser.ParseDefaultIdentifier(bootManagerOutput);
            int timeoutSeconds = BcdParser.ParseTimeout(bootManagerOutput);
            List<BootEntry> discoveredEntries = BcdParser.ParseBootLoaders(loaderOutput);
            List<string> displayOrder = BcdParser.ParseDisplayOrder(bootManagerOutput);

            if (timeoutSeconds < 0)
            {
                throw new InvalidOperationException(
                    "无法读取 Windows 启动菜单的超时时间。当前版本支持中文和英文 Windows 输出。");
            }

            if (displayOrder.Count == 0)
            {
                throw new InvalidOperationException(
                    "无法读取 Windows 启动菜单。当前版本支持中文和英文 Windows 输出。");
            }

            List<BootEntry> entries = SelectDisplayedEntries(discoveredEntries, displayOrder);

            if (entries.Count == 0)
            {
                throw new InvalidOperationException("没有从 Windows 启动菜单中找到可切换的系统。");
            }

            foreach (BootEntry entry in entries)
            {
                entry.IsDefault = BcdParser.IdentifiersMatch(entry.Identifier, defaultIdentifier);
            }

            return new BootConfiguration(entries, timeoutSeconds);
        }

        public static void SetDefault(BootEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Identifier))
            {
                throw new ArgumentException("请选择一个有效的启动项。", "entry");
            }

            RunBcdEdit("/default " + entry.Identifier);
        }

        public static void SetTimeout(int seconds)
        {
            if (seconds < 0 || seconds > 999)
            {
                throw new ArgumentOutOfRangeException(
                    "seconds",
                    seconds,
                    "启动菜单超时时间必须在 0 到 999 秒之间。");
            }

            RunBcdEdit("/timeout " + seconds);
        }

        public static void RestartComputer()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
                Arguments = "/r /t 0",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string details = string.IsNullOrWhiteSpace(standardError)
                        ? standardOutput
                        : standardError;
                    throw new InvalidOperationException("自动重启失败：" + details.Trim());
                }
            }
        }

        internal static List<BootEntry> SelectDisplayedEntries(
            List<BootEntry> discoveredEntries,
            List<string> displayOrder)
        {
            var selectedEntries = new List<BootEntry>();

            if (discoveredEntries == null || displayOrder == null)
            {
                return selectedEntries;
            }

            foreach (string identifier in displayOrder)
            {
                foreach (BootEntry entry in discoveredEntries)
                {
                    if (BcdParser.IdentifiersMatch(entry.Identifier, identifier))
                    {
                        selectedEntries.Add(entry);
                        break;
                    }
                }
            }

            return selectedEntries;
        }

        private static string RunBcdEdit(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "bcdedit.exe"),
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string standardOutput = process.StandardOutput.ReadToEnd();
                string standardError = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    string details = string.IsNullOrWhiteSpace(standardError)
                        ? standardOutput
                        : standardError;
                    throw new InvalidOperationException("bcdedit 执行失败：" + details.Trim());
                }

                return standardOutput;
            }
        }
    }
}
