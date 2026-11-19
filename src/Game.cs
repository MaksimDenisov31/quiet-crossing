using System;
using System.Collections.Generic;

namespace QuietCrossing {
 public enum Outcome { Playing, Lost, Won }
 public sealed class Car {
  public float X, Y, Speed, Width;
  public int Color;
  public Car(float x,float y,float speed,float width,int color) { X=x;Y=y;Speed=speed;Width=width;Color=color; }
 }
 public sealed class Game {
  readonly Random random;
  readonly float[] next = new float[4];
  public readonly List<Car> Cars = new List<Car>();
  public float X=240,Y=275,Time;
  public Outcome Result=Outcome.Playing;
  public int Difficulty;
  public Game(int difficulty,int seed) {
   Difficulty=difficulty; random=new Random(seed);
   for(int i=0;i<4;i++) {
    next[i]=0.5f+(float)random.NextDouble()*1.6f;
    Spawn(i,60+(float)random.NextDouble()*340);
   }
  }
  void Spawn(int lane,float x) {
   float speed=(42+Difficulty*17+(float)random.NextDouble()*22)*(lane<2?1:-1);
   Cars.Add(new Car(x,116+lane*32,speed,random.Next(0,4)==0?40:29,random.Next(5)));
  }
  public void Update(float dt,float dx,float dy) {
   if(Result!=Outcome.Playing) return;
   // Small simulation steps prevent a fast car from passing through a player.
   int steps=Math.Max(1,(int)Math.Ceiling(dt*120));
   float step=dt/steps;
   for(int s=0;s<steps && Result==Outcome.Playing;s++) {
    Time+=step;
    float length=(float)Math.Sqrt(dx*dx+dy*dy);
    if(length>1) {dx/=length;dy/=length;}
    X=Math.Max(205,Math.Min(275,X+dx*64*step));
    Y=Math.Max(64,Math.Min(281,Y+dy*64*step));
    for(int lane=0;lane<4;lane++) {
     next[lane]-=step;
     if(next[lane]<=0) {
      Spawn(lane,lane<2?-48:528);
      next[lane]=(1.65f+(float)random.NextDouble()*1.85f)/(1+Difficulty*0.24f);
     }
    }
    foreach(Car car in Cars) {
     car.X+=car.Speed*step;
     // The feet represent the character's position on the ground.
     if(X+4>car.X-car.Width/2 && X-4<car.X+car.Width/2 && Y>car.Y-7 && Y-6<car.Y+7)
      Result=Outcome.Lost;
    }
    Cars.RemoveAll(c=>c.X < -90 || c.X>570);
    if(Result==Outcome.Playing && Y<=77) Result=Outcome.Won;
   }
  }
 }
}
