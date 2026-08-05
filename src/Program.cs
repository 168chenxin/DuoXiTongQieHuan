using System;
using System.Security.Principal;
using System.Windows.Forms;

namespace DualBootSwitcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            if (!IsAdministrator())
            {
                MessageBox.Show(
                    "请以管理员身份运行此程序。",
                    "双系统快速切换",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
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
