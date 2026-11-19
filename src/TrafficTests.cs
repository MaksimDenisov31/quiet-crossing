using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace QuietCrossing {
 static partial class Tests {
  internal static void AdvanceSignal(Game game,Street road,double phase){double at=(game.Time+road.Index*7)%TrafficSignals.Cycle;double delta=(phase-at+TrafficSignals.Cycle)%TrafficSignals.Cycle;if(delta>.0001)game.Update((float)delta,0,0);}
  static Game SignalGame(Street road,double phase){var g=new Game(1,123);g.X=road.CrossX;g.Y=road.Bottom+30;g.Update(.001f,0,0);AdvanceSignal(g,road,phase);g.Cars.Clear();return g;}
  static void TrafficChecks(StringBuilder log){
   int controlled=0;bool consistent=true;var phases=new HashSet<SignalPhase>();
   foreach(Street road in CityMap.Streets)if(road.HasLights){controlled++;for(double t=0;t<TrafficSignals.Cycle;t+=.125){SignalState s=TrafficSignals.At(road,t);phases.Add(s.Phase);if(s.PedestrianGreen&&s.Traffic!=SignalColor.Red)consistent=false;}}
   Check(controlled==18&&controlled*20==CityMap.Streets.Length,"exactly 5 percent of crossings have signals (18 of 360)",log);
   Check(consistent&&phases.Count==7,"all seven signal phases are covered and walking green never conflicts with traffic",log);
   Street signal=Array.Find(CityMap.Streets,r=>r.HasLights);
   foreach(int direction in new int[]{1,-1})foreach(double phase in new double[]{15,28,44.1}){
    Game g=SignalGame(signal,phase);float stop=direction>0?signal.CrosswalkLeft-10:signal.CrosswalkRight+10;
    var c=new Car(stop-direction*(38+15),signal.Top+16,direction*65,VehicleKind.SemiTruck,0){Road=signal};g.Cars.Add(c);g.Update(phase>40?.6f:.7f,0,0);
    Check(direction*(c.X+direction*c.Width/2-stop)<=.01f,"long vehicle stops before line: direction="+direction+" phase="+phase,log);
    float before=c.X;AdvanceSignal(g,signal,5);Check(direction*(c.X-before)>20,"traffic resumes on green: direction="+direction+" phase="+phase,log);
   }
   Game pedestrian=SignalGame(signal,19);pedestrian.Y=signal.Bottom+Game.FootDepth;float curb=pedestrian.Y;pedestrian.Update(.1f,0,-1);
   Check(pedestrian.Result==Outcome.Playing&&pedestrian.Y==curb,"red pedestrian signal holds at curb without ending attempt",log);
   AdvanceSignal(pedestrian,signal,28);pedestrian.Cars.Clear();pedestrian.Update(.2f,0,-1);Check(pedestrian.Y<curb&&pedestrian.Result==Outcome.Playing,"pedestrian can start on green",log);
   pedestrian.Y=signal.Top+8;AdvanceSignal(pedestrian,signal,42);pedestrian.Cars.Clear();pedestrian.Update(.3f,0,-1);Check(pedestrian.Y<signal.Top&&pedestrian.Result==Outcome.Playing,"pedestrian already on crossing can finish after signal turns red",log);
   Game activation=new Game(1,123);AdvanceSignal(activation,signal,28);activation.Cars.Clear();activation.X=signal.CrossX;activation.Y=signal.Bottom+30;activation.Update(.001f,0,0);
   Check(!activation.Cars.Exists(c=>c.Road==signal&&c.X+c.Width/2>signal.CrosswalkLeft-9&&c.X-c.Width/2<signal.CrosswalkRight+9),"activating a crossing on pedestrian green never spawns a vehicle over zebra",log);
   foreach(int direction in new int[]{1,-1}){
    Game g=new Game(1,92);g.Cars.Clear();Street road=CityMap.Streets[0];float y=road.Top+16;
    var lead=new Car(direction>0?200:440,y,direction*25,VehicleKind.Bicycle,0){Road=road};var rear=new Car(direction>0?100:540,y,direction*85,VehicleKind.BoxTruck,0){Road=road};g.Cars.Add(lead);g.Cars.Add(rear);bool separated=true;
    for(int i=0;i<200;i++){g.Update(.02f,0,0);if(direction*(lead.X-rear.X)<(lead.Width+rear.Width)/2+8.9f)separated=false;}
    Check(separated,"queue follows cyclist without overlap at every frame: direction="+direction,log);
   }
   foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind))){Game g=SignalGame(signal,28);float stop=signal.CrosswalkLeft-10;var c=new Car(stop-Vehicles.Spec(kind).Length/2-20,signal.Top+16,65,kind,0){Road=signal};g.Cars.Add(c);g.Update(.7f,0,0);Check(c.X+c.Width/2<=stop+.01f,"every vehicle obeys red: "+kind,log);}
   Game clearance=SignalGame(signal,16.9);float line=signal.CrosswalkLeft-10,semiX=line-38+1;
   clearance.Cars.Add(new Car(semiX+56,signal.Top+16,16.8f,VehicleKind.Bicycle,0){Road=signal});var slowSemi=new Car(semiX,signal.Top+16,70,VehicleKind.SemiTruck,0){Road=signal,Speed=16.8f};clearance.Cars.Add(slowSemi);
   double firstWalk=0;while(!TrafficSignals.At(signal,firstWalk-signal.Index*7).PedestrianGreen)firstWalk+=.25;AdvanceSignal(clearance,signal,firstWalk+.01);
   Check(slowSemi.X-slowSemi.Width/2>signal.CrosswalkRight+9,"long truck behind slow cyclist clears before first pedestrian green",log);
   foreach(VehicleKind kind in new VehicleKind[]{VehicleKind.Sedan,VehicleKind.SemiTruck}){Street road=CityMap.Streets[0];Game g=CrosswalkCase(road,road.CrossX,road.Top+16);g.Cars.Add(new Car(g.X+35,g.Y,0,kind,0));g.Update(.001f,0,0);Check((g.Loss==LossReason.Car)==(kind==VehicleKind.SemiTruck),"collision uses vehicle length: "+kind,log);}
   foreach(VehicleKind kind in new VehicleKind[]{VehicleKind.Bicycle,VehicleKind.Van}){Street road=CityMap.Streets[0];Game g=CrosswalkCase(road,road.CrossX,road.Top+16);g.Cars.Add(new Car(g.X,g.Y+6,0,kind,0));g.Update(.001f,0,0);Check((g.Loss==LossReason.Car)==(kind==VehicleKind.Van),"collision uses vehicle depth: "+kind,log);}
   var sample=new Game(1,41);sample.Cars.Clear();sample.SpawnMotorcade(CityMap.Streets[0],0,440);
   int limo=0;bool same=true;int id=sample.Cars[0].ConvoyId;float speed=sample.Cars[0].CruiseSpeed;foreach(Car c in sample.Cars){if(c.Kind==VehicleKind.PresidentialLimousine)limo++;if(c.ConvoyId!=id||c.CruiseSpeed!=speed)same=false;}
   Check(sample.Cars.Count==5&&limo==1&&id>0&&same,"presidential convoy has five coordinated vehicles and exactly one limousine",log);
   var random=new Random(123);var kinds=new HashSet<VehicleKind>();for(int i=0;i<10000;i++)kinds.Add(Vehicles.RandomKind(random));Check(kinds.Count==16,"random traffic includes all sixteen normal and service vehicle kinds",log);
   var frames=new HashSet<long>();using(var renderer=new PixelWorld())using(var image=new Bitmap(480,320))using(Graphics graphics=Graphics.FromImage(image)){
    foreach(VehicleKind kind in Enum.GetValues(typeof(VehicleKind))){var g=new Game(1,123);g.Cars.Clear();g.Y=CityMap.Streets[0].Bottom+35;g.Cars.Add(new Car(g.X+10,g.Y-55,60,kind,0));renderer.Render(graphics,g,CameraMode.FirstPerson,0,new Color[]{Color.Beige,Color.Beige,Color.Beige,Color.Beige,Color.Beige},false,false);frames.Add(Digest(image));}
   }
   Check(frames.Count==Enum.GetValues(typeof(VehicleKind)).Length,"all nineteen vehicle models have distinct 3D silhouettes or markings",log);
  }
 }
}
