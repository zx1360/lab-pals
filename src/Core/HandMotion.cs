using System;

namespace LabPals
{
    internal sealed class HandMotion
    {
        private bool held;
        private long changed = -1000;
        public HandSample Sample(long now)
        {
            float age = Math.Max(0, now - changed);
            if (held) return new HandSample { Press = 1, Impact = Math.Max(0, 1 - age / AnimationTiming.StrikeMs) };
            float t = Math.Min(1, age / (float)AnimationTiming.HandReleaseMs);
            // Immediate contact, then an eased lift with a very small overshoot.
            return new HandSample { Press = (1 - t) * (1 - t) * (1 - t) - (float)Math.Sin(t * Math.PI) * 0.09f };
        }
        public bool Animating(long now) { return now - changed < (held ? AnimationTiming.StrikeMs : AnimationTiming.HandReleaseMs); }
        public void Set(bool down, long now) { if (held == down) return; held = down; changed = now; }
        public void Strike(long now) { held = true; changed = now; }
        public void Reset() { held = false; changed = -1000; }
    }
}
