using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace LabPals
{
    public static class PetRenderer
    {
        public const int Width = 320, Height = 280;
        public static readonly Color Ink = Color.FromArgb(48, 50, 55);
        private static readonly Dictionary<Character, SpriteRig> rigs = new Dictionary<Character, SpriteRig>();
        private static float cachedScale;

        public static Bitmap Render(Character c, Pose pose, int phase, float scale, string timer)
        { return Render(c,pose,phase,scale,timer,new HandSample { Press=pose==Pose.TapLeft?1:0 },new HandSample { Press=pose==Pose.TapRight?1:0 }); }
        public static Bitmap Render(Character c, Pose pose, int phase, float scale, string timer, HandSample left, HandSample right)
        {
            var image=new Bitmap((int)Math.Ceiling(Width*scale),(int)Math.Ceiling(Height*scale),PixelFormat.Format32bppPArgb);
            using(var g=Graphics.FromImage(image)) DrawTo(g,c,pose,phase,scale,timer,left,right); return image;
        }
        public static void Draw(Graphics g, Character c, Pose pose, int phase, string timer)
        {
            var state=g.Save(); DrawScene(g,c,pose,phase,1,timer,new HandSample { Press=pose==Pose.TapLeft?1:0 },new HandSample { Press=pose==Pose.TapRight?1:0 }); g.Restore(state);
        }
        internal static void DrawTo(Graphics g, Character c, Pose pose, int phase, float scale, string timer, HandSample left, HandSample right)
        { g.ResetTransform(); g.Clear(Color.Transparent); DrawScene(g,c,pose,phase,scale,timer,left,right); }
        internal static float ReactionWeight(int phase)
        {
            float time=Math.Max(0,Math.Min(AnimationTiming.PatPhases,phase))*AnimationTiming.PoseFrameMs;
            float t=time<AnimationTiming.PatInMs?time/AnimationTiming.PatInMs:time<AnimationTiming.PatHoldUntilMs?1:(AnimationTiming.PatMs-time)/(AnimationTiming.PatMs-AnimationTiming.PatHoldUntilMs);
            t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);
        }
        private static void DrawScene(Graphics g, Character c, Pose pose, int phase, float scale, string timer, HandSample left, HandSample right)
        {
            if(Math.Abs(cachedScale-scale)>.001) { ClearCache(); cachedScale=scale; }
            SpriteRig rig;if(!rigs.TryGetValue(c,out rig)) { rig=new SpriteRig(c,scale); rigs.Add(c,rig); }
            if(pose==Pose.Sleep)
            {
                g.DrawImageUnscaled(rig.SleepFrame(phase),0,0);
            }
            else
            {
                float reaction=pose==Pose.Pat?ReactionWeight(phase):0;
                float lp=left.Press+left.Impact*.08f,rp=right.Press+right.Impact*.08f;
                g.DrawImageUnscaled(rig.Frame(pose==Pose.Blink,lp,rp,reaction),0,0);
            }
            if(!string.IsNullOrEmpty(timer))
            {
                var state=g.Save();g.ScaleTransform(scale,scale);
                using(var path=Rounded(126,264,68,15,7))using(var brush=new SolidBrush(Color.FromArgb(240,251,251,252)))g.FillPath(brush,path);
                using(var font=new Font("Segoe UI",9,FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(CharacterCatalog.Get(c).Accent))
                using(var format=new StringFormat { Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center })g.DrawString(timer,font,brush,new RectangleF(126,263,68,16),format);
                g.Restore(state);
            }
        }
        public static void ClearCache() { foreach(var rig in rigs.Values)rig.Dispose();rigs.Clear(); }
        public static Icon MakeIcon()
        {
            using (var stream = typeof(PetRenderer).Assembly.GetManifestResourceStream("LabPals.App.Icon"))
            {
                if (stream == null) throw new InvalidOperationException("缺少应用图标。");
                using (var icon = new Icon(stream)) return (Icon)icon.Clone();
            }
        }
        public static GraphicsPath Rounded(float x,float y,float w,float h,float r)
        {
            var p=new GraphicsPath();float d=r*2;p.AddArc(x,y,d,d,180,90);p.AddArc(x+w-d,y,d,d,270,90);p.AddArc(x+w-d,y+h-d,d,d,0,90);p.AddArc(x,y+h-d,d,d,90,90);p.CloseFigure();return p;
        }

    }
}
