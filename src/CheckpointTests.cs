using System;
using System.Text;

namespace QuietCrossing {
 static partial class Tests {
  static void CheckpointChecks(StringBuilder log){
   var game=new Game(2,87);game.Update(.01f,0,0);float elapsed=game.Time;
   game.Respawn();Check(game.X==CityMap.Start.X&&game.Y==CityMap.Start.Y&&game.Time==elapsed,"before first checkpoint respawn uses A and preserves elapsed time",log);
   foreach(int count in new[]{1,2,30,180,360}){
    VisitRoads(game,count);MapPoint checkpoint=CityMap.Streets[count-1].Exit;
    Check(game.Checkpoint.X==checkpoint.X&&game.Checkpoint.Y==checkpoint.Y,"ordered checkpoint saved: "+count,log);
    Street road=CityMap.Streets[Math.Min(count,359)];
    game.X=road.CrosswalkRight+Game.BodyHalfWidth+1;game.Y=road.Top+16;game.Cars.Clear();game.Update(.001f,0,0);
    Check(game.Loss==LossReason.OutsideCrosswalk,"zebra violation after checkpoint "+count,log);
    elapsed=game.Time;int cars=game.Cars.Count;game.Respawn();
    Check(game.CompletedRoads==count&&game.Time==elapsed&&game.X==checkpoint.X&&game.Y==checkpoint.Y&&game.Result==Outcome.Playing&&game.Loss==LossReason.None&&game.Cars.Count==cars,"zebra respawn preserves progress, clock and traffic: "+count,log);
    game.X=road.CrossX;game.Y=road.Top+16;game.Cars.Clear();game.Cars.Add(new Car(game.X,game.Y,0,29,0));game.Update(.001f,0,0);
    Check(game.Loss==LossReason.Car,"collision after checkpoint "+count,log);game.Respawn();
    Check(game.CompletedRoads==count&&game.X==checkpoint.X&&game.Y==checkpoint.Y,"collision retry uses saved checkpoint: "+count,log);
   }
  }
 }
}
