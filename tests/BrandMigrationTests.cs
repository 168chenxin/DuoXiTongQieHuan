using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using SysSwitch;

internal static class BrandMigrationTests
{
    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == BrandMigration.SkipArgument &&
            string.Equals(Path.GetFileName(Assembly.GetExecutingAssembly().Location), "BrandMigrationRecoveryProbe.exe", StringComparison.OrdinalIgnoreCase))
        {
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recovery-marker.txt"), args[0]);
            return 0;
        }

        try
        {
            const string LegacyExecutablePath = @"C:\Apps\DualBootSwitcher.exe";
            const string LegacyShortcutName = "多系统切换.lnk";
            const string targetPath = @"C:\Apps\SysSwitch.exe";

            AssertTrue(BrandMigration.ShouldMigrate(LegacyExecutablePath, new string[0]), "The legacy executable should migrate.");
            AssertTrue(!BrandMigration.ShouldMigrate(targetPath, new string[0]), "The renamed executable should not migrate.");
            AssertTrue(
                !BrandMigration.ShouldMigrate(LegacyExecutablePath, new[] { BrandMigration.SkipArgument }),
                "The recovery launch should skip migration.");
            AssertTrue(
                BrandMigration.IsLegacyUninstallerPath(targetPath, @"C:\Apps\unins000.exe"),
                "A standard Inno Setup uninstaller in the same directory should be accepted.");
            AssertTrue(
                !BrandMigration.IsLegacyUninstallerPath(targetPath, @"C:\Apps\helper.exe"),
                "An arbitrary executable should not be accepted as the legacy uninstaller.");
            AssertTrue(
                !BrandMigration.IsLegacyUninstallerPath(targetPath, @"C:\Other\unins000.exe"),
                "An uninstaller from another directory should not be accepted.");

            string script = BrandMigration.BuildScript(LegacyExecutablePath, targetPath, 1234);
            AssertContains(script, "$ErrorActionPreference = 'Stop'", "File migration errors should enter the recovery path.");
            AssertContains(script, "Wait-Process -Id 1234", "The script should wait for the current process.");
            AssertContains(script, "if (Test-Path -LiteralPath $target) { throw '目标程序已存在。' }", "An existing target executable should not be overwritten.");
            AssertContains(script, "Move-Item -LiteralPath $source -Destination $target", "The script should rename the executable.");
            AssertDoesNotContain(script, "Move-Item -LiteralPath $source -Destination $target -Force", "The executable rename should not force an overwrite.");
            AssertContains(script, LegacyShortcutName, "The script should locate legacy shortcuts.");
            AssertContains(script, "if (-not (Test-Path -LiteralPath $LegacyShortcut)) { continue }", "Missing legacy shortcuts should be ignored.");
            AssertContains(script, "$existingTarget = [IO.Path]::GetFullPath", "The legacy shortcut target should be normalized before migration.");
            AssertContains(script, "if (-not [string]::Equals($existingTarget, $source", "Shortcuts for other installations should be preserved.");
            AssertContains(script, "if (-not [string]::Equals($newTarget, $target", "A new shortcut for another installation should not be overwritten.");
            AssertContains(script, "系统切换大师.lnk", "The script should create renamed shortcuts.");
            AssertContains(script, "Start-Process -FilePath $target", "The script should restart the renamed executable.");
            AssertContains(script, BrandMigration.SkipArgument, "The recovery launch should skip another migration attempt.");
            AssertContains(script, "DisplayName -Value '系统切换大师'", "The installed-app entry should use the new public name.");
            AssertContains(script, BrandMigration.LegacyUninstallArgument, "The installed-app entry should use the compatibility uninstaller.");
            AssertDoesNotContain(script, "HKEY_CURRENT_USER", "The elevated compatibility uninstaller should only trust machine-protected registration.");

            AssertTrue(
                BrandMigration.BuildLegacyUninstallCommand(targetPath, @"C:\Apps\unins000.exe", false) ==
                    "\"C:\\Apps\\SysSwitch.exe\" --uninstall-legacy \"C:\\Apps\\unins000.exe\"",
                "The registered compatibility uninstall command should be deterministic.");

            string cleanupScript = BrandMigration.BuildUninstallCleanupScript(targetPath, 4321);
            AssertContains(cleanupScript, "Wait-Process -Id 4321", "Uninstall cleanup should wait for the compatibility process.");
            AssertContains(cleanupScript, "Remove-Item -LiteralPath $target -Force", "Uninstall cleanup should remove the migrated executable.");
            AssertContains(cleanupScript, "系统切换大师.lnk", "Uninstall cleanup should remove migrated shortcuts.");
            AssertDoesNotContain(cleanupScript, "-Recurse", "Uninstall cleanup should never recursively delete the install directory.");

            string scriptPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".ps1");
            try
            {
                BrandMigration.WriteScript(scriptPath, LegacyExecutablePath, targetPath, 1234);
                byte[] scriptBytes = File.ReadAllBytes(scriptPath);
                AssertTrue(
                    scriptBytes.Length >= 3 && scriptBytes[0] == 0xEF && scriptBytes[1] == 0xBB && scriptBytes[2] == 0xBF,
                    "The migration script should use UTF-8 with BOM so Windows PowerShell preserves Chinese shortcut names.");
            }
            finally
            {
                File.Delete(scriptPath);
            }

            VerifyRecoveryLaunch();
            VerifyUninstallCleanup();

            Console.WriteLine("Brand migration tests passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    private static void VerifyRecoveryLaunch()
    {
        string directory = Path.Combine(Path.GetTempPath(), "SysSwitch-brand-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string sourcePath = Path.Combine(directory, "BrandMigrationRecoveryProbe.exe");
            string targetPath = Path.Combine(directory, "SysSwitch.exe");
            string scriptPath = Path.Combine(directory, "migration.ps1");
            string markerPath = Path.Combine(directory, "recovery-marker.txt");
            File.Copy(Assembly.GetExecutingAssembly().Location, sourcePath);
            File.Copy(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "where.exe"), targetPath);
            BrandMigration.WriteScript(scriptPath, sourcePath, targetPath, int.MaxValue);

            using (var sourceLock = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            }))
            {
                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    throw new InvalidOperationException("The migration recovery script timed out.");
                }
            }

            for (int attempt = 0; attempt < 20 && !File.Exists(markerPath); attempt++)
            {
                Thread.Sleep(100);
            }

            AssertTrue(File.Exists(markerPath), "A failed rename should restart the legacy executable.");
            AssertTrue(File.ReadAllText(markerPath).Trim() == BrandMigration.SkipArgument, "The recovery launch should include the skip argument.");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static void VerifyUninstallCleanup()
    {
        string directory = Path.Combine(Path.GetTempPath(), "SysSwitch-cleanup-test-" + Guid.NewGuid().ToString("N"));
        string targetPath = Path.Combine(directory, "SysSwitch.exe");
        string scriptPath = Path.Combine(Path.GetTempPath(), "SysSwitch-cleanup-test-" + Guid.NewGuid().ToString("N") + ".ps1");
        Directory.CreateDirectory(directory);
        File.WriteAllText(targetPath, "probe");
        try
        {
            File.WriteAllText(scriptPath, BrandMigration.BuildUninstallCleanupScript(targetPath, int.MaxValue));
            using (Process process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            }))
            {
                if (!process.WaitForExit(10000))
                {
                    process.Kill();
                    throw new InvalidOperationException("The uninstall cleanup script timed out.");
                }
            }

            AssertTrue(!File.Exists(targetPath), "Uninstall cleanup should remove the migrated executable.");
            AssertTrue(!Directory.Exists(directory), "Uninstall cleanup should remove the empty install directory.");
            AssertTrue(!File.Exists(scriptPath), "Uninstall cleanup should remove its temporary script.");
        }
        finally
        {
            if (File.Exists(scriptPath))
            {
                File.Delete(scriptPath);
            }

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static void AssertContains(string value, string expected, string message)
    {
        if (value == null || !value.Contains(expected))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertDoesNotContain(string value, string expected, string message)
    {
        if (value != null && value.Contains(expected))
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
