using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace LabPals
{
    internal static class PreviewGallery
    {
        internal static void Export(string folder, bool includeFrames)
        {
            Directory.CreateDirectory(folder);
            if (includeFrames)
            foreach (Character character in Enum.GetValues(typeof(Character)))
                foreach (Pose pose in Enum.GetValues(typeof(Pose)))
                    for (int phase = 0; phase < (pose == Pose.Sleep ? AnimationTiming.SleepPhases + 1 : pose == Pose.Pat ? AnimationTiming.PatPhases + 1 : 1); phase++)
                        using (var image = PetRenderer.Render(character, pose, phase, 1, null))
                            image.Save(Path.Combine(folder, character + "-" + pose + "-" + phase + ".png"), ImageFormat.Png);
            using (var icon = PetRenderer.MakeIcon()) using (var stream = File.Create(Path.Combine(folder, "lab-pals.ico"))) icon.Save(stream);
            DrawGallery(Path.Combine(folder, "角色一览.png"));
            DrawStates(Path.Combine(folder, "动作预览.png"));
            if (includeFrames) PreviewExports.ExportTyping(folder);
            PreviewExports.ExportPersonalities(folder);
            PreviewExports.ExportSleep(folder);
            PreviewExports.ExportMayuriDetails(folder);
            if (includeFrames) PreviewExports.ExportInteraction(folder);
            using (var pet = new PetWindow(new PetSettings(), false))
            using (var panel = new CompanionPanel(pet, new PetSettings(), new PetBrain(0)))
            { panel.Show(); Application.DoEvents(); using (var image = panel.Snapshot()) image.Save(Path.Combine(folder, "设置面板.png"), ImageFormat.Png); panel.Close(); }
        }
        private static void DrawGallery(string path)
        {
            using (var image = new Bitmap(80 + 340 * CharacterCatalog.All.Count, 660))
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(246, 243, 235)); g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                Text(g, "LAB PALS", "Segoe UI", 15, FontStyle.Bold, Color.FromArgb(125, 145, 131), 58, 40);
                Text(g, "实验室同桌", "Microsoft YaHei UI", 38, FontStyle.Bold, PetRenderer.Ink, 52, 75);
                Text(g, "留住原作里的小特征，安静陪你读完这一页。", "Microsoft YaHei UI", 17, FontStyle.Regular, Color.FromArgb(138, 129, 122), 58, 143);
                for (int i = 0; i < CharacterCatalog.All.Count; i++)
                {
                    int x = 45 + i * 340;
                    using (var card = PetRenderer.Rounded(x, 204, 326, 355, 18))
                    using (var brush = new SolidBrush(Color.FromArgb(255, 252, 246))) g.FillPath(brush, card);
                    var state = g.Save(); g.TranslateTransform(x + 3, 207); PetRenderer.Draw(g, CharacterCatalog.All[i].Id, Pose.Idle, 0, null); g.Restore(state);
                    Text(g, CharacterCatalog.All[i].Name, "Microsoft YaHei UI", 21, FontStyle.Bold, PetRenderer.Ink, x + 38, 486);
                    Text(g, "0" + (i + 1) + "  /  " + CharacterCatalog.All[i].English, "Segoe UI", 12, FontStyle.Bold, CharacterCatalog.All[i].Accent, x + 40, 522);
                    Text(g, CharacterCatalog.All[i].Traits, "Microsoft YaHei UI", 11, FontStyle.Regular, Color.FromArgb(145,134,125), x + 38, 545);
                }
                Text(g, "轻点摸摸    ·    键盘敲敲    ·    安静陪读    ·    困了就睡", "Microsoft YaHei UI", 15, FontStyle.Regular, Color.FromArgb(138, 129, 122), 58, 604);
                Text(g, "WINDOWS  /  v" + typeof(PetRenderer).Assembly.GetName().Version.ToString(3), "Segoe UI", 12, FontStyle.Regular, Color.FromArgb(155, 147, 136), 1211, 611);
                image.Save(path, ImageFormat.Png);
            }
        }
        private static void DrawStates(string path)
        {
            using (var image = new Bitmap(1280, 470))
            using (var g = Graphics.FromImage(image))
            {
                g.Clear(Color.FromArgb(246, 243, 235));
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                Text(g, "小动作，轻一点。", "Microsoft YaHei UI", 28, FontStyle.Bold, PetRenderer.Ink, 32, 25);
                Pose[] poses = { Pose.Idle, Pose.TapLeft, Pose.Pat, Pose.Sleep };
                string[] names = { "待机 · 偶尔眨眼", "敲敲 · 跟随输入", "摸摸 · 点击回应", "睡眠 · 安静休息" };
                for (int i = 0; i < poses.Length; i++)
                {
                    var state = g.Save(); g.TranslateTransform(i * 320, 89);
                    PetRenderer.Draw(g, Character.Mayuri, poses[i], poses[i] == Pose.Sleep ? AnimationTiming.SleepPhases : i == 2 ? 10 : 0, null); g.Restore(state);
                    Text(g, names[i], "Microsoft YaHei UI", 17, FontStyle.Regular, PetRenderer.Ink, i * 320 + 68, 381);
                }
                image.Save(path, ImageFormat.Png);
            }
        }
        private static void Text(Graphics g, string text, string family, int size, FontStyle style, Color color, int x, int y)
        {
            using (var font = new Font(family, size, style, GraphicsUnit.Pixel))
            using (var brush = new SolidBrush(color)) g.DrawString(text, font, brush, x, y);
        }
    }
}
