using System;
using System.Drawing;

namespace QuietCrossing {
 sealed partial class PixelWorld {
  float trafficTime;
  void VB(Car c,Color color,float x1,float y1,float z1,float x2,float y2,float z2){float a=c.X+c.Direction*x1,b=c.X+c.Direction*x2;Box(Math.Min(a,b),c.Y+y1,z1,Math.Max(a,b),c.Y+y2,z2,color);}
  void DrawCar(Car c,Color civilian){
   Color paint=Vehicles.Paint(c.Kind,civilian),glass=Color.FromArgb(64,93,100),red=Color.FromArgb(207,76,65),blue=Color.FromArgb(93,164,205);
   float back=-c.Width/2,front=c.Width/2,d=c.Depth/2,h=Vehicles.Spec(c.Kind).Height;
   Ground(c.X-c.Width/2-1,c.Y-d-2,c.X+c.Width/2+2,c.Y+d+3,.18f,Color.FromArgb(50,66,64));
   if(Vehicles.Rider(c.Kind)){
    VB(c,Ink,-9,-1.5f,1,-5,1.5f,7);VB(c,Ink,5,-1.5f,1,9,1.5f,7);
    VB(c,c.Kind==VehicleKind.Bicycle?Cream:paint,-6,-1,5,7,1,8);if(c.Kind==VehicleKind.Motorcycle)VB(c,glass,-5,-2.5f,6,5,2.5f,10);
    VB(c,Ink,-3,-2,7,0,2,13);VB(c,paint,-2,-3,12,3,3,18);VB(c,Color.FromArgb(241,188,142),3,-3,12,5,3,15);
    VB(c,Cream,0,-2.5f,18,5,2.5f,22);VB(c,Ink,5,-4,9,6,4,11);return;
   }
   foreach(float axle in new float[]{back+6,front-6}){VB(c,Ink,axle-3,-d-2,1,axle+3,-d+1,7);VB(c,Ink,axle-3,d-1,1,axle+3,d+2,7);}
   if(c.Width>50){VB(c,Ink,back+11,-d-2,1,back+16,-d+1,7);VB(c,Ink,back+11,d-1,1,back+16,d+2,7);}
   VB(c,paint,back,-d,4,front,d,11);
   bool heavy=c.Kind==VehicleKind.BoxTruck||c.Kind==VehicleKind.SemiTruck||c.Kind==VehicleKind.DumpTruck||c.Kind==VehicleKind.FireEngine||c.Kind==VehicleKind.TowTruck;
   if(c.Kind==VehicleKind.Bus){
    VB(c,paint,back,-d,7,front,d,h);VB(c,glass,front-.2f,-d+1,16,front+.2f,d-1,h-3);
    for(float x=back+5;x<front-6;x+=9)foreach(float yy in new float[]{-d-.3f,d+.1f})VB(c,glass,x,yy,17,x+6,yy+.2f,h-4);
    VB(c,Cream,-7,-4,h,7,4,h+2);
   }else if(heavy){
    VB(c,paint,front-14,-d,7,front,d,23);VB(c,glass,front-7,-d+.5f,14,front-2,d-.5f,21);VB(c,paint,front-11,-d,22,front-2,d,24);
    Color cargo=c.Kind==VehicleKind.SemiTruck?Color.FromArgb(210,214,199):c.Kind==VehicleKind.BoxTruck?Color.FromArgb(168,185,166):paint;
    if(c.Kind==VehicleKind.SemiTruck||c.Kind==VehicleKind.BoxTruck)VB(c,cargo,back,-d,8,front-16,d,h);
    if(c.Kind==VehicleKind.DumpTruck){
     VB(c,Color.FromArgb(150,124,88),back+3,-d+2,10,front-18,d-2,23);
     foreach(float yy in new float[]{-d,d-2})VB(c,paint,back,yy,9,front-16,yy+2,26);
     for(int i=0;i<3;i++)VB(c,Color.FromArgb(186,163,124),back+5+i*8,-3,22,back+11+i*8,3,27);
    }
    if(c.Kind==VehicleKind.FireEngine){
     VB(c,paint,back,-d,8,front-16,d,22);foreach(float yy in new float[]{-d-.2f,d+.1f})VB(c,Cream,back+2,yy,12,front-2,yy+.2f,15);
     VB(c,Cream,back+4,-4,25,front-17,-3,27);VB(c,Cream,back+4,3,25,front-17,4,27);
     for(float x=back+4;x<front-17;x+=6)VB(c,Cream,x,-3,25,x+1,3,26);
    }
    if(c.Kind==VehicleKind.TowTruck){VB(c,glass,back,-d+1,8,front-16,d-1,10);VB(c,paint,back+8,-1,10,back+10,1,24);VB(c,Cream,back+8,-1,23,back+21,1,25);VB(c,Ink,back+18,-1,18,back+21,1,23);}
   }else if(c.Kind==VehicleKind.Van||c.Kind==VehicleKind.Ambulance||c.Kind==VehicleKind.Utility){
    VB(c,paint,back,-d,8,front-10,d,h);VB(c,glass,front-10,-d+1,12,front-3,d-1,20);VB(c,paint,front-9,-d,20,front-3,d,22);
   }else{
    VB(c,glass,-c.Width*.23f,-d+1,11,c.Width*.23f,d-1,h-1);VB(c,paint,-c.Width*.15f,-d,11,c.Width*.15f,d,h-1);VB(c,paint,-c.Width*.25f,-d, h-1,c.Width*.25f,d,h);
   }
   if(c.Kind==VehicleKind.Police){VB(c,blue,front-8,-d,11,front,d,12);foreach(float yy in new float[]{-d-.3f,d+.1f})VB(c,blue,back+2,yy,7,front-2,yy+.2f,10);}
   if(c.Kind==VehicleKind.Taxi){VB(c,Cream,-3,-3,h,4,3,h+3);foreach(float yy in new float[]{-d-.3f,d+.1f})for(float x=back+4;x<front-4;x+=5)VB(c,Ink,x,yy,8,x+2,yy+.2f,10);}
   if(c.Kind==VehicleKind.Ambulance){
    foreach(float yy in new float[]{-d-.3f,d+.1f}){VB(c,red,back+2,yy,11,front-2,yy+.2f,14);VB(c,red,-9,yy,17,-6,yy+.2f,25);VB(c,red,-13,yy,20,-2,yy+.2f,23);}
    VB(c,red,-9,-5,h,-6,5,h+.3f);VB(c,red,-13,-1,h,-2,2,h+.3f);
   }
   if(c.Kind==VehicleKind.Utility)for(float x=back+4;x<0;x+=5)foreach(float yy in new float[]{-d-.2f,d+.1f})VB(c,Ink,x,yy,8,x+2,yy+.2f,18);
   if(c.Kind==VehicleKind.PresidentialLimousine){VB(c,Ink,front-8,-d-1,11,front-7,-d,25);VB(c,Cream,front-7,-d-1,21,front-2,-d,25);VB(c,Color.FromArgb(223,187,99),front-6,-d-1.2f,22,front-4,-d-1,24);}
   if(Vehicles.Beacon(c.Kind)){
    bool flash=((int)(trafficTime*4)%2)==0;float xx=heavy?front-10:0,z=heavy?25:h+1;
    Color amber=Color.FromArgb(245,187,72);VB(c,Ink,xx-2,-5,z,xx+2,5,z+1);
    VB(c,flash?(Vehicles.Amber(c.Kind)?amber:red):Color.FromArgb(102,80,67),xx-1.5f,-4,z+1,xx+1.5f,-1,z+3);
    VB(c,!flash?(Vehicles.Amber(c.Kind)?amber:blue):Color.FromArgb(58,84,96),xx-1.5f,1,z+1,xx+1.5f,4,z+3);
   }
   foreach(float yy in new float[]{-d+1,d-3}){VB(c,Cream,front-.3f,yy,6,front+.3f,yy+2,9);VB(c,red,back-.3f,yy,6,back+.3f,yy+2,8);}
  }
 }
}
