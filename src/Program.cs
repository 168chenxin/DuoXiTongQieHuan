using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            EmbeddedAssemblyLoader.Register();

            if (!IsAdministrator())
            {
                RequestAdministratorAccess();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AntdUiTheme.Configure();
            Application.Run(new MainForm());
        }

        private static void RequestAdministratorAccess()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
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

        private static void ShowElevationError(string details)
        {
            MessageBox.Show(
                "无法获取管理员权限。\r\n\r\n" + details,
                "多系统切换",
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
