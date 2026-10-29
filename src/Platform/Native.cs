using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LabPals
{
    internal static class Native
    {
        internal static readonly int RestoreMessage = RegisterWindowMessage("LabPals.Restore.DesktopPet.v1");
        [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; public Point(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] internal struct Size { public int X, Y; public Size(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] internal struct Blend { public byte Op, Flags, Alpha, Format; }
        [StructLayout(LayoutKind.Sequential)] internal struct RawDevice { public ushort Page, Usage; public uint Flags; public IntPtr Target; }
        [StructLayout(LayoutKind.Sequential)] internal struct RawHeader { public uint Type, Size; public IntPtr Device, WParam; }
        [StructLayout(LayoutKind.Sequential)] internal struct RawKeyboard { public ushort Scan, Flags, Reserved, VirtualKey; public uint Message, Extra; }
        [StructLayout(LayoutKind.Sequential)] internal struct RawInput { public RawHeader Header; public RawKeyboard Keyboard; }
        [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
        {
            public uint HeaderSize; public int Width,Height; public ushort Planes,BitCount;
            public uint Compression,ImageSize; public int XResolution,YResolution; public uint ColorsUsed,ColorsImportant;
        }
        internal struct KeyboardTransition { public int Identity, HandMask; public bool Down; }

        [DllImport("user32.dll", SetLastError = true)] internal static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screen, ref Point position, ref Size size, IntPtr source, ref Point origin, int key, ref Blend blend, int flags);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
        [DllImport("user32.dll", SetLastError = true)] internal static extern bool RegisterRawInputDevices(RawDevice[] devices, uint count, uint size);
        [DllImport("user32.dll", SetLastError = true)] internal static extern uint GetRawInputData(IntPtr input, uint command, out RawInput data, ref uint size, uint headerSize);
        [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr window, int index);
        [DllImport("user32.dll")] internal static extern int SetWindowLong(IntPtr window, int index, int value);
        [DllImport("user32.dll")] internal static extern uint GetGuiResources(IntPtr process, uint flags);
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] internal static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr window);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);
        [DllImport("dwmapi.dll")] internal static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int RegisterWindowMessage(string name);
        [DllImport("user32.dll")] internal static extern bool PostMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        internal static bool RegisterKeyboard(IntPtr window, bool enabled)
        {
            var device = new RawDevice { Page = 1, Usage = 6, Flags = enabled ? 0x100u : 1u, Target = enabled ? window : IntPtr.Zero };
            return RegisterRawInputDevices(new[] { device }, 1, (uint)Marshal.SizeOf(typeof(RawDevice)));
        }
        internal static bool ReadKeyboardTransition(IntPtr input, out KeyboardTransition transition)
        {
            transition = new KeyboardTransition();
            uint size = (uint)Marshal.SizeOf(typeof(RawInput)); RawInput data;
            uint result = GetRawInputData(input, 0x10000003, out data, ref size, (uint)Marshal.SizeOf(typeof(RawHeader)));
            if (result == uint.MaxValue || data.Header.Type != 1 || data.Keyboard.VirtualKey == 255 || data.Keyboard.Scan == 0) return false;
            bool extended = (data.Keyboard.Flags & 2) != 0;
            transition.Identity = (data.Keyboard.Scan & 255) | (extended ? 256 : 0);
            transition.HandMask = KeyboardHand(data.Keyboard.Scan, extended);
            transition.Down = (data.Keyboard.Flags & 1) == 0;
            return true;
        }
        internal static int KeyboardHand(int scan, bool extended)
        {
            // Physical keyboard position only. No text translation, history or output.
            if (scan == 57) return 3; // space: both hands
            if (extended) return 2;
            if (scan >= 2 && scan <= 13) return scan <= 6 ? 1 : 2;
            if (scan >= 16 && scan <= 27) return scan <= 20 ? 1 : 2;
            if (scan >= 30 && scan <= 40) return scan <= 34 ? 1 : 2;
            if (scan >= 44 && scan <= 53) return scan <= 48 ? 1 : 2;
            if (scan == 28 || scan == 14 || scan == 54 || scan == 55 || scan >= 64) return 2;
            return 1;
        }
        internal sealed class LayeredSurface : IDisposable
        {
            private IntPtr screen,memory,handle,previous;
            private Bitmap pixels;
            internal Graphics Graphics { get; private set; }
            internal int Width { get; private set; }
            internal int Height { get; private set; }
            internal LayeredSurface(int width,int height)
            {
                Width=width; Height=height;
                try
                {
                    screen=GetDC(IntPtr.Zero); memory=CreateCompatibleDC(screen);
                    var info=new BitmapInfo { HeaderSize=40,Width=width,Height=-height,Planes=1,BitCount=32 };
                    IntPtr bits; handle=CreateDIBSection(memory,ref info,0,out bits,IntPtr.Zero,0);
                    if(handle==IntPtr.Zero || bits==IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
                    previous=SelectObject(memory,handle);
                    pixels=new Bitmap(width,height,width*4,PixelFormat.Format32bppPArgb,bits);
                    Graphics=Graphics.FromImage(pixels);
                }
                catch { Dispose(); throw; }
            }
            internal void Present(IntPtr window,int x,int y)
            {
                Graphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
                var position=new Point(x,y); var size=new Size(Width,Height); var origin=new Point(); var blend=new Blend { Alpha=255,Format=1 };
                if(!UpdateLayeredWindow(window,screen,ref position,ref size,memory,ref origin,0,ref blend,2)) throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            internal Bitmap Snapshot() { Graphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync); return (Bitmap)pixels.Clone(); }
            public void Dispose()
            {
                if(Graphics!=null) { Graphics.Dispose(); Graphics=null; }
                if(pixels!=null) { pixels.Dispose(); pixels=null; }
                if(previous!=IntPtr.Zero && memory!=IntPtr.Zero) { SelectObject(memory,previous); previous=IntPtr.Zero; }
                if(handle!=IntPtr.Zero) { DeleteObject(handle); handle=IntPtr.Zero; }
                if(memory!=IntPtr.Zero) { DeleteDC(memory); memory=IntPtr.Zero; }
                if(screen!=IntPtr.Zero) { ReleaseDC(IntPtr.Zero,screen); screen=IntPtr.Zero; }
            }
        }
    }
}
