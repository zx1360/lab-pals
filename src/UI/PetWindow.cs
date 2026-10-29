using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace LabPals
{
    public sealed class PetWindow : Form
    {
        private readonly PetSettings settings;
        private readonly PetBrain brain;
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly Timer scheduler = new Timer();
        private readonly NotifyIcon tray;
        private readonly Icon petIcon;
        private readonly bool persistSettings;
        private CompanionPanel panel;
        private bool dragging, moved, hidden, keyboardRegistered, disposed;
        private Point downScreen, downWindow;
        private readonly FocusSession focus = new FocusSession();
        private string frameKey;
        private long scheduledAt = long.MaxValue;
        private Native.LayeredSurface surface;
        public int RenderCount { get; private set; }
        internal PetBrain Brain { get { return brain; } }
        internal CompanionPanel SettingsPanel { get { return panel; } }
        internal bool ReviewMode { get; set; }

        public PetWindow(PetSettings settings) : this(settings, true) { }
        internal PetWindow(PetSettings settings, bool persistSettings)
        {
            this.settings = settings; this.persistSettings = persistSettings;
            brain = new PetBrain(0) { Quiet = settings.Quiet, AutoSleep = settings.AutoSleep, KeyboardFeedback = settings.Keyboard };
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false; MinimizeBox = false; MaximizeBox = false;
            TopMost = settings.TopMost;
            Text = "Lab Pals · 实验室同桌";
            Size = new Size((int)Math.Ceiling(PetRenderer.Width * settings.Scale), (int)Math.Ceiling(PetRenderer.Height * settings.Scale));
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = settings.Left == int.MinValue ? new Point(area.Right - Width - 36, area.Bottom - Height - 10) : new Point(settings.Left, settings.Top);
            ClampPosition();
            petIcon = PetRenderer.MakeIcon(); Icon = petIcon;
            tray = new NotifyIcon { Icon = petIcon, Text = "实验室同桌 · 右键设置 / 双击找回", Visible = true };
            tray.DoubleClick += delegate { ShowPet(); };
            tray.MouseUp += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Right) ShowPanel(Cursor.Position); };
            scheduler.Tick += OnScheduled;
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged;
            Microsoft.Win32.SystemEvents.SessionSwitch += OnSessionSwitch;
        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.Style &= ~0x30000; // No minimize/maximize: Show Desktop must not iconize a companion.
                cp.ExStyle |= 0x80000 | 0x80 | 0x8000000;
                if (settings != null && settings.ClickThrough) cp.ExStyle |= 0x20;
                return cp;
            }
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e); SyncKeyboard(); RenderFrame(true); Schedule();
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == Native.RestoreMessage) { ShowPet(); message.Result = IntPtr.Zero; return; }
            if (message.Msg == 0x21) { message.Result = new IntPtr(3); return; }
            if (message.Msg == 0x112 && ((long)message.WParam & 0xFFF0) == 0xF020) { message.Result = IntPtr.Zero; return; }
            Native.KeyboardTransition stroke;
            if (message.Msg == 0xFF && Native.ReadKeyboardTransition(message.LParam, out stroke))
                ProcessStroke(stroke.Identity, stroke.HandMask, stroke.Down);
            base.WndProc(ref message);
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && !hidden && !disposed && IsHandleCreated)
                BeginInvoke((Action)delegate { if (!hidden && !disposed) { WindowState = FormWindowState.Normal; RenderFrame(true); } });
        }
        internal bool ProcessStroke(int identity, int handMask, bool down)
        {
            if (!brain.KeyTransition(identity, handMask, down, clock.ElapsedMilliseconds)) return false;
            RenderFrame(true); Schedule(); return true;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (panel != null && !panel.IsDisposed) panel.Close();
            if (e.Button == MouseButtons.Left)
            { downScreen = Cursor.Position; downWindow = Location; dragging = true; moved = false; Capture = true; }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!dragging) return;
            var p = Cursor.Position;
            int dx = p.X - downScreen.X, dy = p.Y - downScreen.Y;
            if (Math.Abs(dx) + Math.Abs(dy) > 5) moved = true;
            if (moved) Location = new Point(downWindow.X + dx, downWindow.Y + dy);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Right) { ShowPanel(Cursor.Position); return; }
            if (e.Button != MouseButtons.Left || !dragging) return;
            dragging = false; Capture = false;
            if (moved) { ClampPosition(); SaveSettings(); }
            else { brain.Pat(clock.ElapsedMilliseconds); SyncKeyboard(); RenderFrame(false); Schedule(); }
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);
            if (dragging && !Capture) { dragging = false; ClampPosition(); SaveSettings(); }
        }
        internal void ShowPanel(Point point)
        {
            if (panel != null && !panel.IsDisposed) { panel.Activate(); return; }
            panel = new CompanionPanel(this, settings, brain, ReviewMode) { Location = new Point(point.X - 300, point.Y - 440) };
            panel.KeepOnScreen(); panel.Show();
            Native.SetForegroundWindow(panel.Handle); panel.Activate();
        }
        internal void SetCharacter(Character character)
        { settings.Character = character; brain.Wake(clock.ElapsedMilliseconds); Changed(); }
        internal void SetKeyboard(bool value)
        { settings.Keyboard = value; brain.KeyboardFeedback = value; Changed(); }
        internal void SetQuiet(bool value)
        { settings.Quiet = value; brain.Quiet = value; brain.Wake(clock.ElapsedMilliseconds); Changed(); }
        internal void SetAutoSleep(bool value)
        { settings.AutoSleep = value; brain.AutoSleep = value; brain.Step(clock.ElapsedMilliseconds); Changed(); }
        internal void SetAlwaysOnTop(bool value)
        { settings.TopMost = value; TopMost = value; SaveSettings(); }
        internal void SetClickThrough(bool value)
        {
            settings.ClickThrough = value;
            int style = Native.GetWindowLong(Handle, -20);
            Native.SetWindowLong(Handle, -20, value ? style | 0x20 : style & ~0x20); SaveSettings();
        }
        internal void ToggleSleep()
        {
            if (brain.Pose == Pose.Sleep) brain.Wake(clock.ElapsedMilliseconds);
            else brain.Sleep(clock.ElapsedMilliseconds);
            Changed();
        }
        internal void ChangeScale(float scale)
        {
            int bottom = Bottom, center = Left + Width / 2;
            settings.Scale = scale;
            Size = new Size((int)Math.Ceiling(PetRenderer.Width * scale), (int)Math.Ceiling(PetRenderer.Height * scale));
            Location = new Point(center - Width / 2, bottom - Height);
            ClampPosition(); Changed();
        }
        internal void StartFocus(int minutes)
        {
            focus.Start(minutes, clock.ElapsedMilliseconds);
            brain.Focusing = true; brain.Wake(clock.ElapsedMilliseconds); Changed();
        }
        internal void StopFocus()
        {
            focus.Stop(); brain.Focusing = false; brain.Wake(clock.ElapsedMilliseconds); Changed();
        }
        private void Changed()
        { brain.ClearHands(); SyncKeyboard(); SaveSettings(); RenderFrame(true); Schedule(); }
        private void SyncKeyboard()
        {
            bool enabled = !hidden && settings.Keyboard && !settings.Quiet && !brain.Focusing && !brain.ManualSleep;
            if (!hidden && settings.AutoSleep && !brain.ManualSleep && !brain.Focusing) enabled = true;
            if (enabled == keyboardRegistered) return;
            if (Native.RegisterKeyboard(Handle, enabled)) keyboardRegistered = enabled;
            else if (enabled) tray.Text = "实验室同桌 · 键盘跟随不可用，右键仍可操作";
        }
        private void OnScheduled(object sender, EventArgs e)
        {
            scheduler.Stop(); scheduledAt = long.MaxValue;
            if (focus.Advance(clock.ElapsedMilliseconds))
            { brain.Focusing = false; brain.Wake(clock.ElapsedMilliseconds); SyncKeyboard(); }
            brain.Step(clock.ElapsedMilliseconds); RenderFrame(false); Schedule();
        }
        private string TimerText()
        {
            if (focus.Completed) return "完成";
            if (!focus.Active) return null;
            int seconds = focus.RemainingSeconds(clock.ElapsedMilliseconds);
            return string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);
        }
        private void Schedule()
        {
            if (hidden && !focus.Active) { scheduler.Stop(); scheduledAt = long.MaxValue; return; }
            long now = clock.ElapsedMilliseconds;
            int delay = brain.NextDelay(now);
            if (focus.Active) delay = Math.Min(delay, hidden ? 60000 : 1000);
            if (scheduler.Enabled && scheduledAt <= now + delay) return;
            scheduler.Stop(); scheduledAt = now + delay;
            scheduler.Interval = Math.Max(15, delay); scheduler.Start();
        }
        private void RenderFrame(bool force)
        {
            if (hidden || !IsHandleCreated || disposed) return;
            long now = clock.ElapsedMilliseconds;
            string timer = TimerText(); int phase = brain.Phase(now);
            HandSample left = brain.LeftHand(now), right = brain.RightHand(now);
            string key = settings.Character + ":" + brain.Pose + ":" + phase + ":" + settings.Scale + ":" + timer + ":" + (int)(left.Press * 1000) + ":" + (int)(right.Press * 1000) + ":" + (int)(left.Impact * 1000) + ":" + (int)(right.Impact * 1000);
            if (!force && key == frameKey) return;
            if (surface == null || surface.Width != Width || surface.Height != Height)
            { if (surface != null) surface.Dispose(); surface = new Native.LayeredSurface(Width, Height); }
            PetRenderer.DrawTo(surface.Graphics, settings.Character, brain.Pose, phase, settings.Scale, timer, left, right);
            surface.Present(Handle, Left, Top); frameKey = key; RenderCount++;
        }
        internal void HidePet()
        { hidden = true; brain.Hidden = true; Hide(); SyncKeyboard(); Schedule(); }
        internal void ShowPet()
        {
            hidden = false; brain.Hidden = false; ClampPosition(); Show();
            brain.Wake(clock.ElapsedMilliseconds); SyncKeyboard(); RenderFrame(true); Schedule();
        }
        internal void ResetPosition()
        {
            var area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 36, area.Bottom - Height - 10);
            ShowPet(); SaveSettings();
        }
        private void ClampPosition()
        {
            var area = Screen.FromRectangle(Bounds).WorkingArea;
            Left = Math.Max(area.Left, Math.Min(area.Right - Width, Left));
            Top = Math.Max(area.Top, Math.Min(area.Bottom - Height, Top));
        }
        private void OnDisplayChanged(object sender, EventArgs e)
        { if (!disposed && IsHandleCreated) BeginInvoke((Action)delegate { if (surface != null) { surface.Dispose(); surface = null; } ClampPosition(); RenderFrame(true); }); }
        private void OnSessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        { if (!disposed && IsHandleCreated) BeginInvoke((Action)delegate { brain.ClearHands(); RenderFrame(true); Schedule(); }); }
        private void SaveSettings()
        { if (persistSettings) { settings.Left = Left; settings.Top = Top; settings.Save(); } }
        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true; SaveSettings();
                Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged;
                Microsoft.Win32.SystemEvents.SessionSwitch -= OnSessionSwitch;
                if (panel != null) panel.Dispose();
                scheduler.Stop(); scheduler.Dispose();
                if (keyboardRegistered) Native.RegisterKeyboard(Handle, false);
                tray.Visible = false; tray.Dispose(); petIcon.Dispose();
                if (surface != null) { surface.Dispose(); surface = null; }
            }
            base.Dispose(disposing);
        }
    }
}
