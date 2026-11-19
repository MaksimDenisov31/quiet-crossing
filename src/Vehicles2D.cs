using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace QuietCrossing {
 public sealed partial class Window {
  void VR(Graphics g,Car c,Color color,float x,float y,float width,float height){Rect(g,color,c.Direction>0?c.X+x:c.X-x-width,c.Y+y,width,height);}
  void DrawVehicle(Graphics g,Car c,float time){
   Color paint=Vehicles.Paint(c.Kind,carColors[c.Color%carColors.Length]),glass=Color.FromArgb(64,93,100),red=Color.FromArgb(207,76,65),blue=Color.FromArgb(93,164,205);
   float w=c.Width,d=c.Depth,back=-w/2,front=w/2;
   VR(g,c,Color.FromArgb(47,61,63),back+1,d/2,w,4);
   if(Vehicles.Rider(c.Kind)){
    VR(g,c,ink,-9,-2,4,4);VR(g,c,ink,5,-2,4,4);VR(g,c,c.Kind==VehicleKind.Bicycle?mint:paint,-6,-1,13,2);
    if(c.Kind==VehicleKind.Motorcycle)VR(g,c,glass,-5,-3,9,6);
    VR(g,c,paint,-3,-3,6,6);VR(g,c,cream,1,-3,4,5);VR(g,c,ink,6,-4,1,8);
    VR(g,c,Color.FromArgb(244,193,146),-1,-1,5,2);return;
   }
   foreach(float axle in new float[]{back+6,front-6}){VR(g,c,ink,axle-3,-d/2-2,6,3);VR(g,c,ink,axle-3,d/2-1,6,3);}
   if(w>50){VR(g,c,ink,back+14,-d/2-2,5,3);VR(g,c,ink,back+14,d/2-1,5,3);}
   VR(g,c,paint,back,-d/2,w,d);
   bool truck=c.Kind==VehicleKind.BoxTruck||c.Kind==VehicleKind.SemiTruck||c.Kind==VehicleKind.DumpTruck||c.Kind==VehicleKind.FireEngine||c.Kind==VehicleKind.TowTruck;
   if(truck){
    Color cargo=c.Kind==VehicleKind.SemiTruck?Color.FromArgb(210,214,199):c.Kind==VehicleKind.BoxTruck?Color.FromArgb(168,185,166):paint;
    VR(g,c,cargo,back+1,-d/2+1,w-17,d-2);VR(g,c,ink,front-16,-d/2,2,d);VR(g,c,glass,front-10,-d/2+2,4,d-4);
    if(c.Kind==VehicleKind.DumpTruck){VR(g,c,ink,back+4,-d/2+3,w-24,d-6);VR(g,c,Color.FromArgb(158,130,91),back+5,-d/2+4,w-26,d-8);for(int i=0;i<4;i++)VR(g,c,cream,back+7+i*6,-2,3,3);}
    if(c.Kind==VehicleKind.FireEngine){for(int y=-3;y<=3;y+=6)VR(g,c,cream,back+4,y,w-22,1);for(float x=back+4;x<front-17;x+=6)VR(g,c,cream,x,-3,1,7);}
    if(c.Kind==VehicleKind.TowTruck){VR(g,c,glass,back+3,-d/2+2,w-24,d-4);VR(g,c,cream,back+7,-1,14,2);VR(g,c,ink,back+6,-4,3,8);}
   }else if(c.Kind==VehicleKind.Bus){
    for(float x=back+5;x<front-6;x+=9){VR(g,c,glass,x,-d/2+1,6,3);VR(g,c,glass,x,d/2-4,6,3);}
    VR(g,c,glass,front-6,-d/2+1,4,d-2);VR(g,c,cream,-6,-3,12,6);
   }else{
    float cabin=c.Kind==VehicleKind.Van||c.Kind==VehicleKind.Ambulance||c.Kind==VehicleKind.Utility?w*.58f:w*.48f;
    VR(g,c,glass,-cabin/2,-d/2+1,cabin,d-2);VR(g,c,paint,-cabin/2+4,-d/2,cabin-8,d);
    if(c.Kind==VehicleKind.Suv||c.Kind==VehicleKind.SecuritySuv)VR(g,c,ink,back-1,-3,3,6);
   }
   if(c.Kind==VehicleKind.Taxi){VR(g,c,cream,-3,-2,7,4);for(int i=0;i<5;i++)VR(g,c,ink,back+4+i*4,d/2-2,2,1);}
   if(c.Kind==VehicleKind.Police){VR(g,c,blue,front-8,-d/2,8,d);VR(g,c,blue,back,-d/2,7,d);}
   if(c.Kind==VehicleKind.Ambulance){VR(g,c,cream,back+3,-d/2+1,w-16,d-2);VR(g,c,red,-9,-5,3,10);VR(g,c,red,-13,-1,11,3);VR(g,c,red,back+1,d/2-2,w-2,2);}
   if(c.Kind==VehicleKind.Utility){for(int i=0;i<4;i++)VR(g,c,ink,back+4+i*5,-d/2+1,2,3);VR(g,c,cream,-9,-2,10,4);}
   if(c.Kind==VehicleKind.PresidentialLimousine){VR(g,c,Color.FromArgb(72,91,96),-15,-5,30,10);VR(g,c,ink,-9,-5,18,10);VR(g,c,ink,front-8,-d/2-5,1,5);VR(g,c,cream,front-7,-d/2-5,5,3);VR(g,c,Color.FromArgb(223,187,99),front-6,-d/2-4,2,1);}
   if(Vehicles.Beacon(c.Kind)){
    bool flash=((int)(time*4)%2)==0;Color left=Vehicles.Amber(c.Kind)?Color.FromArgb(245,187,72):red;
    VR(g,c,ink,truck?front-12:-2,-5,4,10);VR(g,c,flash?left:Color.FromArgb(102,80,67),truck?front-11:-1,-4,2,3);VR(g,c,!flash?(Vehicles.Amber(c.Kind)?left:blue):Color.FromArgb(58,84,96),truck?front-11:-1,1,2,3);
   }
   VR(g,c,cream,front-2,-d/2+1,2,3);VR(g,c,cream,front-2,d/2-4,2,3);VR(g,c,red,back,-d/2+1,1,3);VR(g,c,red,back,d/2-4,1,3);
  }
  void CaptureVehicleGallery(){
   BeginTextFrame();
   int count=Enum.GetValues(typeof(VehicleKind)).Length,rows=(count+1)/2;
   using(var canvas=new Bitmap(480,rows*52+34))using(Graphics g=Graphics.FromImage(canvas)){
    g.Clear(ink);Center(g,"ТРАНСПОРТ ГОРОДА",8,16,cream,true);
    for(int i=0;i<count;i++){
     float x=12+(i%2)*238,y=34+(i/2)*52;Rect(g,Color.FromArgb(65,78,80),x,y,222,29);
     DrawVehicle(g,new Car(x+110,y+14,60,(VehicleKind)i,i%5),.25f);TextAt(g,Vehicles.Spec((VehicleKind)i).Name,x+3,y+33,9,cream);
    }
    using(var output=new Bitmap(canvas.Width*2,canvas.Height*2))using(Graphics scaled=Graphics.FromImage(output)){scaled.InterpolationMode=InterpolationMode.NearestNeighbor;scaled.PixelOffsetMode=PixelOffsetMode.Half;scaled.DrawImage(canvas,0,0,output.Width,output.Height);DrawCaptions(scaled,new Rectangle(0,0,output.Width,output.Height),canvas.Width,canvas.Height);output.Save("docs/traffic-gallery.png",ImageFormat.Png);}
   }
  }
 }
}
