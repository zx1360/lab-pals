using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace LabPals
{
    internal static class SelfTest
    {
        private static readonly List<string> checks = new List<string>();
        private static void Assert(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); checks.Add("PASS " + message); }

        internal static void Run(string reportPath)
        {
            checks.Clear();
            CheckSettingsAndFocus();
            var brain = new PetBrain(0);
            Assert(brain.Pose == Pose.Idle, "Starts idle");
            Assert(brain.NextDelay(0) >= 5500, "Idle scheduler sleeps at least 5.5 seconds");
            long blink = brain.NextBlink;
            brain.Step(blink); Assert(brain.Pose == Pose.Blink, "Scheduled blink");
            brain.Step(blink + 151); Assert(brain.Pose == Pose.Idle, "Blink settles");
            Assert(brain.KeyTransition(30,1,true,10000), "Keydown responds synchronously");
            Assert(brain.LeftHand(10000).Press==1, "Immediate full contact on the first press");
            Assert(brain.KeyTransition(36,2,true,10010), "Opposite hand responds 10 ms later without a throttle");
            Assert(brain.LeftHand(10010).Press==1 && brain.RightHand(10010).Press==1, "Both hands can be pressed independently");
            Assert(!brain.KeyTransition(30,1,true,10020), "OS auto-repeat does not fabricate extra presses");
            brain.KeyTransition(31,1,true,10025);
            Assert(brain.LeftHand(10025).Impact==1, "A different key on the same hand retriggers the contact impulse");
            brain.KeyTransition(30,1,false,10030);
            Assert(brain.LeftHand(10080).Press==1, "Releasing one of two left-hand keys keeps that hand down");
            brain.KeyTransition(31,1,false,10090); brain.KeyTransition(36,2,false,10095);
            Assert(brain.LeftHand(10090).Press==1, "Release begins continuously from the contact pose");
            Assert(brain.LeftHand(10120).Press>0 && brain.LeftHand(10120).Press<1, "Release uses interpolated motion rather than swapping two stills");
            brain.Step(10216); Assert(brain.Pose==Pose.Idle && Math.Abs(brain.LeftHand(10216).Press)<0.001f, "Both hands settle after the 120 ms return");
            Assert(brain.NextDelay(10216)>=5500, "Animation frame timer stops once both hands settle");
            Assert(Native.KeyboardHand(30,false)==1 && Native.KeyboardHand(36,false)==2 && Native.KeyboardHand(57,false)==3, "Physical left/right rows and both-hand space mapping");
            Assert(Native.KeyboardHand(42,false)==1 && Native.KeyboardHand(54,false)==2 && Native.KeyboardHand(29,true)==2, "Left/right modifiers use physical scan positions");
            brain.Pat(11000); Assert(brain.Pose == Pose.Pat, "Pat reacts");
            brain.Step(11901); Assert(brain.Pose == Pose.Idle, "Pat settles after its 900 ms expression response");
            brain.Quiet = true;
            Assert(!brain.KeyTransition(30,1,true,12000) && brain.Pose == Pose.Idle, "Quiet mode suppresses typing");
            brain.Quiet = false; brain.KeyboardFeedback = false;
            Assert(!brain.KeyTransition(30,1,true,12500) && brain.Pose == Pose.Idle, "Keyboard toggle suppresses animation even with auto-sleep enabled");
            brain.KeyboardFeedback = true;
            brain.Sleep(13000); brain.KeyTransition(30,1,true,14000);
            Assert(brain.Pose == Pose.Sleep && brain.ManualSleep, "Manual sleep ignores keyboard");
            brain.Pat(15000); Assert(brain.Pose == Pose.Idle && !brain.ManualSleep, "Click wakes manual sleep");
            brain.Step(195000); Assert(brain.Pose == Pose.Sleep && !brain.ManualSleep, "Automatic sleep after 3 minutes");
            Assert(brain.KeyTransition(30,1,true,196000) && brain.Pose != Pose.Sleep, "Keyboard wakes automatic sleep");
            brain.Quiet = true; brain.Step(376000); brain.KeyTransition(30,1,true,377000);
            Assert(brain.Pose == Pose.Idle, "Quiet mode wakes without typing animation");
            brain.Focusing = true; brain.Step(577000);
            Assert(brain.Pose != Pose.Sleep, "Focus session prevents automatic sleep");
            brain.Hidden = true; Assert(brain.NextDelay(578000) == 60000, "Hidden pet has no animation work");

            var heldBrain = new PetBrain(0);
            heldBrain.KeyTransition(30,1,true,1); heldBrain.Step(10000); heldBrain.Step(10151);
            Assert(heldBrain.NextDelay(10151)>1000, "A held key does not leave an expired blink deadline running at 60 fps");
            var fastBrain = new PetBrain(0); int accepted=0;
            for(int i=0;i<100;i++) { if(fastBrain.KeyTransition(30,1,true,i*8)) accepted++; if(fastBrain.KeyTransition(30,1,false,i*8+4)) accepted++; }
            Assert(accepted==200, "All 200 distinct rapid down/up events are accepted without dropping a stroke");
            var affectionBrain=new PetBrain(0);affectionBrain.Pat(100);
            affectionBrain.KeyTransition(30,1,true,150);
            Assert(affectionBrain.Pose==Pose.Pat && affectionBrain.LeftHand(150).Press==1, "Typing responds immediately while the click expression continues");
            long started=affectionBrain.PatStarted;affectionBrain.Pat(200);
            Assert(affectionBrain.PatStarted==started, "Repeated clicks do not restart and flicker the face");
            affectionBrain.KeyTransition(30,1,false,220);affectionBrain.Step(1001);
            Assert(affectionBrain.Pose==Pose.Idle, "Expression and hand return to rest independently");
            affectionBrain.Sleep(2000);
            Assert(affectionBrain.NextDelay(2000)<=33 && affectionBrain.Phase(2361)==12 && affectionBrain.NextDelay(2361)==60000, "Sleep settles briefly then stops high-frequency scheduling");

            int frames = 0;
            foreach (Character c in Enum.GetValues(typeof(Character)))
                foreach (Pose pose in Enum.GetValues(typeof(Pose)))
                    foreach (float scale in new[] { 0.55f, 0.75f, 1f, 1.25f })
                        using (var image = PetRenderer.Render(c, pose, 1, scale, "25:00"))
                        {
                            if (image.GetPixel(0, 0).A != 0) throw new Exception("Transparent edge failed");
                            if (image.Width != (int)Math.Ceiling(320 * scale)) throw new Exception("Scale failed");
                            frames++;
                        }
            Assert(frames == CharacterCatalog.All.Count * 6 * 4, "Every registered character x 6 poses x 4 sizes renders with transparent edges");
            using (var neutral = PetRenderer.Render(Character.Luka, Pose.Idle, 0, 1, null))
            using (var pressed = PetRenderer.Render(Character.Luka, Pose.TapLeft, 0, 1, null))
            using (var resting = PetRenderer.Render(Character.Luka, Pose.Blink, 0, 1, null))
            {
                int moving = 0, stationaryChanges = 0, bodyChanges = 0;
                for (int y = 0; y < 280; y++) for (int x = 0; x < 320; x++)
                {
                    if (neutral.GetPixel(x,y).ToArgb() != pressed.GetPixel(x,y).ToArgb())
                    { moving++; if (x >= 160 || y < 176) stationaryChanges++; }
                    if (y > 170 && neutral.GetPixel(x,y).ToArgb() != resting.GetPixel(x,y).ToArgb()) bodyChanges++;
                }
                Assert(moving > 150 && stationaryChanges == 0, "Left wrist animation leaves the head and opposite hand pixel-identical");
                Assert(bodyChanges == 0, "Blink changes only the face; clothing and hand pixels remain identical");
            }
            foreach(Character character in Enum.GetValues(typeof(Character)))
            using(var neutral=PetRenderer.Render(character,Pose.Idle,0,1,null))
            using(var contact=PetRenderer.Render(character,Pose.TapLeft,0,1,null,new HandSample { Press=1 },new HandSample { Press=1 }))
            using(var sleeping=PetRenderer.Render(character,Pose.Sleep,12,1,null))
            using(var sleepStart=PetRenderer.Render(character,Pose.Sleep,0,1,null))
            using(var sleepMiddle=PetRenderer.Render(character,Pose.Sleep,6,1,null))
            using(var closedEyes=PetRenderer.Render(character,Pose.Blink,0,1,null))
            using(var reaction=PetRenderer.Render(character,Pose.Pat,10,1,null))
            {
                int clothingChanges=0,sleepChanges=0,faceChanges=0,bodyChanges=0,sleepStartChanges=0,mixErrors=0;
                for(int y=0;y<280;y++)for(int x=0;x<320;x++)
                {
                    if(y<223 && neutral.GetPixel(x,y).ToArgb()!=contact.GetPixel(x,y).ToArgb())clothingChanges++;
                    if(neutral.GetPixel(x,y).ToArgb()!=sleeping.GetPixel(x,y).ToArgb())sleepChanges++;
                    if(sleepStart.GetPixel(x,y).ToArgb()!=closedEyes.GetPixel(x,y).ToArgb())sleepStartChanges++;
                    int expectedAlpha=(closedEyes.GetPixel(x,y).A+sleeping.GetPixel(x,y).A+1)/2;
                    if(Math.Abs(sleepMiddle.GetPixel(x,y).A-expectedAlpha)>1)mixErrors++;
                    if(neutral.GetPixel(x,y).ToArgb()!=reaction.GetPixel(x,y).ToArgb()){faceChanges++;if(y>170)bodyChanges++;}
                }
                Assert(clothingChanges==0, character+" typing keeps shoulders, chest, tie and sleeve folds above the wrists unchanged");
                Assert(sleepChanges>10000,character+" has a dedicated sleeping illustration rather than a closed-eye duplicate");
                Assert(sleepStartChanges==0 && mixErrors==0,character+" sleep transition starts with closed eyes and blends without transparent-edge dimming");
                Assert(faceChanges>800 && bodyChanges==0,character+" has a distinct click expression with stationary clothing and hands");
            }

            using(var surface=new Native.LayeredSurface(240,210))
            {
                var left=new HandSample { Press=1,Impact=0.5f }; var right=new HandSample { Press=0.3f };
                PetRenderer.DrawTo(surface.Graphics,Character.Mayuri,Pose.TapLeft,0,0.75f,null,left,right);
                using(var actual=surface.Snapshot()) using(var expected=PetRenderer.Render(Character.Mayuri,Pose.TapLeft,0,0.75f,null,left,right))
                {
                    int mismatches=0;
                    for(int y=0;y<210;y+=3) for(int x=0;x<240;x+=3) if(actual.GetPixel(x,y).ToArgb()!=expected.GetPixel(x,y).ToArgb()) mismatches++;
                    Assert(mismatches==0, "Persistent native DIB pixels match the exported animation renderer including alpha");
                }
                PetRenderer.DrawTo(surface.Graphics,Character.Mayuri,Pose.Idle,0,0.75f,null,new HandSample(),new HandSample());
                using(var actual=surface.Snapshot()) using(var expected=PetRenderer.Render(Character.Mayuri,Pose.Idle,0,0.75f,null))
                {
                    int mismatches=0;
                    for(int y=0;y<210;y+=3) for(int x=0;x<240;x+=3) if(actual.GetPixel(x,y).ToArgb()!=expected.GetPixel(x,y).ToArgb()) mismatches++;
                    Assert(mismatches==0, "Reusing the native DIB leaves no stale hand pixels after settling");
                }
            }

            using (var window = new PetWindow(new PetSettings { Character = Character.Mayuri, AutoSleep = true }, false))
            {
                window.Show(); Application.DoEvents();
                Assert((Native.GetWindowLong(window.Handle, -20) & 0x80000) != 0, "Real native window uses per-pixel transparency");
                Assert((Native.GetWindowLong(window.Handle, -20) & 0x8000000) != 0, "Pet window cannot steal typing focus");
                Assert(!window.ShowInTaskbar && window.TopMost, "No taskbar entry; topmost enabled");
                Assert((Native.GetWindowLong(window.Handle, -16) & 0x30000) == 0, "Desktop companion has neither minimize nor maximize capability");
                Native.SendMessage(window.Handle, 0x112, new IntPtr(0xF020), IntPtr.Zero); Application.DoEvents();
                Assert(!Native.IsIconic(window.Handle) && window.Visible, "Shell minimize command leaves the companion visible");
                window.ShowPanel(new Point(window.Right, window.Bottom)); Application.DoEvents();
                var popup = window.SettingsPanel;
                Assert(popup.Visible && (Native.GetWindowLong(popup.Handle,-20) & 0x8000000) == 0, "Settings popup is focusable and separate from the no-activate companion");
                using (var outside = new Form { Text = "Lab Pals dismissal test", ShowInTaskbar = false })
                {
                    outside.Show(); Native.SetForegroundWindow(outside.Handle); outside.Activate(); Application.DoEvents();
                    Assert(popup.IsDisposed, "Settings popup disposes on activation of a different window"); outside.Close();
                }
                window.HidePet(); Assert(!window.Visible, "Explicit hide still works");
                Native.SendMessage(window.Handle, Native.RestoreMessage, IntPtr.Zero, IntPtr.Zero); Application.DoEvents();
                Assert(window.Visible, "Single-instance restore message finds a hidden companion without a dialog");
                window.HidePet();
                window.ShowPet(); Assert(window.Visible, "Tray restore returns an explicitly hidden companion");
                window.SetCharacter(Character.Kurisu);
                window.ChangeScale(0.55f); Assert(window.Width == 176 && window.Height == 154, "Live scale updates native window");
                window.ToggleSleep(); Assert(window.Brain.Pose == Pose.Sleep, "Sleep menu action");
                window.ToggleSleep(); Assert(window.Brain.Pose == Pose.Idle, "Wake menu action");
                window.StartFocus(25); Assert(window.Brain.Focusing, "Focus menu starts quiet session");
                window.StopFocus(); Assert(!window.Brain.Focusing, "Focus menu restores normal behavior");

                int beforePress=window.RenderCount;
                Assert(window.ProcessStroke(30,1,true) && window.RenderCount==beforePress+1, "Real key handler presents the contact frame before returning");
                window.ProcessStroke(30,1,false);

                using (var process = Process.GetCurrentProcess())
                {
                    for (int i = 0; i < 30; i++) window.SetCharacter(CharacterCatalog.All[i % CharacterCatalog.All.Count].Id);
                    Application.DoEvents();
                    uint gdiBefore = Native.GetGuiResources(process.Handle, 0);
                    var renderWatch = Stopwatch.StartNew();
                    for (int i = 0; i < 600; i++) window.SetCharacter(CharacterCatalog.All[i % CharacterCatalog.All.Count].Id);
                    renderWatch.Stop(); Application.DoEvents();
                    uint gdiAfter = Native.GetGuiResources(process.Handle, 0);
                    Assert(gdiAfter <= gdiBefore + 3, "600 real layered-window renders do not leak GDI handles");
                    window.SetCharacter(Character.Mayuri);
                    window.SetKeyboard(false); window.SetAutoSleep(false);
                    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                    int count = window.RenderCount;
                    TimeSpan cpuStart = process.TotalProcessorTime;
                    var idleWatch = Stopwatch.StartNew();
                    while (idleWatch.ElapsedMilliseconds < 15000) { Application.DoEvents(); Thread.Sleep(20); }
                    idleWatch.Stop(); process.Refresh();
                    double cpuMs = (process.TotalProcessorTime - cpuStart).TotalMilliseconds;
                    double oneCorePercent = cpuMs / idleWatch.Elapsed.TotalMilliseconds * 100;
                    Assert(window.RenderCount - count <= 6, "15 seconds idle uses at most six redraws");
                    checks.Add("METRIC native_render_average_ms=" + (renderWatch.Elapsed.TotalMilliseconds / 600).ToString("0.00", CultureInfo.InvariantCulture));
                    checks.Add("METRIC idle_duration_seconds=" + idleWatch.Elapsed.TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture));
                    checks.Add("METRIC idle_cpu_ms=" + cpuMs.ToString("0.00", CultureInfo.InvariantCulture));
                    checks.Add("METRIC idle_cpu_percent_of_one_core=" + oneCorePercent.ToString("0.000", CultureInfo.InvariantCulture));
                    checks.Add("METRIC working_set_mb_after_render_stress=" + (process.WorkingSet64 / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture));
                    checks.Add("METRIC private_bytes_mb_after_render_stress=" + (process.PrivateMemorySize64 / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture));
                    checks.Add("METRIC idle_redraws=" + (window.RenderCount - count));
                    checks.Add("METRIC gdi_handles_before=" + gdiBefore + " after=" + gdiAfter);
                }
                window.Close();
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
            File.WriteAllLines(reportPath, checks);
        }

        private static void CheckSettingsAndFocus()
        {
            var focus = new FocusSession();
            focus.Start(25, 1000);
            Assert(focus.Active && focus.RemainingSeconds(1001) == 1500, "Focus countdown rounds up using elapsed time");
            Assert(!focus.Advance(1500999) && focus.Advance(1501000), "Focus completes at the monotonic deadline");
            Assert(focus.Completed && !focus.Advance(1501001), "Focus completion fires only once");
            focus.Start(45, 2000000); focus.Stop();
            Assert(!focus.Active && !focus.Completed, "Cancelling focus clears its completion state");
            bool rejected = false;
            try { focus.Start(0, 0); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Assert(rejected, "Invalid focus duration is rejected before changing state");

            var invalid = new PetSettings { Character = (Character)999, Scale = float.PositiveInfinity, Left = 42 };
            invalid.Normalize();
            Assert(invalid.Character == Character.Luka && invalid.Scale == .75f, "Invalid character and non-finite size recover to the documented defaults");
            Assert(invalid.Left == int.MinValue && invalid.Top == int.MinValue, "A partially missing position resets both coordinates");
            string folder = Path.Combine(Path.GetTempPath(), "LabPals-" + Guid.NewGuid().ToString("N"));
            string path = Path.Combine(folder, "settings.xml"), broken = Path.Combine(folder, "broken.xml");
            string logFolder = Path.Combine(folder, "nested"), logPath = Path.Combine(logFolder, "error.log");
            Directory.CreateDirectory(folder);
            try
            {
                var settings = new PetSettings { Character = Character.Kurisu, Scale = 1.25f, Quiet = true, Left = -120, Top = 80 };
                Assert(settings.SaveTo(path), "Settings can be saved without accessing the user's settings file");
                var loaded = PetSettings.LoadFrom(path);
                Assert(loaded.Character == Character.Kurisu && loaded.Scale == 1.25f && loaded.Quiet && loaded.Left == -120, "Settings round-trip preserves preferences and negative monitor coordinates");
                settings.Character = Character.Mayuri;
                Assert(settings.SaveTo(path) && PetSettings.LoadFrom(path).Character == Character.Mayuri, "Atomic replacement updates an existing settings file");
                File.WriteAllText(broken, "invalid xml");
                Assert(PetSettings.LoadFrom(broken).Character == Character.Luka && File.ReadAllText(broken) == "invalid xml", "Corrupt settings recover without overwriting the source file");
                Directory.CreateDirectory(path + ".tmp");
                Assert(!settings.SaveTo(path) && PetSettings.LoadFrom(path).Character == Character.Mayuri, "Failed settings writes preserve the last valid file");
                Assert(ErrorLog.TryWrite(logPath, new InvalidOperationException("diagnostic sample")) && File.ReadAllText(logPath).Contains("diagnostic sample"), "Error reports create missing destination directories");
                Assert(!ErrorLog.TryWrite(null, new Exception("sample")), "An invalid log path cannot throw a second exception");
            }
            finally
            {
                // Only remove these explicitly created test files and their empty parent directories.
                foreach (string file in new[] { path, broken, logPath }) if (File.Exists(file)) File.Delete(file);
                foreach (string directory in new[] { path + ".tmp", logFolder, folder })
                    if (Directory.Exists(directory)) Directory.Delete(directory);
            }
        }
    }
}
