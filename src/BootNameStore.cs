using System;
using Microsoft.Win32;

namespace SysSwitch
{
    internal static class BootNameStore
    {
        private const string KeyPath = @"Software\SysSwitch\BootNames";
        private const string LegacyKeyPath = @"Software\DualBootSwitcher\BootNames";

        public static string GetOriginal(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return string.Empty;
            }

            string value = ReadValue(KeyPath, identifier);
            if (string.IsNullOrWhiteSpace(value))
            {
                value = ReadValue(LegacyKeyPath, identifier);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    try
                    {
                        using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                        {
                            if (key != null)
                            {
                                key.SetValue(identifier, value, RegistryValueKind.String);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return value;
        }

        public static void RememberOriginal(string identifier, string description)
        {
            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(description) ||
                !string.IsNullOrWhiteSpace(GetOriginal(identifier)))
            {
                return;
            }

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
            {
                if (key == null)
                {
                    throw new InvalidOperationException("无法保存启动项原名称。");
                }

                key.SetValue(identifier, description.Trim(), RegistryValueKind.String);
            }
        }

        public static void ClearOriginal(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return;
            }

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath, true))
            {
                if (key != null)
                {
                    key.DeleteValue(identifier, false);
                }
            }
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(LegacyKeyPath, true))
            {
                if (key != null)
                {
                    key.DeleteValue(identifier, false);
                }
            }
        }

        private static string ReadValue(string keyPath, string identifier)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, false))
            {
                object value = key == null ? null : key.GetValue(identifier, null);
                return value == null ? string.Empty : value.ToString();
            }
        }
    }
}
