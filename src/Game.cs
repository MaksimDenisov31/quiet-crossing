using System;
using System.Collections.Generic;

namespace QuietCrossing {
 public enum Outcome { Playing, Lost, Won }
 public enum LossReason { None, Car, OutsideCrosswalk }
 public sealed class Car {
  public float X,Y,Speed,Width;
  public int Color;
  public Car(float x,float y,float speed,float width,int color){X=x;Y=y;Speed=speed;Width=width;Color=color;}
 }
 sealed class TrafficLane {public float Y,Speed,Next,Pace;public bool Active;}
 public sealed class Game {
  public const float MoveSpeed=64;
  public const float BodyHalfWidth=7,FootDepth=6;
  double positionX=CityMap.Start.X,positionY=CityMap.Start.Y,elapsed;
  readonly Random random;
  readonly List<TrafficLane> lanes=new List<TrafficLane>();
  public readonly List<Car> Cars=new List<Car>();
  public float X{get{return (float)positionX;}set{positionX=value;}}
  public float Y{get{return (float)positionY;}set{positionY=value;}}
  public float Time{get{return (float)elapsed;}}
  public int CompletedRoads {get;private set;}
  public Outcome Result=Outcome.Playing;
  public LossReason Loss{get;private set;}
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
    lanes.Add(lane);if(Math.Abs(lane.Y-Y)<900)Activate(lane);
   }
  }
  void Spawn(TrafficLane lane,float x){Cars.Add(new Car(x,lane.Y,lane.Speed,random.Next(4)==0?40:29,random.Next(5)));}
  void Activate(TrafficLane lane){lane.Active=true;float initial=20+(float)random.NextDouble()*280;Spawn(lane,initial);Spawn(lane,initial+420);Spawn(lane,initial+840);}
  public static string Duration(double seconds){int value=(int)seconds;return (value/60).ToString("00")+":"+(value%60).ToString("00");}
  public void Update(float dt,float dx,float dy) {
   if(Result!=Outcome.Playing)return;
   int steps=Math.Max(1,(int)Math.Ceiling(dt*120));float step=dt/steps;
   float length=(float)Math.Sqrt(dx*dx+dy*dy);if(length>1){dx/=length;dy/=length;}
   for(int s=0;s<steps&&Result==Outcome.Playing;s++) {
    elapsed+=step;
    double nx=Math.Max(12,Math.Min(CityMap.Width-12,positionX+dx*MoveSpeed*(double)step));if(CityMap.CanStand((float)nx,Y))positionX=nx;
    double ny=Math.Max(24,Math.Min(CityMap.Height-12,positionY+dy*MoveSpeed*(double)step));if(CityMap.CanStand(X,(float)ny))positionY=ny;
    foreach(TrafficLane lane in lanes) {
     if(Math.Abs(lane.Y-Y)>900){if(lane.Active){float oldY=lane.Y;Cars.RemoveAll(c=>c.Y==oldY);lane.Active=false;}continue;}
     if(!lane.Active)Activate(lane);
     lane.Next-=step;
     if(lane.Next<=0){Spawn(lane,lane.Speed>0?-48:CityMap.Width+48);lane.Next=(1.6f+(float)random.NextDouble()*1.9f)/(1+Difficulty*.24f)/lane.Pace;}
    }
    foreach(Car car in Cars) {
     car.X+=car.Speed*step;
     if(X+4>car.X-car.Width/2&&X-4<car.X+car.Width/2&&Y>car.Y-7&&Y-6<car.Y+7){Result=Outcome.Lost;Loss=LossReason.Car;}
    }
    Cars.RemoveAll(c=>c.X < -90||c.X>CityMap.Width+90||Math.Abs(c.Y-Y)>1000);
    // Sidewalk exits are legal. While feet overlap the roadway, the whole
    // character must leave the zebra's side before the attempt is rejected.
    // Contact with the boundary remains allowed; stripe gaps belong to the zebra.
    if(Result!=Outcome.Won)foreach(Street street in CityMap.Streets){
     if(positionY>street.Top&&positionY-FootDepth<street.Bottom&&
       (positionX+BodyHalfWidth<street.CrosswalkLeft||positionX-BodyHalfWidth>street.CrosswalkRight)){
      Result=Outcome.Lost;Loss=LossReason.OutsideCrosswalk;break;
     }
    }
    // Only the far safe curb counts. The entire route must be walked in order.
    if(Result==Outcome.Playing&&CompletedRoads<CityMap.Streets.Length) {
     MapPoint exit=Target;if(Math.Abs(X-exit.X)<=30&&Math.Abs(Y-exit.Y)<=21)CompletedRoads++;
    }
    if(Result==Outcome.Playing&&CompletedRoads==CityMap.Streets.Length&&CityMap.Distance(new MapPoint(X,Y),CityMap.Finish)<=20)Result=Outcome.Won;
   }
  }
 }
}
