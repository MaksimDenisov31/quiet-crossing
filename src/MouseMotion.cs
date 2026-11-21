using System;
using System.Collections.Generic;
using System.Drawing;

namespace QuietCrossing {
 // Preserve every relative count, regardless of device polling rate. Absolute
 // devices have their own baseline; entering capture never produces a jump.
 sealed class MouseMotion {
  struct Absolute {public double X,Y;public Rectangle Bounds;}
  readonly Dictionary<IntPtr,Absolute> devices=new Dictionary<IntPtr,Absolute>();
  double pendingX,pendingY;
  public void Add(IntPtr device,ushort flags,int x,int y,Rectangle desktop){
   if((flags&1)==0){if(devices.Count>0)devices.Remove(device);pendingX+=x;pendingY+=y;return;}
   var point=new Absolute{X=desktop.X+(double)Math.Max(0,Math.Min(65535,x))*(desktop.Width-1)/65535.0,Y=desktop.Y+(double)Math.Max(0,Math.Min(65535,y))*(desktop.Height-1)/65535.0,Bounds=desktop};
   Absolute before;if(devices.TryGetValue(device,out before)&&before.Bounds==desktop){pendingX+=point.X-before.X;pendingY+=point.Y-before.Y;}
   devices[device]=point;
  }
  public void Take(out float x,out float y){x=(float)pendingX;y=(float)pendingY;pendingX=pendingY=0;}
  public void Clear(){pendingX=pendingY=0;devices.Clear();}
 }
}
