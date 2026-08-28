using System;
using Microsoft.Win32;
using SysSwitch;

internal static class BootRemarkStoreTests
{
    private const string KeyPath = @"Software\SysSwitch\BootRemarks";
    private const string LegacyKeyPath = @"Software\DualBootSwitcher\BootRemarks";

    private static int Main()
    {
        string identifier = "{codex-test-" + Guid.NewGuid().ToString("N") + "}";
        string legacyIdentifier = "{codex-legacy-test-" + Guid.NewGuid().ToString("N") + "}";
        try
        {
            BootRemarkStore.Set(identifier, "测试用途");
            AssertEqual("测试用途", BootRemarkStore.Get(identifier), "Expected the saved remark to be readable.");

            BootRemarkStore.Set(identifier, string.Empty);
            AssertEqual(string.Empty, BootRemarkStore.Get(identifier), "Expected an empty remark to clear the value.");

            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(LegacyKeyPath))
            {
                key.SetValue(legacyIdentifier, "旧版用途", RegistryValueKind.String);
            }
            AssertEqual("旧版用途", BootRemarkStore.Get(legacyIdentifier), "Legacy remarks should be migrated.");
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath, false))
            {
                AssertEqual("旧版用途", key.GetValue(legacyIdentifier, string.Empty).ToString(), "Migrated remarks should use the new key.");
            }
            Console.WriteLine("Boot remark tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
        finally
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(KeyPath, true))
            {
                if (key != null)
                {
                    key.DeleteValue(identifier, false);
                    key.DeleteValue(legacyIdentifier, false);
                }
            }
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(LegacyKeyPath, true))
            {
                if (key != null)
                {
                    key.DeleteValue(legacyIdentifier, false);
                }
            }
        }
    }

    private static void AssertEqual(string expected, string actual, string message)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }
    }
}
