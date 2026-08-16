using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DualBootSwitcher
{
    internal static class BcdService
    {
        internal const string FirmwareSetupRestartArguments = "/r /fw /t 0";

        public static List<BootEntry> LoadEntries()
        {
            return LoadConfiguration().Entries;
        }

        public static List<FirmwareBootEntry> LoadFirmwareEntries()
        {
            BcdCommandResult result = RunBcdEditRaw("/enum firmware");
            List<FirmwareBootEntry> entries = FirmwareBootParser.ParseEntries(result.CombinedOutput);
            if (result.IsSuccess || entries.Count > 0)
            {
                return entries;
            }

            throw new InvalidOperationException("bcdedit 读取固件启动项失败：" + result.ErrorDetails);
        }

        public static void SetNextFirmwareBoot(FirmwareBootEntry entry)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Identifier))
            {
                throw new ArgumentException("请选择一个有效的固件启动项。", "entry");
            }

            BcdCommandResult setResult = RunBcdEditRaw(
                "/set {fwbootmgr} bootsequence " + entry.Identifier);
            if (setResult.IsSuccess)
            {
                return;
            }

            BcdCommandResult verificationResult = RunBcdEditRaw("/enum {fwbootmgr} /v");
            if (WasFirmwareBootSequenceApplied(entry.Identifier, verificationResult))
            {
                return;
            }

            throw new InvalidOperationException(
                "bcdedit 设置下一次固件启动失败：" + setResult.ErrorDetails);
        }

        public static BootConfiguration LoadConfiguration()
        {
            string loaderOutput = RunBcdEditRead("/enum osloader /v");
            string bootManagerOutput = RunBcdEditRead("/enum {bootmgr} /v");
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

            BcdCommandResult setResult = RunBcdEditRaw("/default " + entry.Identifier);
            if (setResult.IsSuccess)
            {
                return;
            }

            BcdCommandResult verificationResult = RunBcdEditRaw("/enum {bootmgr} /v");
            if (WasDefaultApplied(entry.Identifier, verificationResult))
            {
                return;
            }

            throw new InvalidOperationException("bcdedit 设置默认启动项失败：" + setResult.ErrorDetails);
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

            BcdCommandResult setResult = RunBcdEditRaw("/timeout " + seconds);
            if (setResult.IsSuccess)
            {
                return;
            }

            BcdCommandResult verificationResult = RunBcdEditRaw("/enum {bootmgr} /v");
            if (WasTimeoutApplied(seconds, verificationResult))
            {
                return;
            }

            throw new InvalidOperationException("bcdedit 设置启动等待时间失败：" + setResult.ErrorDetails);
        }

        public static void RestartComputer()
        {
            RunShutdown("/r /t 0", "自动重启失败");
        }

        public static void RestartToFirmwareSettings()
        {
            RunShutdown(FirmwareSetupRestartArguments, "重启进入 UEFI/BIOS 设置失败");
        }

        private static void RunShutdown(string arguments, string failureMessage)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
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
                    throw new InvalidOperationException(failureMessage + "：" + details.Trim());
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

        internal static bool WasFirmwareBootSequenceApplied(
            string identifier,
            BcdCommandResult verificationResult)
        {
            return verificationResult != null &&
                BcdParser.BootSequenceContains(verificationResult.CombinedOutput, identifier);
        }

        internal static bool WasDefaultApplied(string identifier, BcdCommandResult verificationResult)
        {
            return verificationResult != null && BcdParser.IdentifiersMatch(
                BcdParser.ParseDefaultIdentifier(verificationResult.CombinedOutput),
                identifier);
        }

        internal static bool WasTimeoutApplied(int seconds, BcdCommandResult verificationResult)
        {
            return verificationResult != null &&
                BcdParser.ParseTimeout(verificationResult.CombinedOutput) == seconds;
        }

        internal static bool CanUseBcdReadOutput(BcdCommandResult result)
        {
            return result != null && !string.IsNullOrWhiteSpace(result.CombinedOutput);
        }

        private static string RunBcdEditRead(string arguments)
        {
            BcdCommandResult result = RunBcdEditRaw(arguments);
            if (result.IsSuccess || CanUseBcdReadOutput(result))
            {
                return result.CombinedOutput;
            }

            throw new InvalidOperationException("bcdedit 读取失败：" + result.ErrorDetails);
        }

        private static BcdCommandResult RunBcdEditRaw(string arguments)
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

                return new BcdCommandResult(process.ExitCode, standardOutput, standardError);
            }
        }
    }
}
