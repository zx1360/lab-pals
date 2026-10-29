using System;
using System.Drawing;
using System.Windows.Forms;

namespace LabPals
{
    internal static class DiagnosticsProgram
    {
        [STAThread]
        private static int Main(string[] args)
        {
            Native.SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                if (args.Length == 2 && args[0] == "--self-test") SelfTest.Run(args[1]);
                else if (args.Length == 2 && args[0] == "--motion-test") MotionBenchmark.Run(args[1]);
                else if (args.Length == 2 && args[0] == "--burst-test") MotionBenchmark.RunBurst(args[1]);
                else if (args.Length == 2 && (args[0] == "--preview" || args[0] == "--preview-frames"))
                    PreviewGallery.Export(args[1], args[0] == "--preview-frames");
                else if (args.Length == 1 && args[0] == "--review")
                {
                    using (var pet = new PetWindow(new PetSettings(), false))
                    {
                        pet.ReviewMode = true;
                        pet.Shown += delegate { pet.BeginInvoke((Action)delegate { pet.ShowPanel(new Point(pet.Right, pet.Bottom)); }); };
                        Application.Run(pet);
                    }
                }
                else return 2;
                return 0;
            }
            catch (Exception error)
            {
                ErrorLog.TryWrite(ErrorLog.DefaultPath, error);
                if (args.Length >= 2) ErrorLog.TryWrite(args[1] + ".error.txt", error);
                return 1;
            }
            finally { PetRenderer.ClearCache(); }
        }
    }
}
