using System;
using System.Collections.Generic;

namespace QuietCrossing {
 public enum Outcome { Playing, Lost, Won }
 public sealed class Car {
  public float X,Y,Speed,Width;
  public int Color;
  public Car(float x,float y,float speed,float width,int color){X=x;Y=y;Speed=speed;Width=width;Color=color;}
 }
 sealed class TrafficLane {public float Y,Speed,Next,Pace;}
 public sealed class Game {
  readonly Random random;
  readonly List<TrafficLane> lanes=new List<TrafficLane>();
  public readonly List<Car> Cars=new List<Car>();
  public float X=CityMap.Start.X,Y=CityMap.Start.Y,Time;
  public int CompletedRoads {get;private set;}
  public Outcome Result=Outcome.Playing;
  public int Difficulty;
  public MapPoint Target {get{return CompletedRoads<CityMap.Streets.Length?CityMap.Streets[CompletedRoads].Exit:CityMap.Finish;}}
  public string TargetName {get{return CompletedRoads<CityMap.Streets.Length?CityMap.Streets[CompletedRoads].Name:"Точка Б · набережная";}}
  public float RemainingDistance {
   get {
    float distance=CityMap.Distance(new MapPoint(X,Y),Target);
    if(CompletedRoads<CityMap.Streets.Length){int index=CompletedRoads*2+1;for(int i=index+1;i<CityMap.Path.Length;i++)distance+=CityMap.Distance(CityMap.Path[i-1],CityMap.Path[i]);}
    return distance;
   }
  }
  public float Progress {get{return Result==Outcome.Won?1:Math.Max(0,Math.Min(1,1-RemainingDistance/CityMap.TotalDistance));}}
  public Game(int difficulty,int seed) {
   Difficulty=difficulty;random=new Random(seed);
   foreach(Street street in CityMap.Streets)for(int i=0;i<street.Lanes;i++) {
    float speed=(42+Difficulty*17+(float)random.NextDouble()*18)*street.Pace*(i<(street.Lanes+1)/2?1:-1);
    var lane=new TrafficLane{Y=street.Top+16+i*32,Speed=speed,Pace=street.Pace,Next=.4f+(float)random.NextDouble()*1.8f};
    lanes.Add(lane);float initial=20+(float)random.NextDouble()*220;Spawn(lane,initial);Spawn(lane,initial+330);
   }
  }
  void Spawn(TrafficLane lane,float x){Cars.Add(new Car(x,lane.Y,lane.Speed,random.Next(4)==0?40:29,random.Next(5)));}
  public void Update(float dt,float dx,float dy) {
   if(Result!=Outcome.Playing)return;
   int steps=Math.Max(1,(int)Math.Ceiling(dt*120));float step=dt/steps;
   float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length>1){dx/=length;dy/=length;}
   for(int s=0;s<steps&&Result==Outcome.Playing;s++) {
    Time+=step;
    float nx=Math.Max(12,Math.Min(CityMap.Width-12,X+dx*64*step));if(CityMap.CanStand(nx,Y))X=nx;
    float ny=Math.Max(24,Math.Min(CityMap.Height-12,Y+dy*64*step));if(CityMap.CanStand(X,ny))Y=ny;
    foreach(TrafficLane lane in lanes) {
     lane.Next-=step;
     if(lane.Next<=0){Spawn(lane,lane.Speed>0?-48:CityMap.Width+48);lane.Next=(1.6f+(float)random.NextDouble()*1.9f)/(1+Difficulty*.24f)/lane.Pace;}
    }
    foreach(Car car in Cars) {
     car.X+=car.Speed*step;
     if(X+4>car.X-car.Width/2&&X-4<car.X+car.Width/2&&Y>car.Y-7&&Y-6<car.Y+7)Result=Outcome.Lost;
    }
    Cars.RemoveAll(c=>c.X < -90||c.X>CityMap.Width+90);
    // Only the far safe curb counts. The entire route must be walked in order.
    if(Result==Outcome.Playing&&CompletedRoads<CityMap.Streets.Length) {
     MapPoint exit=Target;if(Math.Abs(X-exit.X)<=30&&Math.Abs(Y-exit.Y)<=21)CompletedRoads++;
    }
    if(Result==Outcome.Playing&&CompletedRoads==CityMap.Streets.Length&&CityMap.Distance(new MapPoint(X,Y),CityMap.Finish)<=20)Result=Outcome.Won;
   }
  }
 }
}
