using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DualBootSwitcher
{
    internal static class FirmwareBootParser
    {
        private static readonly Regex BlockSeparator = new Regex(@"(?:\r?\n\s*){2,}");
        private static readonly Regex PropertyPattern = new Regex(
            @"^\s*(?<name>identifier|description|device|path|标识符|描述|设备|路径)\s+(?<value>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        private static readonly Regex IdentifierPattern = new Regex(@"\{[^}\r\n]+\}");

        public static List<FirmwareBootEntry> ParseEntries(string output)
        {
            var entries = new List<FirmwareBootEntry>();
            if (string.IsNullOrWhiteSpace(output))
            {
                return entries;
            }

            foreach (string block in BlockSeparator.Split(output.Trim()))
            {
                string identifier = GetProperty(block, "identifier", "标识符");
                if (string.IsNullOrWhiteSpace(identifier) ||
                    string.Equals(identifier, "{fwbootmgr}", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(identifier, "{bootmgr}", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string description = GetProperty(block, "description", "描述");
                string device = GetProperty(block, "device", "设备");
                string path = GetProperty(block, "path", "路径");
                entries.Add(new FirmwareBootEntry(identifier, description, device, path));
            }

            return entries;
        }

        public static bool IsNetworkBoot(FirmwareBootEntry entry)
        {
            return GetNetworkScore(entry) > 0;
        }

        public static FirmwareBootEntry FindBestNetworkBootEntry(IEnumerable<FirmwareBootEntry> entries)
        {
            FirmwareBootEntry bestEntry = null;
            int bestScore = 0;
            if (entries == null)
            {
                return null;
            }

            foreach (FirmwareBootEntry entry in entries)
            {
                int score = GetNetworkScore(entry);
                if (score > bestScore)
                {
                    bestEntry = entry;
                    bestScore = score;
                }
            }

            return bestEntry;
        }

        public static string GetNetworkType(FirmwareBootEntry entry)
        {
            if (!IsNetworkBoot(entry))
            {
                return "非网络启动";
            }

            string text = GetSearchText(entry);
            bool isPxe = text.Contains("pxe");
            bool isIpv4 = text.Contains("ipv4");
            bool isIpv6 = text.Contains("ipv6");
            if (isPxe && isIpv4)
            {
                return "PXE / IPv4";
            }

            if (isPxe && isIpv6)
            {
                return "PXE / IPv6";
            }

            if (isIpv4)
            {
                return "网络启动 / IPv4";
            }

            if (isIpv6)
            {
                return "网络启动 / IPv6";
            }

            return isPxe ? "PXE 网络启动" : "UEFI 网络启动";
        }

        private static int GetNetworkScore(FirmwareBootEntry entry)
        {
            if (entry == null)
            {
                return 0;
            }

            string text = GetSearchText(entry);
            int score = 0;
            if (text.Contains("pxe")) score += 100;
            if (text.Contains("ipv4")) score += 70;
            if (text.Contains("ipv6")) score += 55;
            if (text.Contains("network")) score += 45;
            if (text.Contains("ethernet")) score += 40;
            if (text.Contains("网络")) score += 45;
            if (text.Contains("网卡")) score += 40;
            if (text.Contains("有线")) score += 30;

            // Generic NIC/LAN wording is useful only when it describes a boot-capable entry.
            bool hasBootContext = text.Contains("boot") || text.Contains("启动") ||
                text.Contains("efi") || text.Contains("venhw") || text.Contains("pci");
            if (hasBootContext && ContainsWord(text, "nic")) score += 25;
            if (hasBootContext && ContainsWord(text, "lan")) score += 25;
            return score;
        }

        private static string GetSearchText(FirmwareBootEntry entry)
        {
            return (entry.Description + " " + entry.Device + " " + entry.Path).ToLowerInvariant();
        }

        private static bool ContainsWord(string text, string word)
        {
            return Regex.IsMatch(text, @"(?:^|[^a-z0-9])" + Regex.Escape(word) + @"(?:$|[^a-z0-9])");
        }

        private static string GetProperty(string text, params string[] names)
        {
            foreach (Match match in PropertyPattern.Matches(text))
            {
                foreach (string name in names)
                {
                    if (string.Equals(match.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return match.Groups["value"].Value.Trim();
                    }
                }
            }

            return string.Empty;
        }
    }
}
