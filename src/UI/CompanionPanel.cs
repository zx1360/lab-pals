using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LabPals
{
    // A regular focusable window gives Windows ownership of outside-click dismissal.
    internal sealed class CompanionPanel : Form
    {
        private readonly PetWindow pet;
        private readonly PetSettings settings;
        private readonly PetBrain brain;
        private readonly bool review;
        private bool rebuilding;
        private bool advanced;
        private readonly Font titleFont;
        private static readonly Color TextColor = Color.FromArgb(48, 50, 55);
        private static readonly Color Muted = Color.FromArgb(125, 128, 134);
        private static readonly Color Accent = Color.FromArgb(103, 119, 138);
        internal CompanionPanel(PetWindow pet, PetSettings settings, PetBrain brain, bool review = false)
        {
            this.pet = pet; this.settings = settings; this.brain = brain; this.review = review;
            Text = "实验室同桌 · 设置";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = review; MinimizeBox = false; MaximizeBox = false;
            StartPosition = FormStartPosition.Manual; TopMost = true;
            AutoScaleMode = AutoScaleMode.None;
            Font = new Font("Microsoft YaHei UI", 9F);
            titleFont = new Font(Font, FontStyle.Bold);
            BackColor = Color.FromArgb(252, 252, 253);
            ForeColor = TextColor;
            DoubleBuffered = true; KeyPreview = true;
            Build();
        }
        protected override CreateParams CreateParams
        {
            get { var cp = base.CreateParams; if (!review) cp.ExStyle |= 0x80; cp.ClassStyle |= 0x20000; return cp; }
        }
        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            if (!rebuilding && Visible) Close();
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { e.Handled = true; Close(); }
            base.OnKeyDown(e);
        }
        protected override bool ProcessCmdKey(ref Message message, Keys keyData)
        {
            if (keyData == Keys.Escape) { Close(); return true; }
            return base.ProcessCmdKey(ref message, keyData);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(226, 228, 232)))
                e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
        private void LabelAt(string text, int x, int y, int w, Color color, bool bold)
        {
            var label = new Label { Text = text, Left = x, Top = y, Width = w, Height = 24, ForeColor = color,
                Font = bold ? titleFont : Font, BackColor = Color.Transparent };
            Controls.Add(label);
        }
        private PanelButton ButtonAt(string text, int x, int y, int w, int h, Action action, bool selected)
        {
            var b = new PanelButton { Text = text, Left = x, Top = y, Width = w, Height = h, Selected = selected,
                Font = Font, ForeColor = TextColor, Accent = Accent, AccessibleName = text, TabStop = true };
            b.Click += delegate { action(); };
            Controls.Add(b); return b;
        }
        private void ToggleAt(string title, string detail, int y, bool value, Action action)
        {
            var b = ButtonAt(title, 20, y, 288, 50, delegate { action(); if (!IsDisposed) Build(); }, value);
            b.Toggle = true; b.Detail = detail; b.AccessibleDescription = detail + (value ? "，已开启" : "，已关闭");
        }
        private void Build()
        {
            rebuilding = true;
            SuspendLayout();
            while (Controls.Count > 0) { var control = Controls[0]; Controls.RemoveAt(0); control.Dispose(); }
            int characterOffset = ((CharacterCatalog.All.Count + 3) / 4 - 1) * 44;
            ClientSize = new Size(328, (advanced ? 654 : 486) + characterOffset);
            LabelAt("实验室同桌", 22, 19, 236, TextColor, true);
            LabelAt("陪你把这一页读完", 22, 45, 248, Muted, false);
            ButtonAt("×", 280, 14, 30, 30, Close, false);
            for (int i = 0; i < CharacterCatalog.All.Count; i++)
            {
                var character = CharacterCatalog.All[i];
                var b = ButtonAt(character.ShortName, 20 + (i % 4) * 74, 86 + (i / 4) * 44, 66, 36,
                    delegate { pet.SetCharacter(character.Id); Build(); }, settings.Character == character.Id);
                b.Accent = character.Accent;
            }
            ToggleAt("键盘跟随", "按下落掌，松开回弹", 141 + characterOffset, settings.Keyboard,
                delegate { pet.SetKeyboard(!settings.Keyboard); });
            ToggleAt("安静陪伴", "暂停敲击，保留偶尔眨眼", 198 + characterOffset, settings.Quiet,
                delegate { pet.SetQuiet(!settings.Quiet); });
            LabelAt("大小", 22, 270 + characterOffset, 50, Muted, false);
            float[] scales = { .55f, .75f, 1f, 1.25f };
            string[] sizes = { "迷你", "小", "标准", "大" };
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                ButtonAt(sizes[i], 78 + i * 59, 263 + characterOffset, 53, 32,
                    delegate { pet.ChangeScale(scales[index]); Build(); }, Math.Abs(settings.Scale - scales[i]) < .01);
            }
            LabelAt(brain.Focusing ? "专注中 · 结束时安静提醒" : "一起专注", 22, 318 + characterOffset, 280, Muted, false);
            if (brain.Focusing)
                ButtonAt("结束本次专注", 20, 345 + characterOffset, 288, 34, delegate { pet.StopFocus(); Build(); }, false);
            else
            {
                int[] minutes = { 25, 45, 60 };
                for (int i = 0; i < 3; i++)
                {
                    int duration = minutes[i];
                    ButtonAt(duration + " 分钟", 20 + i * 98, 345 + characterOffset, 92, 34,
                        delegate { pet.StartFocus(duration); Build(); }, false);
                }
            }
            ButtonAt(brain.Pose == Pose.Sleep ? "叫醒同桌" : "休息一会儿", 20, 397 + characterOffset, 140, 34,
                delegate { pet.ToggleSleep(); Build(); }, brain.Pose == Pose.Sleep);
            ButtonAt(advanced ? "收起设置 ↑" : "更多设置 ↓", 168, 397 + characterOffset, 140, 34,
                delegate { advanced = !advanced; Build(); KeepOnScreen(); }, false);
            int footer = 448 + characterOffset;
            if (advanced)
            {
                ToggleAt("始终置顶", "返回桌面后仍然陪伴", 442 + characterOffset, settings.TopMost,
                    delegate { pet.SetAlwaysOnTop(!settings.TopMost); });
                ToggleAt("自动休息", "3 分钟没有输入后入睡", 496 + characterOffset, settings.AutoSleep,
                    delegate { pet.SetAutoSleep(!settings.AutoSleep); });
                ToggleAt("鼠标穿透", "通过托盘图标重新打开设置", 550 + characterOffset, settings.ClickThrough,
                    delegate { pet.SetClickThrough(!settings.ClickThrough); });
                footer += 168;
            }
            ButtonAt("收起到托盘", 20, footer, 122, 26, delegate { Close(); pet.HidePet(); }, false).Subtle = true;
            ButtonAt("找回位置", 154, footer, 82, 26, delegate { pet.ResetPosition(); Close(); }, false).Subtle = true;
            ButtonAt("退出", 254, footer, 54, 26, delegate { Close(); pet.Close(); }, false).Subtle = true;
            ResumeLayout(); rebuilding = false; Invalidate();
        }
        internal void KeepOnScreen()
        {
            var area = Screen.FromPoint(Location).WorkingArea;
            Left = Math.Max(area.Left + 8, Math.Min(area.Right - Width - 8, Left));
            Top = Math.Max(area.Top + 8, Math.Min(area.Bottom - Height - 8, Top));
        }
        internal Bitmap Snapshot()
        {
            var image = new Bitmap(Width, Height); DrawToBitmap(image, new Rectangle(0, 0, Width, Height)); return image;
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int preference = 2; Native.DwmSetWindowAttribute(Handle, 33, ref preference, 4); } catch (DllNotFoundException) { }
        }
        protected override void Dispose(bool disposing)
        { if (disposing) titleFont.Dispose(); base.Dispose(disposing); }
    }

    internal sealed class PanelButton : Button
    {
        internal bool Selected, Toggle, Subtle;
        internal string Detail;
        internal Color Accent;
        private bool hovered;
        public PanelButton()
        {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { hovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Parent.BackColor);
            Color fill = Subtle ? Parent.BackColor : Selected && !Toggle ? Color.FromArgb(24, Accent) : Color.FromArgb(243, 244, 247);
            if (hovered) fill = Color.FromArgb(232, 235, 240);
            using (var path = PetRenderer.Rounded(0, 0, Width - 1, Height - 1, 7))
            {
                using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                if (Focused && ShowFocusCues) using (var pen = new Pen(Accent)) g.DrawPath(pen, path);
            }
            if (Toggle)
            {
                TextRenderer.DrawText(g, Text, Font, new Rectangle(12, 6, Width - 70, 21), ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                using (var detailFont = new Font(Font.FontFamily, 8))
                    TextRenderer.DrawText(g, Detail, detailFont, new Rectangle(12, 27, Width - 60, 18), Color.FromArgb(128, 131, 138), TextFormatFlags.NoPrefix);
                using (var path = PetRenderer.Rounded(Width - 48, 15, 34, 20, 10))
                using (var brush = new SolidBrush(Selected ? Accent : Color.FromArgb(201, 205, 213))) g.FillPath(brush, path);
                using (var brush = new SolidBrush(Color.White)) g.FillEllipse(brush, Width - (Selected ? 31 : 45), 18, 14, 14);
            }
            else TextRenderer.DrawText(g, Text, Font, ClientRectangle, Selected ? Accent : ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
