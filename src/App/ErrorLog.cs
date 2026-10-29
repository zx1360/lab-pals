using System;
using System.IO;

namespace LabPals
{
    internal static class ErrorLog
    {
        internal static string DefaultPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabPals", "error.log"); }
        }
        internal static bool TryWrite(string path, Exception error)
        {
            try
            {
                string absolute = Path.GetFullPath(path);
                Directory.CreateDirectory(Path.GetDirectoryName(absolute));
                File.WriteAllText(absolute, error.ToString());
                return true;
            }
            catch { return false; }
        }
    }
}
