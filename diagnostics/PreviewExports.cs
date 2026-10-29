using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace LabPals
{
    internal static class PreviewExports
    {
        private struct Stroke { public int Time,Identity,Mask; public bool Down; }
        internal static void ExportTyping(string folder)
        {
            string frames=Path.Combine(folder,"typing-frames"); Directory.CreateDirectory(frames);
            var events=new List<Stroke>();
            Action<int,int,int,int> stroke=delegate(int time,int id,int mask,int duration)
            {
                events.Add(new Stroke { Time=time,Identity=id,Mask=mask,Down=true });
                events.Add(new Stroke { Time=time+duration,Identity=id,Mask=mask,Down=false });
            };
            stroke(250,30,1,50); stroke(340,36,2,45); stroke(410,30,1,60); stroke(445,36,2,65);
            for(int i=0;i<7;i++) stroke(650+i*90,i%2==0 ? 30 : 36,i%2==0 ? 1 : 2,40);
            stroke(1450,57,3,100); events.Sort(delegate(Stroke a,Stroke b) { return a.Time.CompareTo(b.Time); });
            var brain=new PetBrain(0); int index=0;
            using(var sheet=new Bitmap(256*CharacterCatalog.All.Count,300))
            using(var g=Graphics.FromImage(sheet))
            {
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using(var font=new Font("Microsoft YaHei UI",14,FontStyle.Regular,GraphicsUnit.Pixel))
                using(var title=new Font("Microsoft YaHei UI",15,FontStyle.Bold,GraphicsUnit.Pixel))
                using(var ink=new SolidBrush(PetRenderer.Ink))
                {
                    for(int frame=0;frame<100;frame++)
                    {
                        int t=frame*20;
                        while(index<events.Count && events[index].Time<=t)
                        {
                            var e=events[index++]; brain.KeyTransition(e.Identity,e.Mask,e.Down,e.Time);
                        }
                        brain.Step(t); g.Clear(Color.FromArgb(246,243,235));
                        g.DrawString("按下立即落掌 · 松开平滑回弹",title,ink,22,9);
                        using(var dim=new SolidBrush(Color.FromArgb(146,136,126))) g.DrawString("与桌宠共用动作逻辑 / 50 fps 预览",font,dim,730,11);
                        for(int i=0;i<CharacterCatalog.All.Count;i++)
                        {
                            using(var pet=PetRenderer.Render(CharacterCatalog.All[i].Id,brain.Pose,brain.Phase(t),0.8f,null,brain.LeftHand(t),brain.RightHand(t))) g.DrawImageUnscaled(pet,i*256,32);
                            using(var accent=new SolidBrush(CharacterCatalog.All[i].Accent)) g.DrawString(CharacterCatalog.All[i].Name,font,accent,i*256+83,267);
                        }
                        sheet.Save(Path.Combine(frames,frame.ToString("000")+".png"),ImageFormat.Png);
                    }
                }
            }
        }
        internal static void ExportPersonalities(string folder)
        {
            using(var sheet=new Bitmap(320*CharacterCatalog.All.Count,420)) using(var g=Graphics.FromImage(sheet))
            {
                g.Clear(Color.FromArgb(246,243,235)); g.SmoothingMode=SmoothingMode.AntiAlias;
                g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using(var title=new Font("Microsoft YaHei UI",25,FontStyle.Bold,GraphicsUnit.Pixel)) using(var b=new SolidBrush(PetRenderer.Ink)) g.DrawString("被轻轻摸头时，各自的小回应。",title,b,30,20);
                for(int i=0;i<CharacterCatalog.All.Count;i++)
                {
                    var state=g.Save(); g.TranslateTransform(i*320,60); PetRenderer.Draw(g,CharacterCatalog.All[i].Id,Pose.Pat,10,null); g.Restore(state);
                    using(var font=new Font("Microsoft YaHei UI",18,FontStyle.Bold,GraphicsUnit.Pixel)) using(var b=new SolidBrush(PetRenderer.Ink)) g.DrawString(CharacterCatalog.All[i].Name,font,b,i*320+100,345);
                    using(var font=new Font("Microsoft YaHei UI",14,FontStyle.Regular,GraphicsUnit.Pixel)) using(var b=new SolidBrush(CharacterCatalog.All[i].Accent)) g.DrawString(CharacterCatalog.All[i].ReactionCaption,font,b,i*320+95,380);
                }
                sheet.Save(Path.Combine(folder,"点击回应.png"),ImageFormat.Png);
            }
        }
        internal static void ExportSleep(string folder)
        {
            using(var sheet=new Bitmap(320*CharacterCatalog.All.Count,410))using(var g=Graphics.FromImage(sheet))
            {
                g.Clear(Color.FromArgb(246,243,235));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using(var font=new Font("Microsoft YaHei UI",25,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(PetRenderer.Ink))g.DrawString("读累了，就一起休息一会儿。",font,b,30,20);
                for(int i=0;i<CharacterCatalog.All.Count;i++)
                {
                    var state=g.Save();g.TranslateTransform(i*320,58);PetRenderer.Draw(g,CharacterCatalog.All[i].Id,Pose.Sleep,12,null);g.Restore(state);
                    using(var font=new Font("Microsoft YaHei UI",18,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(PetRenderer.Ink))g.DrawString(CharacterCatalog.All[i].Name,font,b,i*320+100,354);
                }
                sheet.Save(Path.Combine(folder,"入睡姿势.png"),ImageFormat.Png);
            }
        }
        internal static void ExportMayuriDetails(string folder)
        {
            float[] scales={.55f,.75f,1f,1.25f};
            using(var sheet=new Bitmap(1140,800))using(var g=Graphics.FromImage(sheet))
            using(var font=new Font("Microsoft YaHei UI",16,FontStyle.Regular,GraphicsUnit.Pixel))
            using(var ink=new SolidBrush(PetRenderer.Ink))
            {
                g.Clear(Color.FromArgb(246,243,235));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                int x=0;
                for(int i=0;i<scales.Length;i++)
                {
                    float scale=scales[i];int width=(int)Math.Ceiling(PetRenderer.Width*scale);
                    g.DrawString(new[]{"迷你","小号","标准","大号"}[i]+" / "+width+" px",font,ink,x+12,12);
                    using(var image=PetRenderer.Render(Character.Mayuri,Pose.Idle,0,scale,null))g.DrawImageUnscaled(image,x,380-image.Height);
                    using(var image=PetRenderer.Render(Character.Mayuri,Pose.Sleep,AnimationTiming.SleepPhases,scale,null))g.DrawImageUnscaled(image,x,780-image.Height);
                    x+=width+16;
                }
                sheet.Save(Path.Combine(folder,"真由理尺寸检查.png"),ImageFormat.Png);
            }
        }
        internal static void ExportInteraction(string folder)
        {
            string frames=Path.Combine(folder,"interaction-frames");Directory.CreateDirectory(frames);var brain=new PetBrain(0);
            using(var sheet=new Bitmap(256*CharacterCatalog.All.Count,300))using(var g=Graphics.FromImage(sheet))
            using(var font=new Font("Microsoft YaHei UI",15,FontStyle.Bold,GraphicsUnit.Pixel))using(var ink=new SolidBrush(PetRenderer.Ink))
            {
                for(int frame=0;frame<160;frame++)
                {
                    int t=frame*20;if(t==300)brain.Pat(t);if(t==640)brain.KeyTransition(30,1,true,t);if(t==700)brain.KeyTransition(30,1,false,t);if(t==1800)brain.Sleep(t);if(t==2800)brain.Pat(t);brain.Step(t);
                    g.Clear(Color.FromArgb(246,243,235));g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                    string label=t<300?"平时安静陪伴":t<1200?"轻点回应，输入仍可即时跟随":t<1800?"回应结束，恢复待机":t<2800?"休息：各自的睡姿，停止敲击":"轻点叫醒";
                    g.DrawString(label,font,ink,22,10);
                    for(int i=0;i<CharacterCatalog.All.Count;i++)
                    {
                        using(var pet=PetRenderer.Render(CharacterCatalog.All[i].Id,brain.Pose,brain.Phase(t),.8f,null,brain.LeftHand(t),brain.RightHand(t)))g.DrawImageUnscaled(pet,i*256,32);
                        using(var b=new SolidBrush(CharacterCatalog.All[i].Accent))g.DrawString(CharacterCatalog.All[i].Name,font,b,i*256+78,267);
                    }
                    sheet.Save(Path.Combine(frames,frame.ToString("000")+".png"),ImageFormat.Png);
                }
            }
        }
    }
}
