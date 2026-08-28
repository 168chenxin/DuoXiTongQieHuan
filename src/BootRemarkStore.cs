using System;
using Microsoft.Win32;

namespace SysSwitch
{
    internal static class BootRemarkStore
    {
        private const string KeyPath = @"Software\SysSwitch\BootRemarks";
        private const string LegacyKeyPath = @"Software\DualBootSwitcher\BootRemarks";

        public static string Get(string identifier)
        {
            string valueName = GetValueName(identifier);
            if (valueName == null)
            {
                return string.Empty;
            }

            try
            {
                string value = ReadValue(KeyPath, valueName);
                if (value.Length == 0)
                {
                    value = ReadValue(LegacyKeyPath, valueName);
                    if (value.Length != 0)
                    {
                        try
                        {
                            Set(identifier, value);
                        }
                        catch (InvalidOperationException)
                        {
                        }
                    }
                }

                return value;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public static void Set(string identifier, string remark)
        {
            string valueName = GetValueName(identifier);
            if (valueName == null)
            {
                throw new ArgumentException("启动项标识符不能为空。", "identifier");
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(KeyPath))
                {
                    if (key == null)
                    {
                        throw new InvalidOperationException("无法打开当前用户设置存储。");
                    }

                    string normalizedRemark = (remark ?? string.Empty).Trim();
                    if (normalizedRemark.Length == 0)
                    {
                        key.DeleteValue(valueName, false);
                        using (RegistryKey legacyKey = Registry.CurrentUser.OpenSubKey(LegacyKeyPath, true))
                        {
                            if (legacyKey != null)
                            {
                                legacyKey.DeleteValue(valueName, false);
                            }
                        }
                        return;
                    }

                    key.SetValue(valueName, normalizedRemark, RegistryValueKind.String);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("保存启动项备注失败。", exception);
            }
        }

        private static string GetValueName(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return null;
            }

            return identifier.Trim().Replace("\\", "_");
        }

        private static string ReadValue(string keyPath, string valueName)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, false))
            {
                object value = key == null ? null : key.GetValue(valueName, string.Empty);
                return value == null ? string.Empty : value.ToString().Trim();
            }
        }
    }
}
