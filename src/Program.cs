using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace SysSwitch
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            EmbeddedAssemblyLoader.Register();

            if (!IsAdministrator())
            {
                RequestAdministratorAccess(args);
                return;
            }

            if (BrandMigration.TryRunLegacyUninstaller(args))
            {
                return;
            }

            if (BrandMigration.TryStart(args))
            {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AntdUiTheme.Configure();
            Application.Run(new MainForm());
        }

        private static void RequestAdministratorAccess(string[] arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = BuildArguments(arguments),
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (Process elevatedProcess = Process.Start(startInfo))
                {
                }
            }
            catch (Win32Exception exception)
            {
                if (exception.NativeErrorCode != 1223)
                {
                    ShowElevationError(exception.Message);
                }
            }
            catch (Exception exception)
            {
                ShowElevationError(exception.Message);
            }
        }

        private static string BuildArguments(string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
            {
                return string.Empty;
            }

            string[] quotedArguments = new string[arguments.Length];
            for (int index = 0; index < arguments.Length; index++)
            {
                quotedArguments[index] = "\"" + (arguments[index] ?? string.Empty).Replace("\"", "\\\"") + "\"";
            }

            return string.Join(" ", quotedArguments);
        }

        private static void ShowElevationError(string details)
        {
            MessageBox.Show(
                "无法获取管理员权限。\r\n\r\n" + details,
                "系统切换大师",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }
}
