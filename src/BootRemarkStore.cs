using System;
using Microsoft.Win32;

namespace DualBootSwitcher
{
    internal static class BootRemarkStore
    {
        private const string KeyPath = @"Software\DualBootSwitcher\BootRemarks";

        public static string Get(string identifier)
        {
            string valueName = GetValueName(identifier);
            if (valueName == null)
            {
                return string.Empty;
            }

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath, false))
                {
                    if (key == null)
                    {
                        return string.Empty;
                    }

                    object value = key.GetValue(valueName, string.Empty);
                    return value == null ? string.Empty : value.ToString().Trim();
                }
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
    }
}
