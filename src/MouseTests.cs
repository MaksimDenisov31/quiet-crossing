using System;
using System.Drawing;
using System.Text;

namespace QuietCrossing {
 static partial class Tests {
  static void MouseChecks(StringBuilder log){
   foreach(int rate in new[]{125,500,1000,4000,8000})foreach(int fps in new[]{60,120,170}){
    var mouse=new MouseMotion();double x=0,y=0;int lastFrame=-1;
    for(int i=0;i<rate;i++){
     mouse.Add(new IntPtr(1),0,(i+1)*1600/rate-i*1600/rate,(i+1)*240/rate-i*240/rate,Rectangle.Empty);
     int frame=i*fps/rate;if(frame!=lastFrame){float dx,dy;mouse.Take(out dx,out dy);x+=dx;y+=dy;lastFrame=frame;}
    }
    float restX,restY;mouse.Take(out restX,out restY);x+=restX;y+=restY;
    Check(x==1600&&y==240,"mouse counts preserved at "+rate+" Hz and "+fps+" FPS",log);
   }
   var motion=new MouseMotion();var desktop=new Rectangle(-1920,0,4480,1440);float a,b;
   motion.Add(new IntPtr(1),3,50000,30000,desktop);motion.Take(out a,out b);
   Check(a==0&&b==0,"first absolute mouse packet sets a baseline without a camera jump",log);
   motion.Add(new IntPtr(1),3,51000,31000,desktop);motion.Take(out a,out b);
   Check(Math.Abs(a-1000*4479/65535.0)<.001&&Math.Abs(b-1000*1439/65535.0)<.001,"absolute mouse uses actual primary/virtual desktop coordinates",log);
   motion.Add(new IntPtr(2),3,1000,5000,desktop);motion.Take(out a,out b);
   Check(a==0&&b==0,"switching absolute devices does not jump to another device's cursor",log);
   motion.Add(new IntPtr(1),3,52000,32000,new Rectangle(0,0,2560,1440));motion.Take(out a,out b);
   Check(a==0&&b==0,"display layout change resets absolute baseline",log);
   motion.Add(IntPtr.Zero,0,1000,-1000,desktop);motion.Clear();motion.Take(out a,out b);
   Check(a==0&&b==0,"pause/cursor release discards pending mouse movement",log);
   motion.Add(IntPtr.Zero,0,50,-30,desktop);motion.Add(IntPtr.Zero,0,-50,30,desktop);motion.Take(out a,out b);
   Check(a==0&&b==0,"opposite high-rate packets cancel without an artificial drift",log);
  }
 }
}
