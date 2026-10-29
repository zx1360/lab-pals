using System;
using System.IO;
using System.Xml.Serialization;

namespace LabPals
{
    public sealed class PetSettings
    {
        public int Version = 1;
        public Character Character = Character.Luka;
        public float Scale = 0.75f;
        public bool Quiet;
        public bool Keyboard = true;
        public bool AutoSleep = true;
        public bool TopMost = true;
        public bool ClickThrough;
        public int Left = int.MinValue;
        public int Top = int.MinValue;

        public static string SettingsPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LabPals", "settings.xml"); }
        }
        public static PetSettings Load() { return LoadFrom(SettingsPath); }
        internal static PetSettings LoadFrom(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var result = (PetSettings)new XmlSerializer(typeof(PetSettings)).Deserialize(stream);
                    result.Normalize();
                    return result;
                }
            }
            catch { return new PetSettings(); }
        }
        internal void Normalize()
        {
            if (!CharacterCatalog.Contains(Character)) Character = Character.Luka;
            if (float.IsNaN(Scale) || float.IsInfinity(Scale) || Scale < 0.5f || Scale > 1.5f) Scale = 0.75f;
            if (Left == int.MinValue || Top == int.MinValue) Left = Top = int.MinValue;
        }
        public bool Save() { return SaveTo(SettingsPath); }
        internal bool SaveTo(string path)
        {
            string temporary = path + ".tmp";
            try
            {
                Normalize();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                using (var stream = File.Create(temporary))
                    new XmlSerializer(typeof(PetSettings)).Serialize(stream, this);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch { return false; }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
        }
    }
}
