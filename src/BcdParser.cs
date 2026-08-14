using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DualBootSwitcher
{
    internal static class BcdParser
    {
        private static readonly Regex BlockSeparator = new Regex(@"(?:\r?\n\s*){2,}");
        private static readonly Regex PropertyPattern = new Regex(
            @"^\s*(?<name>identifier|description|device|default|标识符|描述|设备|默认)\s+(?<value>.+?)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        private static readonly Regex DisplayOrderPattern = new Regex(
            @"^[ \t]*(?:displayorder|显示顺序)[ \t]+(?<identifiers>\{[^}\r\n]+\}(?:\r?\n[ \t]+\{[^}\r\n]+\})*)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        private static readonly Regex IdentifierPattern = new Regex(@"\{[^}\r\n]+\}");
        private static readonly Regex TimeoutPattern = new Regex(
            @"^\s*(?:timeout|超时)\s+(?<seconds>\d+)\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);
        private static readonly Regex BootSequencePattern = new Regex(
            @"^[ \t]*(?:bootsequence|启动顺序)[ \t]+(?<identifiers>\{[^}\r\n]+\}(?:\r?\n[ \t]+\{[^}\r\n]+\})*)",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant);

        public static List<BootEntry> ParseBootLoaders(string output)
        {
            var entries = new List<BootEntry>();

            if (string.IsNullOrWhiteSpace(output))
            {
                return entries;
            }

            foreach (string block in BlockSeparator.Split(output.Trim()))
            {
                string identifier = GetProperty(block, "identifier", "标识符");
                string description = GetProperty(block, "description", "描述");

                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(description))
                {
                    continue;
                }

                string device = GetProperty(block, "device", "设备");
                entries.Add(new BootEntry(identifier, description, FormatDevice(device)));
            }

            return entries;
        }

        public static string ParseDefaultIdentifier(string output)
        {
            return GetProperty(output, "default", "默认");
        }

        public static List<string> ParseDisplayOrder(string output)
        {
            var identifiers = new List<string>();

            if (string.IsNullOrWhiteSpace(output))
            {
                return identifiers;
            }

            Match displayOrder = DisplayOrderPattern.Match(output);
            if (!displayOrder.Success)
            {
                return identifiers;
            }

            foreach (Match identifier in IdentifierPattern.Matches(displayOrder.Groups["identifiers"].Value))
            {
                identifiers.Add(identifier.Value);
            }

            return identifiers;
        }

        public static int ParseTimeout(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return -1;
            }

            Match timeout = TimeoutPattern.Match(output);
            int seconds;
            if (!timeout.Success || !int.TryParse(
                timeout.Groups["seconds"].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out seconds))
            {
                return -1;
            }

            return seconds;
        }

        public static bool BootSequenceContains(string output, string identifier)
        {
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            Match bootSequence = BootSequencePattern.Match(output);
            if (!bootSequence.Success)
            {
                return false;
            }

            foreach (Match value in IdentifierPattern.Matches(bootSequence.Groups["identifiers"].Value))
            {
                if (IdentifiersMatch(value.Value, identifier))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IdentifiersMatch(string first, string second)
        {
            return string.Equals(
                first == null ? string.Empty : first.Trim(),
                second == null ? string.Empty : second.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string GetProperty(string text, params string[] names)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

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

        private static string FormatDevice(string device)
        {
            if (string.IsNullOrWhiteSpace(device))
            {
                return "未识别分区";
            }

            const string partitionPrefix = "partition=";
            if (device.StartsWith(partitionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return device.Substring(partitionPrefix.Length);
            }

            return device;
        }
    }
}
