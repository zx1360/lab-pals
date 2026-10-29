using System;
using System.Threading;
using System.Windows.Forms;

namespace LabPals
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (args.Length != 0) return 2;
                bool first;
                using (var mutex = new Mutex(true, "Local\\LabPals.DesktopPet.v1", out first))
                {
                    if (!first)
                    {
                        Native.PostMessage(new IntPtr(0xFFFF), Native.RestoreMessage, IntPtr.Zero, IntPtr.Zero); return 0;
                    }
                    using (var window = new PetWindow(PetSettings.Load())) Application.Run(window);
                }
                return 0;
            }
            catch (Exception error)
            {
                ErrorLog.TryWrite(ErrorLog.DefaultPath, error);
                MessageBox.Show("同桌暂时没能启动。\n" + error.Message + "\n\n诊断信息保存在：" + ErrorLog.DefaultPath, "实验室同桌", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return 1;
            }
            finally { PetRenderer.ClearCache(); }
        }

    }
}
