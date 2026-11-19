using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 public sealed partial class Window {
  [StructLayout(LayoutKind.Sequential)] struct BitmapHeader {
   public uint Size;public int Width,Height;public ushort Planes,Bits;public uint Compression,ImageSize;public int XResolution,YResolution;public uint Colors,Important;
  }
  [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr dc,int mode);
  [DllImport("gdi32.dll")] static extern int StretchDIBits(IntPtr dc,int x,int y,int width,int height,int sourceX,int sourceY,int sourceWidth,int sourceHeight,IntPtr pixels,ref BitmapHeader info,uint usage,uint operation);
  [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapHeader header,uint usage,out IntPtr bits,IntPtr section,uint offset);
  [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc,IntPtr item);
  [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr item);
  [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
  [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr target,int x,int y,int width,int height,IntPtr source,int sourceX,int sourceY,uint operation);
  [DllImport("gdi32.dll")] static extern bool GdiFlush();
  IntPtr surfaceDc,surfaceBitmap,surfacePrevious,surfaceBits;Graphics surfaceGraphics;Size surfaceSize;
  readonly int[] sourcePixels=new int[480*320];int[] expandedRow,xSamples;int sampledWidth;
  void EnsureSurface(){
   if(surfaceGraphics!=null&&surfaceSize==ClientSize)return;
   DisposeSurface();surfaceSize=ClientSize;
   var header=new BitmapHeader{Size=40,Width=surfaceSize.Width,Height=-surfaceSize.Height,Planes=1,Bits=32};IntPtr bits;
   surfaceDc=CreateCompatibleDC(IntPtr.Zero);surfaceBitmap=CreateDIBSection(surfaceDc,ref header,0,out bits,IntPtr.Zero,0);surfaceBits=bits;
   if(surfaceDc==IntPtr.Zero||surfaceBitmap==IntPtr.Zero)throw new System.ComponentModel.Win32Exception("Cannot allocate frame buffer");
   surfacePrevious=SelectObject(surfaceDc,surfaceBitmap);surfaceGraphics=Graphics.FromHdc(surfaceDc);surfaceGraphics.Clear(ink);
  }
  void BlitSurface(Graphics target){
   surfaceGraphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);IntPtr dc=target.GetHdc();
   try{BitBlt(dc,0,0,surfaceSize.Width,surfaceSize.Height,surfaceDc,0,0,0x00CC0020);}finally{target.ReleaseHdc(dc);}
  }
  void DisposeSurface(){
   if(surfaceGraphics!=null){surfaceGraphics.Dispose();surfaceGraphics=null;}
   if(surfaceDc!=IntPtr.Zero&&surfacePrevious!=IntPtr.Zero)SelectObject(surfaceDc,surfacePrevious);
   if(surfaceBitmap!=IntPtr.Zero)DeleteObject(surfaceBitmap);
   if(surfaceDc!=IntPtr.Zero)DeleteDC(surfaceDc);
   surfaceDc=surfaceBitmap=surfacePrevious=surfaceBits=IntPtr.Zero;
  }
  void PresentPixels(Graphics target,Rectangle destination){
   if(target==surfaceGraphics){ExpandPixels(destination);return;}
   var header=new BitmapHeader{Size=40,Width=scene.Width,Height=-scene.Height,Planes=1,Bits=32};
   BitmapData data=scene.LockBits(new Rectangle(0,0,scene.Width,scene.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   IntPtr dc=IntPtr.Zero;
   try{dc=target.GetHdc();int previous=SetStretchBltMode(dc,3);StretchDIBits(dc,destination.X,destination.Y,destination.Width,destination.Height,0,0,scene.Width,scene.Height,data.Scan0,ref header,0,0x00CC0020);SetStretchBltMode(dc,previous);}
   finally{if(dc!=IntPtr.Zero)target.ReleaseHdc(dc);scene.UnlockBits(data);}
  }
  unsafe void ExpandPixels(Rectangle destination){
   surfaceGraphics.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);GdiFlush();
   BitmapData data=scene.LockBits(new Rectangle(0,0,480,320),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   try{Marshal.Copy(data.Scan0,sourcePixels,0,sourcePixels.Length);}finally{scene.UnlockBits(data);}
   if(sampledWidth!=destination.Width){sampledWidth=destination.Width;expandedRow=new int[sampledWidth];xSamples=new int[sampledWidth];for(int x=0;x<sampledWidth;x++)xSamples[x]=x*480/sampledWidth;}
   int previous=-1;IntPtr previousRow=IntPtr.Zero;long bytes=destination.Width*4L;
   fixed(int* pixels=sourcePixels,samples=xSamples,expanded=expandedRow)for(int y=0;y<destination.Height;y++){
    int source=y*320/destination.Height;IntPtr row=IntPtr.Add(surfaceBits,((destination.Y+y)*surfaceSize.Width+destination.X)*4);
    if(source==previous)Buffer.MemoryCopy(previousRow.ToPointer(),row.ToPointer(),bytes,bytes);
    else{int* sourceRow=pixels+source*480;for(int x=0;x<destination.Width;x++)expanded[x]=sourceRow[samples[x]];Buffer.MemoryCopy(expanded,row.ToPointer(),bytes,bytes);previous=source;previousRow=row;}
   }
  }
  protected override void OnPaintBackground(System.Windows.Forms.PaintEventArgs e){
   // The persistent frame buffer covers the window in a single blit.
  }
 }
}
