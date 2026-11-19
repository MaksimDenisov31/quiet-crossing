using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace QuietCrossing {
 public sealed partial class Window {
  sealed class Caption : IDisposable {
   public string Text;public float X,Y,Size;public Color Color;public bool Bold,Centered;public Region Clip;
   public void Dispose(){Clip.Dispose();}
  }
  readonly List<Caption> captions=new List<Caption>();bool collectingText;
  void ClearCaptions(){foreach(Caption caption in captions)caption.Dispose();captions.Clear();}
  void BeginTextFrame(){ClearCaptions();collectingText=true;}
  void CaptionAt(Graphics g,string text,float x,float y,float size,Color color,bool bold,bool centered){
   if(string.IsNullOrEmpty(text))return;
   if(!collectingText){PaintCaption(g,text,x,y,size,color,bold,centered);return;}
   PointF[] point={new PointF(x,y)};
   using(Matrix matrix=g.Transform){matrix.TransformPoints(point);var clip=new Region(g.VisibleClipBounds);clip.Transform(matrix);
    captions.Add(new Caption{Text=text,X=point[0].X,Y=point[0].Y,Size=size,Color=color,Bold=bold,Centered=centered,Clip=clip});}
  }
  static void PaintCaption(Graphics g,string text,float x,float y,float size,Color color,bool bold,bool centered){
   g.TextRenderingHint=TextRenderingHint.AntiAliasGridFit;
   using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var brush=new SolidBrush(color)){
    if(centered)x-=g.MeasureString(text,font,PointF.Empty,StringFormat.GenericTypographic).Width/2;
    g.DrawString(text,font,brush,x,y,StringFormat.GenericTypographic);
   }
  }
  void CoverCaptions(Graphics g,Color color,float x,float y,float width,float height){
   if(!collectingText||color.A!=255||captions.Count==0)return;
   PointF[] points={new PointF(x,y),new PointF(x+width,y+height)};
   using(Matrix matrix=g.Transform)matrix.TransformPoints(points);
   var bounds=new RectangleF(points[0].X,points[0].Y,points[1].X-points[0].X,points[1].Y-points[0].Y);
   foreach(Caption caption in captions)caption.Clip.Exclude(bounds);
  }
  void DrawCaptions(Graphics target,Rectangle destination,float logicalWidth=480,float logicalHeight=320){
   collectingText=false;GraphicsState state=target.Save();
   target.TranslateTransform(destination.X,destination.Y);target.ScaleTransform(destination.Width/logicalWidth,destination.Height/logicalHeight);
   target.PixelOffsetMode=PixelOffsetMode.Default;
   foreach(Caption caption in captions){GraphicsState item=target.Save();target.SetClip(caption.Clip,CombineMode.Intersect);PaintCaption(target,caption.Text,caption.X,caption.Y,caption.Size,caption.Color,caption.Bold,caption.Centered);target.Restore(item);}
   target.Restore(state);
  }
  void RenderFrame(Graphics target,Rectangle destination){
   BeginTextFrame();
   try{
    using(Graphics g=Graphics.FromImage(scene)){Draw(g);DrawFps(g);}
    target.InterpolationMode=InterpolationMode.NearestNeighbor;target.PixelOffsetMode=PixelOffsetMode.Half;target.DrawImage(scene,destination);
    DrawCaptions(target,destination);
   }finally{collectingText=false;}
  }
 }
}
