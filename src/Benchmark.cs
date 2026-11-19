using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace QuietCrossing {
 static partial class Tests {
  public static int Benchmark(){
   var log=new StringBuilder();
   using(var target=new Bitmap(480,320))using(Graphics g=Graphics.FromImage(target))using(var renderer=new PixelWorld()){
    Color[] colors={Color.Coral,Color.Khaki,Color.CadetBlue,Color.Plum,Color.Beige};
    foreach(CameraMode mode in new[]{CameraMode.FirstPerson,CameraMode.ThirdPerson}){
     var game=new Game(2,125);VisitRoads(game,180);game.Y=CityMap.Streets[180].Bottom+30;
     for(int i=0;i<5;i++)renderer.Render(g,game,mode,.3f,colors,false,false);
     var clock=Stopwatch.StartNew();const int frames=90;
     for(int i=0;i<frames;i++)renderer.Render(g,game,mode,.3f,colors,false,false);
     log.AppendLine(mode+" renderer ms/frame="+(clock.Elapsed.TotalMilliseconds/frames).ToString("0.00"));
     clock.Restart();for(int i=0;i<600;i++){game.Cars.Clear();game.Update(1f/60,0,0);}
     log.AppendLine("simulation ms/update="+(clock.Elapsed.TotalMilliseconds/600).ToString("0.000"));
    }
   }
   File.WriteAllText("benchmark-results.txt",log.ToString());return 0;
  }
 }
}
