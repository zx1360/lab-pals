using System;

namespace LabPals
{
    // An event-driven state machine: no frame loop when the character is still.
    public sealed class PetBrain
    {
        public const long SleepDelay = AnimationTiming.AutoSleepMs;
        public Pose Pose { get; private set; }
        public bool ManualSleep { get; private set; }
        public bool Quiet;
        public bool KeyboardFeedback = true;
        public bool AutoSleep = true;
        public bool Focusing;
        public bool Hidden;
        public long LastActivity { get; private set; }
        public long NextBlink { get; private set; }
        public long ReactionUntil { get; private set; }
        public long PatStarted { get; private set; }
        public long SleepStarted { get; private set; }
        private readonly bool[] heldKeys = new bool[512];
        private readonly byte[] heldMasks = new byte[512];
        private int leftHeld, rightHeld;
        private readonly HandMotion left = new HandMotion(), right = new HandMotion();
        private readonly Random random = new Random(1048);

        public PetBrain(long now) { Wake(now); }
        public void Wake(long now)
        {
            ClearHands();
            ManualSleep = false;
            Pose = Pose.Idle;
            LastActivity = now;
            ReactionUntil = 0;
            NextBlink = now + random.Next(AnimationTiming.IdleBlinkMinMs, AnimationTiming.IdleBlinkMaxMs);
        }
        public void Sleep(long now)
        {
            ClearHands();
            ManualSleep = true;
            Pose = Pose.Sleep;
            SleepStarted = now;
        }
        public void ClearHands()
        {
            Array.Clear(heldKeys, 0, heldKeys.Length);
            Array.Clear(heldMasks, 0, heldMasks.Length);
            leftHeld = rightHeld = 0; left.Reset(); right.Reset();
        }
        public HandSample LeftHand(long now) { return left.Sample(now); }
        public HandSample RightHand(long now) { return right.Sample(now); }
        public bool HandsAnimating(long now) { return left.Animating(now) || right.Animating(now); }
        public bool KeyTransition(int identity, int handMask, bool down, long now)
        {
            if (down) LastActivity = now;
            if (ManualSleep || Hidden) return false;
            bool woke = down && Pose == Pose.Sleep;
            if (woke) Wake(now);
            if (Quiet || Focusing || !KeyboardFeedback) return woke;
            if (identity < 0 || identity >= heldKeys.Length) return woke;
            // Suppress OS auto-repeat, never a distinct physical down/up transition.
            if (heldKeys[identity] == down) return woke;
            if (down)
            {
                heldKeys[identity] = true; heldMasks[identity] = (byte)handMask;
                if ((handMask & 1) != 0) { leftHeld++; left.Strike(now); }
                if ((handMask & 2) != 0) { rightHeld++; right.Strike(now); }
                if (Pose != Pose.Pat || now >= ReactionUntil) Pose = handMask == 1 ? Pose.TapLeft : Pose.TapRight;
            }
            else
            {
                handMask = heldMasks[identity]; heldKeys[identity] = false; heldMasks[identity] = 0;
                if ((handMask & 1) != 0 && --leftHeld == 0) left.Set(false, now);
                if ((handMask & 2) != 0 && --rightHeld == 0) right.Set(false, now);
            }
            if (Pose != Pose.Pat) ReactionUntil = 0;
            return true;
        }
        public void Pat(long now)
        {
            if (Pose == Pose.Sleep) { Wake(now); return; }
            if (Pose == Pose.Pat && now < ReactionUntil) return;
            LastActivity = now;
            PatStarted = now;
            ReactionUntil = now + AnimationTiming.PatMs;
            Pose = Pose.Pat;
        }
        public void Step(long now)
        {
            if (Hidden) return;
            if (ManualSleep) { Pose = Pose.Sleep; return; }
            if (AutoSleep && !Focusing && now - LastActivity >= SleepDelay)
            {
                if (Pose != Pose.Sleep) SleepStarted = now;
                Pose = Pose.Sleep;
                return;
            }
            if (Pose == Pose.Sleep) Wake(now);
            if (Pose == Pose.TapLeft || Pose == Pose.TapRight)
            {
                if (leftHeld + rightHeld > 0 || HandsAnimating(now))
                {
                    if (now < NextBlink) return;
                    Pose = Pose.Blink; ReactionUntil = now + AnimationTiming.BlinkMs;
                }
                else { Pose = Pose.Idle; NextBlink = now + random.Next(AnimationTiming.IdleBlinkMinMs, AnimationTiming.IdleBlinkMaxMs); }
            }
            if (Pose == Pose.Pat || Pose == Pose.Blink)
            {
                if (now < ReactionUntil) return;
                Pose = Pose.Idle;
                NextBlink = now + random.Next(AnimationTiming.IdleBlinkMinMs, AnimationTiming.IdleBlinkMaxMs);
            }
            if (now >= NextBlink)
            {
                Pose = Pose.Blink;
                ReactionUntil = now + AnimationTiming.BlinkMs;
            }
        }
        public int NextDelay(long now)
        {
            if (Hidden) return 60000;
            if (Pose == Pose.Sleep) return now - SleepStarted < AnimationTiming.SleepMs ? AnimationTiming.PoseFrameMs : int.MaxValue;
            if (HandsAnimating(now)) return AnimationTiming.FrameMs;
            long due = NextBlink;
            if (Pose == Pose.Pat)
            {
                long age = now - PatStarted;
                due = age >= AnimationTiming.PatInMs && age < AnimationTiming.PatHoldUntilMs
                    ? PatStarted + AnimationTiming.PatHoldUntilMs
                    : Math.Min(ReactionUntil, now + AnimationTiming.PoseFrameMs);
            }
            else if (Pose == Pose.Blink) due = ReactionUntil;
            if (AutoSleep && !Focusing) due = Math.Min(due, LastActivity + SleepDelay);
            return (int)Math.Max(15, Math.Min(60000, due - now));
        }
        public int Phase(long now)
        {
            if (Pose == Pose.Sleep) return Math.Min(AnimationTiming.SleepPhases, (int)Math.Max(0, (now - SleepStarted) / AnimationTiming.PoseFrameMs));
            if (Pose == Pose.Pat) return Math.Min(AnimationTiming.PatPhases, (int)Math.Max(0, (now - PatStarted) / AnimationTiming.PoseFrameMs));
            return 0;
        }
    }
}
