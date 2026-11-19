using System;
using System.Drawing;

namespace QuietCrossing {
 sealed partial class PixelWorld {
  void DrawCity(Game game,Color[] colors){
   Ground(-1800,-1600,2400,3000,-.05f,Grass);
   for(int i=1;i<CityMap.Path.Length;i++){
    MapPoint a=CityMap.Path[i-1],b=CityMap.Path[i];
    if(a.Y==b.Y)Ground(Math.Min(a.X,b.X)-15,a.Y-15,Math.Max(a.X,b.X)+15,a.Y+15,.025f,Color.FromArgb(199,195,160));
    else {
     float cursor=Math.Min(a.Y,b.Y)-15,end=Math.Max(a.Y,b.Y)+15;
     for(int r=CityMap.Streets.Length-1;r>=0;r--){
      Street road=CityMap.Streets[r];
      if(road.Bottom<cursor||road.Top>end)continue;
      if(cursor<road.Top)Ground(a.X-15,cursor,a.X+15,Math.Min(road.Top,end),.025f,Color.FromArgb(199,195,160));
      cursor=Math.Max(cursor,road.Bottom);
     }
     if(cursor<end)Ground(a.X-15,cursor,a.X+15,end,.025f,Color.FromArgb(199,195,160));
    }
   }
   foreach(Street street in CityMap.Streets){
    float top=street.Top,bottom=street.Bottom,x=street.CrossX;
    Ground(-1600,top,1800,bottom,0,Road);
    Ground(-1600,top-20,1800,top,.05f,Color.FromArgb(194,197,171));
    Ground(-1600,bottom,1800,bottom+20,.05f,Color.FromArgb(194,197,171));
    Ground(-1600,top-3,1800,top,.1f,Cream);Ground(-1600,bottom,1800,bottom+3,.1f,Cream);
    for(int lane=1;lane<street.Lanes;lane++)for(int xx=-400;xx<1040;xx+=29)if(xx<x-36||xx>x+30)
     Ground(xx,top+lane*32,xx+15,top+lane*32+1,.12f,lane==street.Lanes/2?Color.FromArgb(203,181,119):Color.FromArgb(122,135,130));
    for(float y=top+5;y<bottom-4;y+=16)Ground(x-24,y,x+24,y+9,.15f,Cream);
    Ground(x-22,top-12,x+22,top-6,.15f,Color.FromArgb(216,196,128));Ground(x-22,bottom+8,x+22,bottom+14,.15f,Color.FromArgb(216,196,128));
    foreach(float xx in new float[]{x-43,x+42}){
     Box(xx,top-10,0,xx+2,top-8,30,Ink);Box(xx-6,top-11,23,xx+8,top-9,36,Color.FromArgb(102,151,165));
     Box(xx-3,top-8.9f,26,xx+5,top-8.7f,33,Cream);Box(xx,top-8.6f,27,xx+2,top-8.4f,32,Ink);
    }
   }
   for(int i=1;i<CityMap.Path.Length;i++){
    MapPoint a=CityMap.Path[i-1],b=CityMap.Path[i];float distance=CityMap.Distance(a,b);
    for(float t=0;t<distance;t+=14){float x=a.X+(b.X-a.X)*t/distance,y=a.Y+(b.Y-a.Y)*t/distance;if(!CityMap.OnRoad(y))Ground(x-1,y-1,x+2,y+2,.16f,Cream);}
   }
   for(int i=0;i<CityMap.Houses.Length;i++){
    House h=CityMap.Houses[i];if(Math.Abs(h.Y-cameraY)>750)continue;
    Color wall=i%2==0?Color.FromArgb(194,192,162):Color.FromArgb(171,187,161);
    Box(h.X,h.Y,0,h.X+h.Width,h.Y+h.Depth,h.Height,wall);
    Box(h.X-2,h.Y-2,h.Height,h.X+h.Width+2,h.Y+h.Depth+2,h.Height+5,Color.FromArgb(112,136,119));
    for(int j=0;j<3;j++)Box(h.X+8+j*18,h.Y+h.Depth+.1f,17,h.X+16+j*18,h.Y+h.Depth+.4f,28,Color.FromArgb(108,145,147));
   }
   foreach(MapPoint tree in CityMap.Trees)if(Math.Abs(tree.Y-cameraY)<650)Tree(tree.X,tree.Y);
   // A few trees outside the walking boundary make the street edges feel continuous.
   for(int i=0;i<10;i++){float y=100+i*140;if(Math.Abs(y-cameraY)<650){Tree(-55,y);Tree(710,y+30);}}
   for(int i=0;i<CityMap.Streets.Length;i++){
    MapPoint p=CityMap.Streets[i].Exit;Color c=i<game.CompletedRoads?Color.FromArgb(150,189,128):i==game.CompletedRoads?Color.FromArgb(246,201,104):Cream;
    Ground(p.X-13,p.Y-11,p.X+13,p.Y+11,.2f,c);
    // A low guidepost on the far curb stays visible from the eye-level camera.
    Box(p.X+28,p.Y,0,p.X+30,p.Y+2,23,Ink);Box(p.X+23,p.Y,17,p.X+35,p.Y+2,29,c);
   }
   foreach(MapPoint p in new MapPoint[]{CityMap.Start,CityMap.Finish})Ground(p.X-18,p.Y-17,p.X+18,p.Y+17,.18f,Cream);
   foreach(float x in new float[]{CityMap.Finish.X-22,CityMap.Finish.X+22}){
    float y=CityMap.Finish.Y-10;Box(x,y,0,x+1.5f,y+2,29,Ink);Box(x+1.5f,y,20,x+13,y+1,29,Color.FromArgb(196,223,172));
    for(int i=0;i<3;i++)for(int j=0;j<2;j++)if((i+j)%2==0)Box(x+2+i*3.5f,y+1.1f,21+j*3.5f,x+5.5f+i*3.5f,y+1.3f,24.5f+j*3.5f,Ink);
   }
   foreach(Car car in game.Cars)if(Math.Abs(car.Y-cameraY)<650)DrawCar(car,colors[car.Color]);
  }
 }
}
