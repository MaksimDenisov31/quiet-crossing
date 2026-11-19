using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 static class NativeTestInput {
  [System.Runtime.InteropServices.DllImport("user32.dll",EntryPoint="PostMessageW",SetLastError=true)]
  static extern bool PostMessage(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
  internal static void Post(IntPtr handle,int message,int wParam,int lParam){if(!PostMessage(handle,message,new IntPtr(wParam),new IntPtr(lParam)))throw new Exception("Cannot post input to test window");}
 }
 static class Tests {
  internal static void VisitRoads(Game game,int count){
   while(game.CompletedRoads<count){MapPoint p=game.Target;game.X=p.X;game.Y=p.Y;game.Cars.Clear();int before=game.CompletedRoads;game.Update(.001f,0,0);if(game.CompletedRoads==before)throw new Exception("Route setup failed");}
  }
  internal static void WalkRoute(Game game){
   for(int i=1;i<CityMap.Path.Length&&game.Result==Outcome.Playing;i++){
    MapPoint point=CityMap.Path[i];int guard=0;
    while(CityMap.Distance(new MapPoint(game.X,game.Y),point)>.2f&&game.Result==Outcome.Playing){
     float dx=point.X-game.X,dy=point.Y-game.Y,distance=(float)Math.Sqrt(dx*dx+dy*dy);
     game.Cars.Clear();game.Update(Math.Min(1f/60,distance/64),dx/distance,dy/distance);
     if(++guard>4000)throw new Exception("Walking route obstructed at "+i);
    }
   }
  }
  static long Digest(Bitmap image) {
   BitmapData data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
   int[] pixels=new int[image.Width*image.Height];try{Marshal.Copy(data.Scan0,pixels,0,pixels.Length);}finally{image.UnlockBits(data);}
   long hash=17;unchecked{foreach(int pixel in pixels)hash=hash*31+pixel;}return hash;
  }
  static void Check(bool condition,string name,StringBuilder log) {if(!condition)throw new Exception(name);log.AppendLine("PASS "+name);}
  public static int Run() {
   var log=new StringBuilder();string folder=Path.Combine(Path.GetTempPath(),"QuietCrossingTests-"+Guid.NewGuid().ToString("N"));
   try {
    var game=new Game(1,42);game.Update(1,0,0);Check(game.Result==Outcome.Playing&&game.Y==CityMap.Start.Y,"point A is safe",log);
    Check(CityMap.Streets.Length==360&&CityMap.Districts.Length==12,"map has twelve dense districts and 360 streets",log);
    Check(CityMap.MinimumSeconds>=1200,"straight-line vertical lower bound exceeds 20 minutes, including finish tolerance",log);
    Check(CityMap.TotalDistance/Game.MoveSpeed>CityMap.MinimumSeconds,"marked route is longer than the shortcut lower bound",log);
    bool compact=true;for(int i=1;i<CityMap.Streets.Length;i++){Street prev=CityMap.Streets[i-1],next=CityMap.Streets[i];float gap=prev.Top-next.Bottom;if(gap<64||gap>160||Math.Abs(prev.CrossX-next.CrossX)>100)compact=false;}
    Check(compact,"roads have short safe gaps and lateral transfers no longer than 100 units",log);
    foreach(CameraMode mode in new CameraMode[]{CameraMode.FirstPerson,CameraMode.ThirdPerson}){
     MapPoint east=PlayerControls.Movement(mode,(float)Math.PI/2,1,0),south=PlayerControls.Movement(mode,(float)Math.PI,1,0),west=PlayerControls.Movement(mode,-(float)Math.PI/2,1,0);
     Check(Math.Abs(east.X-1)<.001f&&Math.Abs(east.Y)<.001f,"W moves right after a right turn: "+mode,log);
     Check(Math.Abs(south.X)<.001f&&Math.Abs(south.Y-1)<.001f&&Math.Abs(west.X+1)<.001f,"full 360-degree movement: "+mode,log);
     MapPoint back=PlayerControls.Movement(mode,(float)Math.PI/2,-1,0),left=PlayerControls.Movement(mode,(float)Math.PI/2,0,-1);
     Check(Math.Abs(back.X+1)<.001f&&Math.Abs(left.Y+1)<.001f,"backward and strafing follow facing: "+mode,log);
    }
    MapPoint bird=PlayerControls.Movement(CameraMode.Bird,(float)Math.PI/2,1,0);Check(bird.X==0&&bird.Y==-1,"bird's-eye controls remain aligned with the map",log);
    Check(Math.Abs(PlayerControls.Wrap((float)(Math.PI*6+.5))-.5)<.001f,"repeated turns wrap without limiting rotation",log);
    log.AppendLine("INFO shortcut lower bound="+CityMap.MinimumSeconds.ToString("0.00")+" s; marked route="+(CityMap.TotalDistance/Game.MoveSpeed).ToString("0.00")+" s");
    foreach(Street street in CityMap.Streets){
     game=new Game(1,42);game.Cars.Clear();game.X=street.CrossX;game.Y=street.Top+16;game.Cars.Add(new Car(game.X,game.Y,0,29,0));game.Update(.02f,0,0);
     Check(game.Result==Outcome.Lost,"collision on "+street.Name,log);
    }
    float time=game.Time;game.Update(1,0,-1);Check(game.Time==time,"finished game is frozen",log);
    game=new Game(1,1);game.Cars.Clear();game.Y=CityMap.Streets[0].Top+16;game.Cars.Add(new Car(100,game.Y,3000,29,0));game.Update(.1f,0,0);Check(game.Result==Outcome.Lost,"fast car swept collision",log);
    game=new Game(1,7);VisitRoads(game,1);Check(game.CompletedRoads==1&&game.Result==Outcome.Playing,"first crossing does not end the map",log);
    game.Update(.01f,0,0);Check(game.CompletedRoads==1,"one checkpoint cannot be counted twice",log);
    game=new Game(1,7);game.Cars.Clear();game.X=CityMap.Finish.X;game.Y=CityMap.Finish.Y;game.Update(.01f,0,0);Check(game.Result==Outcome.Playing&&game.CompletedRoads==0,"point B cannot bypass the route",log);
    game.X=CityMap.Streets[4].Exit.X;game.Y=CityMap.Streets[4].Exit.Y;game.Update(.01f,0,0);Check(game.CompletedRoads==0,"checkpoints require route order",log);
    game=new Game(1,8);VisitRoads(game,CityMap.Streets.Length);Check(game.CompletedRoads==CityMap.Streets.Length&&game.Result==Outcome.Playing,"last crossing still requires reaching point B",log);
    game.Cars.Clear();game.X=CityMap.Finish.X;game.Y=CityMap.Finish.Y;game.Update(.01f,0,0);Check(game.Result==Outcome.Won&&game.Progress==1,"all crossings and point B win",log);
    game=new Game(1,9);Check(game.CompletedRoads==0&&game.Time==0&&game.X==CityMap.Start.X&&game.Y==CityMap.Start.Y,"new attempt resets the whole route",log);
    game.Update(.5f,0,-1);Check(Math.Abs(game.Y-(CityMap.Start.Y-32))<.01f,"movement speed is time based",log);
    var diagonal=new Game(1,9);diagonal.Update(.5f,1,-1);Check(Math.Abs((CityMap.Start.Y-diagonal.Y)*(float)Math.Sqrt(2)-32)<.01f,"diagonal movement normalized",log);
    game=new Game(1,9);game.X=14;game.Y=28;game.Update(.5f,-1,-1);Check(game.X>=12&&game.Y>=24,"map boundaries contain player",log);
    game=new Game(1,9);game.X=40;game.Y=CityMap.Houses[0].Y+20;game.Update(.5f,1,0);Check(game.X<=CityMap.Houses[0].X-4,"buildings block walking through walls",log);
    game=new Game(2,123);for(int i=0;i<3000;i++)game.Update(.02f,0,0);
    Check(game.Cars.Count>0&&game.Cars.Count<600,"nearby streets maintain bounded traffic over 60 seconds",log);
    foreach(Street street in CityMap.Streets){game=new Game(1,43);game.X=street.CrossX;game.Y=street.Bottom+25;game.Update(.01f,0,0);Check(game.Cars.Exists(c=>c.Y>street.Top&&c.Y<street.Bottom&&c.Speed>0)&&game.Cars.Exists(c=>c.Y>street.Top&&c.Y<street.Bottom&&c.Speed<0),"traffic activates in both directions: "+street.Name,log);}
    game=new Game(1,44);for(int i=0;i<200;i++){game.X=220;game.Y=CityMap.Streets[30].Top+16+(i%2==0?899:901);game.Update(.01f,0,0);}
    Check(game.Cars.Count<600,"crossing activation boundary repeatedly cannot duplicate traffic",log);
    var other=new Game(2,456);Check(other.Cars[0].X!=new Game(2,123).Cars[0].X,"new seeds change traffic",log);
    var easy=new Game(0,42);var hard=new Game(2,42);Check(Math.Abs(hard.Cars[0].Speed)>Math.Abs(easy.Cars[0].Speed),"difficulty changes speed",log);
    game=new Game(1,12);WalkRoute(game);Check(game.Result==Outcome.Won&&game.CompletedRoads==CityMap.Streets.Length&&game.Time>=1200,"entire marked walking route is traversable without teleporting",log);
    log.AppendLine("INFO full-speed route, traffic removed: "+Game.Duration(game.Time));
    Directory.CreateDirectory(folder);string path=Path.Combine(folder,"settings.xml");
    var settings=new Settings{Sound=false,Difficulty=2,Width=1111,Height=777,Wins=3,Maximized=true};Check(settings.Save(path),"settings initial save",log);
    settings.Wins=4;Check(settings.Save(path),"settings atomic replacement",log);
    settings=Settings.Load(path);Check(!settings.Sound&&settings.Difficulty==2&&settings.Width==1111&&settings.Height==777&&settings.Wins==4&&settings.Maximized,"settings round trip",log);
    string legacy=File.ReadAllText(path).Replace("  <Camera>Bird</Camera>","");File.WriteAllText(path,legacy);
    settings=Settings.Load(path);Check(settings.Camera==CameraMode.Bird&&!settings.Sound&&settings.Wins==4&&settings.Width==1111,"v1.0 settings migrate without resetting user data",log);
    foreach(CameraMode camera in Enum.GetValues(typeof(CameraMode))) {
     settings.Camera=camera;settings.Save(path);Settings restored=Settings.Load(path);
     Check(restored.Camera==camera&&restored.Wins==4&&restored.Width==1111,"saved camera restored: "+camera,log);
    }
    using(var renderer=new PixelWorld())using(var image=new Bitmap(480,320))using(Graphics g=Graphics.FromImage(image)) {
     Color[] colors={Color.Coral,Color.Khaki,Color.LightBlue,Color.Thistle,Color.Beige};game=new Game(1,1234);
     renderer.Render(g,game,CameraMode.FirstPerson,0,colors,false,false);long first=Digest(image);
     renderer.Render(g,game,CameraMode.ThirdPerson,0,colors,false,false);Check(Digest(image)!=first,"first and third person have distinct projections",log);
     renderer.Render(g,game,CameraMode.FirstPerson,-1.25f,colors,false,false);Check(Digest(image)!=first,"looking sideways changes the perspective",log);
     game.Y=CityMap.Start.Y-400;renderer.Render(g,game,CameraMode.FirstPerson,0,colors,true,false);Check(Digest(image)!=first,"camera follows movement across road",log);
     game.Y=CityMap.Start.Y;renderer.Render(g,game,CameraMode.FirstPerson,0,colors,false,false);Check(Digest(image)==first,"rendering is deterministic and does not advance simulation",log);
     game.Cars.Clear();game.Cars.Add(new Car(game.X,game.Y-1,60,40,0));
     foreach(CameraMode mode in new CameraMode[]{CameraMode.FirstPerson,CameraMode.ThirdPerson})
      foreach(float y in new float[]{CityMap.Start.Y,CityMap.Streets[0].Top,CityMap.Streets[120].Top,CityMap.Streets[240].Top,CityMap.Streets[359].Top,CityMap.Finish.Y})foreach(float angle in new float[]{-(float)Math.PI,-1.25f,0,1.25f,(float)Math.PI/2,(float)(Math.PI*1.5)}) {
       game.Y=y;renderer.Render(g,game,mode,angle,colors,true,false);
      }
     Check(game.Time==0&&game.Result==Outcome.Playing,"near-plane clipping across crossing never changes game state",log);
    }
    File.WriteAllText(path,"broken");Check(Settings.Load(path).Difficulty==1,"damaged settings recover defaults",log);
    Check(Settings.Load(Path.Combine(folder,"absent.xml")).Sound,"missing settings use defaults",log);
    File.Delete(path);Directory.Delete(folder);File.WriteAllText("test-results.txt",log.ToString());return 0;
   }catch(Exception ex){File.WriteAllText("test-results.txt",log+"FAIL "+ex);return 1;}
  }
 }
}
