namespace LabPals
{
    // Durations in monotonic milliseconds; shared by scheduling and rendering.
    internal static class AnimationTiming
    {
        // One shared presentation budget for input bursts and interpolated motion.
        internal const int FrameMs = 33;
        internal const int HandReleaseMs = 120;
        internal const int StrikeMs = 55;
        internal const int BlinkMs = 150;
        internal const int IdleBlinkMinMs = 5500, IdleBlinkMaxMs = 9500;
        internal const int PoseFrameMs = 30;
        internal const int PatMs = 900, PatInMs = 120, PatHoldUntilMs = 570;
        internal const int PatPhases = PatMs / PoseFrameMs;
        internal const int SleepMs = 360, SleepPhases = SleepMs / PoseFrameMs;
        internal const int AutoSleepMs = 180000;
    }
}
