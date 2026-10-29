using System;

namespace LabPals
{
    // Uses the same monotonic clock as animations, so wall-clock corrections cannot shorten a session.
    internal sealed class FocusSession
    {
        private long deadline;
        internal bool Active { get; private set; }
        internal bool Completed { get; private set; }
        internal void Start(int minutes, long now)
        {
            if (minutes <= 0 || minutes > 240) throw new ArgumentOutOfRangeException("minutes");
            deadline = checked(now + minutes * 60000L);
            Active = true; Completed = false;
        }
        internal void Stop() { Active = false; Completed = false; }
        internal bool Advance(long now)
        {
            if (!Active || now < deadline) return false;
            Active = false; Completed = true; return true;
        }
        internal int RemainingSeconds(long now)
        { return Active ? (int)Math.Max(0, (deadline - now + 999) / 1000) : 0; }
    }
}
