using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace QuietCrossing {
 public sealed partial class Window {
  void SetSignalPreview(string name){
   game=new Game(1,1234);Street road=CityMap.Streets[9];Tests.VisitRoads(game,9);game.X=road.CrossX;game.Y=road.Bottom+28;
   Tests.AdvanceSignal(game,road,name=="signal-red"?19:name=="signal-yellow"?15:28);
  }
  void SetTrafficPreview(){
   game=new Game(1,1234);Tests.VisitRoads(game,3);Street road=CityMap.Streets[3];game.X=road.CrossX;game.Y=road.Bottom+30;game.Cars.Clear();
   foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind))){int i=(int)kind,lane=i%5;var c=new Car(70+i/5*145,road.Top+16+lane*32,lane<3?60:-60,kind,i%5){Road=road};game.Cars.Add(c);}
  }
  void SetMotorcadePreview(){game=new Game(1,1234);game.X=CityMap.Streets[0].CrossX;game.Y=CityMap.Streets[0].Bottom+30;game.Cars.Clear();game.SpawnMotorcade(CityMap.Streets[0],0,440);}
  void Capture3DVehicleGallery(){
   int count=Enum.GetValues(typeof(VehicleKind)).Length,rows=(count+3)/4;
   using(var sample=new Bitmap(480,320))using(Graphics view=Graphics.FromImage(sample))using(var sheet=new Bitmap(960,rows*155+35))using(Graphics g=Graphics.FromImage(sheet)){
    g.Clear(ink);TextAt(g,"ТРАНСПОРТ · ВИД ОТ ПЕРВОГО ЛИЦА",12,8,16,cream,true);g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;
    foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind))){
     int i=(int)kind,x=(i%4)*240,y=(i/4)*155+35;var sampleGame=new Game(1,1234);sampleGame.Cars.Clear();sampleGame.Y=CityMap.Streets[0].Bottom+35;
     sampleGame.Cars.Add(new Car(sampleGame.X+10,sampleGame.Y-55,60,kind,i%5));world.Render(view,sampleGame,CameraMode.FirstPerson,0,carColors,false,false);
     g.DrawImage(sample,new Rectangle(x+5,y,230,120),new Rectangle(50,20,400,240),GraphicsUnit.Pixel);TextAt(g,Vehicles.Spec(kind).Name,x+6,y+125,11,cream);
    }
    sheet.Save("docs/traffic-gallery-3d.png",ImageFormat.Png);
   }
  }
 }
}
