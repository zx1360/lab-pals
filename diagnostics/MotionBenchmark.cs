using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace LabPals
{
    internal static class MotionBenchmark
    {
        internal static void Run(string report)
        { Run(report, false); }
        internal static void RunBurst(string report)
        { Run(report, true); }
        private static void Run(string report, bool burst)
        {
            using(var process=Process.GetCurrentProcess())
            using(var window=new PetWindow(new PetSettings { AutoSleep=false,Scale=0.75f },false))
            using(var input=new Timer())
            {
                var clock=new Stopwatch(); int events=0,renderStart=0; double submitMs=0; TimeSpan cpuStart=TimeSpan.Zero;
                input.Interval=burst ? 15 : 40;
                window.Shown+=delegate { renderStart=window.RenderCount; cpuStart=process.TotalProcessorTime; clock.Start(); input.Start(); };
                input.Tick+=delegate
                {
                    if(clock.ElapsedMilliseconds>=10000)
                    {
                        input.Stop(); clock.Stop(); process.Refresh();
                        double cpuMs=(process.TotalProcessorTime-cpuStart).TotalMilliseconds;
                        int presentations=window.RenderCount-renderStart;
                        if(presentations>clock.ElapsedMilliseconds/AnimationTiming.FrameMs+2)
                            throw new InvalidOperationException("Input bypassed the shared presentation budget.");
                        string[] lines={
                            burst ? "Synthetic burst input in the real native window (32 transitions per tick)." : "Synthetic typing in the real native window; includes its animation scheduler and presentation.",
                            "Does not measure hardware-to-display latency.",
                            "PASS shared presentation budget",
                            "duration_seconds="+clock.Elapsed.TotalSeconds.ToString("0.000",CultureInfo.InvariantCulture),
                            "accepted_input_transitions="+events,
                            "native_presentations="+(window.RenderCount-renderStart),
                            "presentations_per_second="+((window.RenderCount-renderStart)/clock.Elapsed.TotalSeconds).ToString("0.0",CultureInfo.InvariantCulture),
                            "average_input_handler_ms="+(submitMs/Math.Max(1,events)).ToString("0.000",CultureInfo.InvariantCulture),
                            "cpu_percent_one_core="+(cpuMs/clock.Elapsed.TotalMilliseconds*100).ToString("0.000",CultureInfo.InvariantCulture),
                            "cpu_percent_all_logical_processors="+(cpuMs/clock.Elapsed.TotalMilliseconds*100/Environment.ProcessorCount).ToString("0.000",CultureInfo.InvariantCulture),
                            "working_set_mb="+(process.WorkingSet64/1048576.0).ToString("0.0",CultureInfo.InvariantCulture)
                        };
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))); File.WriteAllLines(report,lines);
                        window.Close(); return;
                    }
                    for(int i=0;i<(burst ? 32 : 1);i++)
                    {
                        int side=(events/2)%2; bool down=events%2==0;
                        var stopwatch=Stopwatch.StartNew();
                        if(!window.ProcessStroke(side==0 ? 30 : 36,side==0 ? 1 : 2,down)) throw new InvalidOperationException("Synthetic input was ignored");
                        stopwatch.Stop(); submitMs+=stopwatch.Elapsed.TotalMilliseconds; events++;
                    }
                };
                Application.Run(window);
            }
        }
    }
}
