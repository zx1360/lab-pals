using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;

namespace LabPals
{
    internal sealed class SpriteRig : IDisposable
    {
        private readonly Bitmap body,blinkBody,leftHand,rightHand,face,open,rest,composite;
        private readonly Point leftOrigin,rightOrigin;
        internal readonly Bitmap Sleep;
        private readonly Graphics compositor;
        private readonly float scale;
        private readonly Character character;
        private readonly ImageAttributes[] faceOpacity=new ImageAttributes[33];
        private byte[] sleepFrom,sleepTo,sleepMix;
        private int sleepPhase=-1;
        internal SpriteRig(Character c,float scale)
        {
            character=c;this.scale=scale;int w=(int)Math.Ceiling(PetRenderer.Width*scale),h=(int)Math.Ceiling(PetRenderer.Height*scale);
            open=Load(c+".png",w,h);rest=Load(c+"-rest.png",w,h);
            body=(Bitmap)open.Clone();blinkBody=(Bitmap)rest.Clone();
            leftHand=Load(c+"-left.png",w,h);rightHand=Load(c+"-right.png",w,h);
            face=Load(c+"-reaction.png",w,h);Sleep=Load(c+"-sleep.png",w,h);
            SplitHands();leftHand=Trim(leftHand,out leftOrigin);rightHand=Trim(rightHand,out rightOrigin);
            composite=new Bitmap(w,h,PixelFormat.Format32bppPArgb);
            compositor=Graphics.FromImage(composite);
            for(int i=0;i<faceOpacity.Length;i++)
            {
                var attributes=new ImageAttributes();var matrix=new ColorMatrix();matrix.Matrix33=i/32f;attributes.SetColorMatrix(matrix);faceOpacity[i]=attributes;
            }
        }
        private static byte[] Pixels(Bitmap image,byte[] write)
        {
            var data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),write==null?ImageLockMode.ReadOnly:ImageLockMode.WriteOnly,PixelFormat.Format32bppPArgb);
            try { if(write!=null){Marshal.Copy(write,0,data.Scan0,write.Length);return write;}var result=new byte[image.Width*image.Height*4];Marshal.Copy(data.Scan0,result,0,result.Length);return result; }
            finally { image.UnlockBits(data); }
        }
        private void SplitHands()
        {
            var original=Pixels(open,null);var basePixels=(byte[])original.Clone();var closed=Pixels(rest,null);
            var left=Pixels(leftHand,null);var right=Pixels(rightHand,null);
            for(int i=0;i<original.Length;i+=4)
            {
                bool l=original[i+3]>0&&left[i+3]*2>=original[i+3];
                bool r=!l&&original[i+3]>0&&right[i+3]*2>=original[i+3];
                for(int c=0;c<4;c++)
                {
                    left[i+c]=l?original[i+c]:(byte)0;right[i+c]=r?original[i+c]:(byte)0;
                    if(l||r){basePixels[i+c]=0;closed[i+c]=0;}
                }
            }
            Pixels(body,basePixels);Pixels(blinkBody,closed);Pixels(leftHand,left);Pixels(rightHand,right);
        }
        private static Bitmap Load(string name,int width,int height)
        {
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("LabPals.Art."+name))
            {
                if(stream==null)throw new InvalidOperationException("缺少角色素材："+name);
                using(var source=new Bitmap(stream))
                {
                    var image=new Bitmap(width,height,PixelFormat.Format32bppPArgb);
                    using(var g=Graphics.FromImage(image))using(var attributes=new ImageAttributes())
                    {
                        g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;attributes.SetWrapMode(WrapMode.TileFlipXY);
                        g.DrawImage(source,new Rectangle(0,0,width,height),0,0,source.Width,source.Height,GraphicsUnit.Pixel,attributes);
                    }
                    return image;
                }
            }
        }
        private static Bitmap Trim(Bitmap image,out Point origin)
        {
            var pixels=Pixels(image,null);int x0=image.Width,y0=image.Height,x1=0,y1=0;
            for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(pixels[(y*image.Width+x)*4+3]>0)
            {x0=Math.Min(x0,x);y0=Math.Min(y0,y);x1=Math.Max(x1,x);y1=Math.Max(y1,y);}
            if(x0>x1)throw new InvalidOperationException("角色缺少人手图层。");
            x0=Math.Max(0,x0-2);y0=Math.Max(0,y0-2);x1=Math.Min(image.Width-1,x1+2);y1=Math.Min(image.Height-1,y1+2);
            origin=new Point(x0,y0);var cropped=image.Clone(new Rectangle(x0,y0,x1-x0+1,y1-y0+1),PixelFormat.Format32bppPArgb);image.Dispose();return cropped;
        }
        private void Hand(Graphics g,Bitmap hand,bool right,float amount)
        {
            Point origin=right?rightOrigin:leftOrigin;
            if(Math.Abs(amount)<.004){g.DrawImageUnscaled(hand,origin.X,origin.Y);return;}
            amount=Math.Max(-.1f,Math.Min(1.1f,amount));
            var definition=CharacterCatalog.Get(character);var wrist=right?definition.RightWrist:definition.LeftWrist;
            float px=wrist.X*scale,py=wrist.Y*scale;
            var state=g.Save();g.TranslateTransform(px,py);
            g.RotateTransform((right?-1:1)*2.6f*amount);
            // A small wrist/finger articulation affects only the extracted skin layer.
            g.ScaleTransform(1,1+.075f*amount);g.TranslateTransform(-px,-py);
            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
            g.DrawImage(hand,new Rectangle(origin.X,origin.Y,hand.Width,hand.Height));g.Restore(state);
        }
        private void Compose(Graphics g,Bitmap basis,float left,float right,float reaction)
        {
            sleepPhase=-1;
            g.ResetTransform();g.CompositingMode=CompositingMode.SourceCopy;g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceOver;
            Hand(g,leftHand,false,left);Hand(g,rightHand,true,right);
            // Clothing is a fixed foreground layer and occludes wrists naturally.
            g.DrawImageUnscaled(basis,0,0);
            if(reaction>.002)
            {
                int opacity=(int)Math.Round(Math.Min(1,reaction)*32);
                g.DrawImage(face,new Rectangle(0,0,face.Width,face.Height),0,0,face.Width,face.Height,GraphicsUnit.Pixel,faceOpacity[opacity]);
            }
        }
        internal Bitmap Frame(bool closed,float left,float right,float reaction)
        {
            if(Math.Abs(left)<.004&&Math.Abs(right)<.004&&reaction<.002)return closed?rest:open;
            Compose(compositor,closed?blinkBody:body,left,right,reaction);compositor.Flush();return composite;
        }
        internal Bitmap SleepFrame(int phase)
        {
            if(phase<=0)return rest;
            if(phase>=AnimationTiming.SleepPhases)return Sleep;
            if(phase==sleepPhase)return composite;
            if(sleepFrom==null){sleepFrom=Pixels(rest,null);sleepTo=Pixels(Sleep,null);sleepMix=new byte[sleepFrom.Length];}
            // Blend premultiplied pixels directly so transparent edges do not dim or acquire halos.
            float t=phase/(float)AnimationTiming.SleepPhases;t=t*t*(3-2*t);int weight=(int)Math.Round(t*256),inverse=256-weight;
            for(int i=0;i<sleepMix.Length;i++)sleepMix[i]=(byte)((sleepFrom[i]*inverse+sleepTo[i]*weight+128)>>8);
            Pixels(composite,sleepMix);sleepPhase=phase;return composite;
        }
        public void Dispose()
        {
            compositor.Dispose();body.Dispose();blinkBody.Dispose();leftHand.Dispose();rightHand.Dispose();face.Dispose();Sleep.Dispose();open.Dispose();rest.Dispose();composite.Dispose();
            foreach(var attributes in faceOpacity)attributes.Dispose();
        }
    }
}
