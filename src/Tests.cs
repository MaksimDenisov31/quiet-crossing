using System;
using System.IO;
using System.Text;

namespace QuietCrossing {
 static class Tests {
  static void Check(bool condition,string name,StringBuilder log) {if(!condition)throw new Exception(name);log.AppendLine("PASS "+name);}
  public static int Run() {
   var log=new StringBuilder();string folder=Path.Combine(Path.GetTempPath(),"QuietCrossingTests-"+Guid.NewGuid().ToString("N"));
   try {
    var game=new Game(1,42);game.Update(1,0,0);Check(game.Result==Outcome.Playing && game.Y==275,"starting pavement is safe",log);
    game.Cars.Clear();game.Cars.Add(new Car(240,116,0,29,0));game.Y=116;game.Update(.02f,0,0);Check(game.Result==Outcome.Lost,"car collision ends attempt",log);
    float time=game.Time;game.Update(1,0,-1);Check(game.Time==time,"finished game is frozen",log);
    game=new Game(1,1);game.Cars.Clear();game.Cars.Add(new Car(100,116,3000,29,0));game.Y=116;game.Update(.1f,0,0);Check(game.Result==Outcome.Lost,"fast car swept collision",log);
    game=new Game(1,7);game.Cars.Clear();game.Y=78;game.Update(.05f,0,-1);Check(game.Result==Outcome.Won,"opposite pavement wins",log);
    game=new Game(1,8);for(int i=0;i<100;i++)game.Update(.02f,1,1);Check(game.X<=275&&game.Y<=281,"movement stays on crossing approach",log);
    game=new Game(1,9);game.Update(.5f,0,-1);Check(Math.Abs(game.Y-243)<.01f,"movement speed is time based",log);
    var diagonal=new Game(1,9);diagonal.Update(.5f,1,-1);Check(Math.Abs((275-diagonal.Y)*(float)Math.Sqrt(2)-32)<.01f,"diagonal movement normalized",log);
    game=new Game(2,123);for(int i=0;i<3000;i++)game.Update(.02f,0,0);
    Check(game.Cars.Count>0&&game.Cars.Count<60,"random traffic remains bounded over 60 seconds",log);
    Check(game.Cars.Exists(c=>c.Speed>0)&&game.Cars.Exists(c=>c.Speed<0),"both traffic directions",log);
    var other=new Game(2,456);Check(other.Cars[0].X!=new Game(2,123).Cars[0].X,"new seeds change traffic",log);
    var easy=new Game(0,42);var hard=new Game(2,42);Check(Math.Abs(hard.Cars[0].Speed)>Math.Abs(easy.Cars[0].Speed),"difficulty changes speed",log);
    Directory.CreateDirectory(folder);string path=Path.Combine(folder,"settings.xml");
    var settings=new Settings{Sound=false,Difficulty=2,Width=1111,Height=777,Wins=3,Maximized=true};Check(settings.Save(path),"settings initial save",log);
    settings.Wins=4;Check(settings.Save(path),"settings atomic replacement",log);
    settings=Settings.Load(path);Check(!settings.Sound&&settings.Difficulty==2&&settings.Width==1111&&settings.Height==777&&settings.Wins==4&&settings.Maximized,"settings round trip",log);
    File.WriteAllText(path,"broken");Check(Settings.Load(path).Difficulty==1,"damaged settings recover defaults",log);
    Check(Settings.Load(Path.Combine(folder,"absent.xml")).Sound,"missing settings use defaults",log);
    File.Delete(path);Directory.Delete(folder);File.WriteAllText("test-results.txt",log.ToString());return 0;
   }catch(Exception ex){File.WriteAllText("test-results.txt",log+"FAIL "+ex);return 1;}
  }
 }
}
