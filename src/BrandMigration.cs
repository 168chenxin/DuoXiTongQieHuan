using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SysSwitch
{
    internal static class BrandMigration
    {
        internal const string SkipArgument = "--skip-brand-migration";
        internal const string LegacyUninstallArgument = "--uninstall-legacy";
        private const string QuietUninstallArgument = "--quiet";
        private const string LegacyExecutableName = "DualBootSwitcher.exe";
        private const string TargetExecutableName = "SysSwitch.exe";
        private const string UninstallKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{7D3F8B5A-BF2D-4BD8-A6C0-1B2B90F9E2C1}_is1";

        internal static bool ShouldMigrate(string executablePath, string[] arguments)
        {
            if (!string.Equals(Path.GetFileName(executablePath), LegacyExecutableName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (arguments != null)
            {
                foreach (string argument in arguments)
                {
                    if (string.Equals(argument, SkipArgument, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static bool TryStart(string[] arguments)
        {
            string sourcePath = Path.GetFullPath(Application.ExecutablePath);
            if (!ShouldMigrate(sourcePath, arguments))
            {
                return false;
            }

            string targetPath = Path.Combine(Path.GetDirectoryName(sourcePath), TargetExecutableName);
            string scriptPath = Path.Combine(Path.GetTempPath(), "SysSwitch-brand-" + Guid.NewGuid().ToString("N") + ".ps1");

            try
            {
                WriteScript(scriptPath, sourcePath, targetPath, Process.GetCurrentProcess().Id);

                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + scriptPath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                return true;
            }
            catch
            {
                try
                {
                    File.Delete(scriptPath);
                }
                catch
                {
                }

                return false;
            }
        }

        internal static bool TryRunLegacyUninstaller(string[] arguments)
        {
            if (arguments == null || arguments.Length < 2 ||
                !string.Equals(arguments[0], LegacyUninstallArgument, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool quiet = arguments.Length > 2 &&
                string.Equals(arguments[2], QuietUninstallArgument, StringComparison.OrdinalIgnoreCase);
            string executablePath = Path.GetFullPath(Application.ExecutablePath);
            string uninstallerPath;
            try
            {
                uninstallerPath = Path.GetFullPath(arguments[1]);
                if (!File.Exists(uninstallerPath) ||
                    !IsLegacyUninstallerPath(executablePath, uninstallerPath) ||
                    !IsRegisteredLegacyUninstaller(executablePath, uninstallerPath, quiet))
                {
                    throw new FileNotFoundException("找不到原安装程序的卸载组件。", uninstallerPath);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = uninstallerPath,
                    UseShellExecute = true
                };
                if (quiet)
                {
                    startInfo.Arguments = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
                }

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return true;
                    }
                }

                string scriptPath = Path.Combine(Path.GetTempPath(), "SysSwitch-uninstall-" + Guid.NewGuid().ToString("N") + ".ps1");
                File.WriteAllText(
                    scriptPath,
                    BuildUninstallCleanupScript(executablePath, Process.GetCurrentProcess().Id),
                    new UTF8Encoding(true));
                Process.Start(new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + scriptPath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception exception)
            {
                if (!quiet)
                {
                    MessageBox.Show(
                        "无法完成卸载。\r\n\r\n" + exception.Message,
                        "系统切换大师",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            return true;
        }

        internal static bool IsLegacyUninstallerPath(string executablePath, string uninstallerPath)
        {
            try
            {
                string executableDirectory = Path.GetDirectoryName(Path.GetFullPath(executablePath));
                string fullUninstallerPath = Path.GetFullPath(uninstallerPath);
                if (!string.Equals(
                    executableDirectory,
                    Path.GetDirectoryName(fullUninstallerPath),
                    StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string fileName = Path.GetFileName(fullUninstallerPath);
                if (!fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase) ||
                    !fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string sequence = fileName.Substring(5, fileName.Length - 9);
                if (sequence.Length == 0)
                {
                    return false;
                }

                foreach (char value in sequence)
                {
                    if (value < '0' || value > '9')
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static string BuildLegacyUninstallCommand(string executablePath, string uninstallerPath, bool quiet)
        {
            string command = "\"" + executablePath + "\" " + LegacyUninstallArgument + " \"" + uninstallerPath + "\"";
            return quiet ? command + " " + QuietUninstallArgument : command;
        }

        private static bool IsRegisteredLegacyUninstaller(string executablePath, string uninstallerPath, bool quiet)
        {
            string expectedCommand = BuildLegacyUninstallCommand(executablePath, uninstallerPath, quiet);
            string valueName = quiet ? "QuietUninstallString" : "UninstallString";
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            {
                try
                {
                    using (RegistryKey baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view))
                    using (RegistryKey key = baseKey.OpenSubKey(UninstallKeyPath))
                    {
                        if (key != null && string.Equals(
                            key.GetValue(valueName) as string,
                            expectedCommand,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                }
            }

            return false;
        }

        internal static string BuildUninstallCleanupScript(string targetPath, int processId)
        {
            return "$target = '" + EscapePowerShellLiteral(targetPath) + "'\r\n" +
                "$targetShortcutName = '系统切换大师.lnk'\r\n" +
                "Wait-Process -Id " + processId + " -ErrorAction SilentlyContinue\r\n" +
                "for ($attempt = 0; $attempt -lt 15 -and (Test-Path -LiteralPath $target); $attempt++) {\r\n" +
                "    Remove-Item -LiteralPath $target -Force -ErrorAction SilentlyContinue\r\n" +
                "    if (Test-Path -LiteralPath $target) { Start-Sleep -Seconds 1 }\r\n" +
                "}\r\n" +
                "if (-not (Test-Path -LiteralPath $target)) {\r\n" +
                "    $shortcutDirectories = @(\r\n" +
                "        [Environment]::GetFolderPath('DesktopDirectory'),\r\n" +
                "        [Environment]::GetFolderPath('CommonDesktopDirectory'),\r\n" +
                "        (Join-Path $env:APPDATA 'Microsoft\\Windows\\Start Menu\\Programs'),\r\n" +
                "        (Join-Path $env:ProgramData 'Microsoft\\Windows\\Start Menu\\Programs')\r\n" +
                "    ) | Where-Object { $_ } | Select-Object -Unique\r\n" +
                "    foreach ($directory in $shortcutDirectories) {\r\n" +
                "        try {\r\n" +
                "            $shortcutPath = Join-Path $directory $targetShortcutName\r\n" +
                "            if (-not (Test-Path -LiteralPath $shortcutPath)) { continue }\r\n" +
                "            $shortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($shortcutPath)\r\n" +
                "            $shortcutTarget = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($shortcut.TargetPath))\r\n" +
                "            if ([string]::Equals($shortcutTarget, $target, [StringComparison]::OrdinalIgnoreCase)) {\r\n" +
                "                Remove-Item -LiteralPath $shortcutPath -Force\r\n" +
                "            }\r\n" +
                "        }\r\n" +
                "        catch {\r\n" +
                "            continue\r\n" +
                "        }\r\n" +
                "    }\r\n" +
                "    Remove-Item -LiteralPath ([IO.Path]::GetDirectoryName($target)) -Force -ErrorAction SilentlyContinue\r\n" +
                "}\r\n" +
                "Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue\r\n";
        }

        internal static void WriteScript(string scriptPath, string sourcePath, string targetPath, int processId)
        {
            File.WriteAllText(
                scriptPath,
                BuildScript(sourcePath, targetPath, processId),
                new UTF8Encoding(true));
        }

        internal static string BuildScript(string sourcePath, string targetPath, int processId)
        {
            return "$ErrorActionPreference = 'Stop'\r\n" +
                "$source = '" + EscapePowerShellLiteral(sourcePath) + "'\r\n" +
                "$target = '" + EscapePowerShellLiteral(targetPath) + "'\r\n" +
                "$LegacyShortcutName = '多系统切换.lnk'\r\n" +
                "$targetShortcutName = '系统切换大师.lnk'\r\n" +
                "Wait-Process -Id " + processId + " -ErrorAction SilentlyContinue\r\n" +
                "try {\r\n" +
                "    if (Test-Path -LiteralPath $target) { throw '目标程序已存在。' }\r\n" +
                "    Move-Item -LiteralPath $source -Destination $target\r\n" +
                "    $shortcutDirectories = @(\r\n" +
                "        [Environment]::GetFolderPath('DesktopDirectory'),\r\n" +
                "        [Environment]::GetFolderPath('CommonDesktopDirectory'),\r\n" +
                "        (Join-Path $env:APPDATA 'Microsoft\\Windows\\Start Menu\\Programs'),\r\n" +
                "        (Join-Path $env:ProgramData 'Microsoft\\Windows\\Start Menu\\Programs')\r\n" +
                "    ) | Where-Object { $_ } | Select-Object -Unique\r\n" +
                "    foreach ($directory in $shortcutDirectories) {\r\n" +
                "        try {\r\n" +
                "            $LegacyShortcut = Join-Path $directory $LegacyShortcutName\r\n" +
                "            if (-not (Test-Path -LiteralPath $LegacyShortcut)) { continue }\r\n" +
                "            $existingShortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($LegacyShortcut)\r\n" +
                "            $existingTarget = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($existingShortcut.TargetPath))\r\n" +
                "            if (-not [string]::Equals($existingTarget, $source, [StringComparison]::OrdinalIgnoreCase)) { continue }\r\n" +
                "            $targetShortcutPath = Join-Path $directory $targetShortcutName\r\n" +
                "            if (Test-Path -LiteralPath $targetShortcutPath) {\r\n" +
                "                $newShortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($targetShortcutPath)\r\n" +
                "                $newTarget = [IO.Path]::GetFullPath([Environment]::ExpandEnvironmentVariables($newShortcut.TargetPath))\r\n" +
                "                if (-not [string]::Equals($newTarget, $target, [StringComparison]::OrdinalIgnoreCase)) { continue }\r\n" +
                "            }\r\n" +
                "            $targetShortcut = (New-Object -ComObject WScript.Shell).CreateShortcut($targetShortcutPath)\r\n" +
                "            $targetShortcut.TargetPath = $target\r\n" +
                "            $targetShortcut.Arguments = $existingShortcut.Arguments\r\n" +
                "            $targetShortcut.WorkingDirectory = [IO.Path]::GetDirectoryName($target)\r\n" +
                "            $targetShortcut.IconLocation = $target + ',0'\r\n" +
                "            $targetShortcut.Description = '系统切换大师'\r\n" +
                "            $targetShortcut.WindowStyle = $existingShortcut.WindowStyle\r\n" +
                "            $targetShortcut.Save()\r\n" +
                "            Remove-Item -LiteralPath $LegacyShortcut -Force\r\n" +
                "        }\r\n" +
                "        catch {\r\n" +
                "            continue\r\n" +
                "        }\r\n" +
                "    }\r\n" +
                "    $installDirectory = [IO.Path]::GetFullPath([IO.Path]::GetDirectoryName($target)).TrimEnd('\\')\r\n" +
                "    $fileVersion = [version](Get-Item -LiteralPath $target).VersionInfo.FileVersion\r\n" +
                "    $displayVersion = '{0}.{1}.{2}' -f $fileVersion.Major, $fileVersion.Minor, $fileVersion.Build\r\n" +
                "    $uninstallKeyPaths = @(\r\n" +
                "        'Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{7D3F8B5A-BF2D-4BD8-A6C0-1B2B90F9E2C1}_is1',\r\n" +
                "        'Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{7D3F8B5A-BF2D-4BD8-A6C0-1B2B90F9E2C1}_is1'\r\n" +
                "    )\r\n" +
                "    foreach ($keyPath in $uninstallKeyPaths) {\r\n" +
                "        try {\r\n" +
                "            if (-not (Test-Path -LiteralPath $keyPath)) { continue }\r\n" +
                "            $entry = Get-ItemProperty -LiteralPath $keyPath\r\n" +
                "            if ([string]::IsNullOrWhiteSpace($entry.InstallLocation) -or [string]::IsNullOrWhiteSpace($entry.UninstallString)) { continue }\r\n" +
                "            $registeredDirectory = [IO.Path]::GetFullPath($entry.InstallLocation).TrimEnd('\\')\r\n" +
                "            if (-not [string]::Equals($registeredDirectory, $installDirectory, [StringComparison]::OrdinalIgnoreCase)) { continue }\r\n" +
                "            $uninstallerMatch = [regex]::Match($entry.UninstallString, '^\\s*\"([^\"]+)\"')\r\n" +
                "            if (-not $uninstallerMatch.Success) { continue }\r\n" +
                "            $uninstaller = $uninstallerMatch.Groups[1].Value\r\n" +
                "            if (-not (Test-Path -LiteralPath $uninstaller -PathType Leaf)) { continue }\r\n" +
                "            $uninstallCommand = '\"' + $target + '\" " + LegacyUninstallArgument + " \"' + $uninstaller + '\"'\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name DisplayName -Value '系统切换大师'\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name DisplayVersion -Value $displayVersion\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name DisplayIcon -Value ($target + ',0')\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name Publisher -Value '称心'\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name URLInfoAbout -Value 'https://github.com/168chenxin/SysSwitch-Master'\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name UninstallString -Value $uninstallCommand\r\n" +
                "            Set-ItemProperty -LiteralPath $keyPath -Name QuietUninstallString -Value ($uninstallCommand + ' " + QuietUninstallArgument + "')\r\n" +
                "        }\r\n" +
                "        catch {\r\n" +
                "            continue\r\n" +
                "        }\r\n" +
                "    }\r\n" +
                "    Start-Process -FilePath $target\r\n" +
                "}\r\n" +
                "catch {\r\n" +
                "    if (Test-Path -LiteralPath $source) {\r\n" +
                "        Start-Process -FilePath $source -ArgumentList '" + SkipArgument + "'\r\n" +
                "    }\r\n" +
                "    elseif (Test-Path -LiteralPath $target) {\r\n" +
                "        Start-Process -FilePath $target\r\n" +
                "    }\r\n" +
                "}\r\n" +
                "finally {\r\n" +
                "    Remove-Item -LiteralPath $PSCommandPath -Force -ErrorAction SilentlyContinue\r\n" +
                "}\r\n";
        }

        private static string EscapePowerShellLiteral(string value)
        {
            return (value ?? string.Empty).Replace("'", "''");
        }
    }
}
