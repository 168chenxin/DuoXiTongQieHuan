using System;
using Microsoft.Win32;
using DualBootSwitcher;

internal static class BootNameStoreTests
{
    private const string KeyPath = @"Software\DualBootSwitcher\BootNames";

    private static int Main()
    {
        string identifier = "{codex-name-test-" + Guid.NewGuid().ToString("N") + "}";
        try
        {
            BootNameStore.RememberOriginal(identifier, "原始系统");
            BootNameStore.RememberOriginal(identifier, "不应覆盖");
            AssertEqual("原始系统", BootNameStore.GetOriginal(identifier), "The first original name should be retained.");
            BootNameStore.ClearOriginal(identifier);
            AssertEqual(string.Empty, BootNameStore.GetOriginal(identifier), "Clearing the original name should remove it.");
            Console.WriteLine("Boot name store tests passed.");
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
