using System;

namespace QuietCrossing {
 public sealed partial class Game {
  int nextConvoyId;
  static readonly VehicleKind[] convoy={VehicleKind.MotorcadeEscort,VehicleKind.SecuritySuv,VehicleKind.PresidentialLimousine,VehicleKind.SecuritySuv,VehicleKind.MotorcadeEscort};
  bool Spawn(TrafficLane lane,float x,VehicleKind kind,int convoyId){
   var car=new Car(x,lane.Y,lane.Speed*(convoyId==0?Vehicles.Spec(kind).SpeedFactor:1),kind,random.Next(5)){Road=lane.Road,ConvoyId=convoyId};
   if(lane.Road.HasLights&&TrafficSignals.At(lane.Road,elapsed).Traffic!=SignalColor.Green){
    float stop=car.Direction>0?lane.Road.CrosswalkLeft-10:lane.Road.CrosswalkRight+10;
    bool inCrossing=car.X+car.Width/2>lane.Road.CrosswalkLeft-10&&car.X-car.Width/2<lane.Road.CrosswalkRight+10;
    if(inCrossing){car.X=stop-car.Direction*car.Width/2;car.Speed=0;}
   }
   foreach(Car other in Cars)if(other.Y==lane.Y&&Math.Abs(other.X-car.X)<(other.Width+car.Width)/2+12)return false;
   Cars.Add(car);return true;
  }
  void Activate(TrafficLane lane){
   lane.Active=true;lane.ConvoyStep=-1;
   if(random.Next(120)==0&&(!lane.Road.HasLights||TrafficSignals.At(lane.Road,elapsed).Traffic==SignalColor.Green)){SpawnMotorcade(lane,lane.Speed>0?440:200);lane.Next=5;return;}
   float initial=20+(float)random.NextDouble()*210;
   for(int i=0;i<3;i++)Spawn(lane,initial+i*280,Vehicles.RandomKind(random),0);
  }
  void SpawnMotorcade(TrafficLane lane,float leadX){int id=++nextConvoyId,dir=lane.Speed>0?1:-1;for(int i=0;i<convoy.Length;i++)Spawn(lane,leadX-dir*i*90,convoy[i],id);}
  internal void SpawnMotorcade(Street road,int laneIndex,float leadX){TrafficLane lane=lanes.Find(item=>item.Y==road.Top+16+laneIndex*32);if(lane==null)throw new ArgumentOutOfRangeException("laneIndex");SpawnMotorcade(lane,leadX);}
  void SpawnNext(TrafficLane lane){
   if(lane.ConvoyStep<0&&random.Next(400)==0){lane.ConvoyId=++nextConvoyId;lane.ConvoyStep=0;}
   VehicleKind kind=lane.ConvoyStep>=0?convoy[lane.ConvoyStep]:Vehicles.RandomKind(random);
   if(!Spawn(lane,lane.Speed>0?-48:CityMap.Width+48,kind,lane.ConvoyStep>=0?lane.ConvoyId:0)){lane.Next=.35f;return;}
   if(lane.ConvoyStep>=0){lane.ConvoyStep++;if(lane.ConvoyStep>=convoy.Length)lane.ConvoyStep=-1;lane.Next=1.35f;}
   else lane.Next=(1.6f+(float)random.NextDouble()*1.9f)/(1+Difficulty*.24f)/lane.Pace;
  }
  bool CanEnterSignalRoad(double from,double to){
   foreach(Street road in CityMap.Streets)if(road.HasLights){
    bool wasOn=from>road.Top&&from-FootDepth<road.Bottom,on=to>road.Top&&to-FootDepth<road.Bottom;
    if(!wasOn&&on&&!TrafficSignals.At(road,elapsed).PedestrianGreen)return false;
   }
   return true;
  }
  static int FrontFirst(Car a,Car b){int result=a.Y.CompareTo(b.Y);if(result!=0)return result;result=a.Direction.CompareTo(b.Direction);if(result!=0)return result;return a.Direction>0?b.X.CompareTo(a.X):a.X.CompareTo(b.X);}
  void AdvanceTraffic(float dt){
   Cars.Sort(FrontFirst);Car leader=null;
   foreach(Car car in Cars){
    int dir=car.Direction;float limit=float.PositiveInfinity;
    if(leader!=null&&leader.Y==car.Y&&leader.Direction==dir)
     limit=Math.Max(0,dir*(leader.X-car.X)-(leader.Width+car.Width)/2-9);
    if(car.Road==null)foreach(Street road in CityMap.Streets)if(car.Y>=road.Top&&car.Y<=road.Bottom){car.Road=road;break;}
    if(car.Road!=null&&car.Road.HasLights&&TrafficSignals.At(car.Road,elapsed).Traffic!=SignalColor.Green){
     float stop=dir>0?car.Road.CrosswalkLeft-10:car.Road.CrosswalkRight+10;
     float gap=dir*(stop-car.X)-car.Width/2;
     // Cars already past the stop line clear the crossing instead of freezing on it.
     if(gap>=-.01f)limit=Math.Min(limit,Math.Max(0,gap));
    }
    float desired=Math.Abs(car.CruiseSpeed);if(!float.IsPositiveInfinity(limit))desired=Math.Min(desired,(float)Math.Sqrt(2*180*limit));
    float speed=Math.Abs(car.Speed);speed=speed<desired?Math.Min(desired,speed+90*dt):Math.Max(desired,speed-180*dt);
    float distance=Math.Min(speed*dt,limit);car.X+=dir*distance;if(dt>0)car.Speed=dir*distance/dt;
    leader=car;
   }
  }
 }
}
