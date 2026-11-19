using System;
using System.Text;
using System.Threading;

namespace QuietCrossing {
 static partial class Tests {
  static void FrameChecks(StringBuilder log){
   foreach(int rate in new[]{24,60,75,120,144,165,170,240,360,540}){
    Check(MonitorRefresh.Normalize((uint)rate)==rate,"monitor frequency is used without a 60 FPS ceiling: "+rate,log);
    double next=0;for(int i=0;i<rate*10;i++)next=FramePump.NextDeadline(next,next,rate);
    Check(Math.Abs(next-10)<.00000001,"frame deadlines retain cadence over ten seconds: "+rate,log);
    Check(Math.Abs(FramePump.NextDeadline(1,5,rate)-(5+1.0/rate))<.00000001,"stall discards obsolete frames without a catch-up burst: "+rate,log);
   }
   Check(MonitorRefresh.Normalize(0)==60&&MonitorRefresh.Normalize(1)==60,"driver default/unknown refresh rate uses safe fallback",log);
   int requests=0;
   using(var requested=new AutoResetEvent(false))using(var pump=new FramePump(()=>{Interlocked.Increment(ref requests);requested.Set();return true;})){
    pump.Rate=170;pump.Start();Check(requested.WaitOne(2000),"high-resolution scheduler posts first frame",log);
    Thread.Sleep(30);Check(Volatile.Read(ref requests)==1,"slow UI cannot accumulate pending frame messages",log);
    pump.Presented();Check(requested.WaitOne(2000)&&Volatile.Read(ref requests)==2,"presenting a slow frame releases the next request",log);
    pump.Stop();pump.Presented();Thread.Sleep(30);Check(Volatile.Read(ref requests)==2,"stopped frame scheduler stays idle",log);
    pump.Rate=120;pump.Start();Check(requested.WaitOne(2000)&&Volatile.Read(ref requests)==3,"scheduler resumes with a different monitor rate",log);
   }
   var slow=new Game(1,42);var fast=new Game(1,42);
   for(int i=0;i<24;i++){slow.Cars.Clear();slow.Update(1f/24,0,-1);}
   for(int i=0;i<170;i++){fast.Cars.Clear();fast.Update(1f/170,0,-1);}
   Check(Math.Abs(slow.Y-fast.Y)<.02f&&Math.Abs(slow.Time-fast.Time)<.001f,"movement and game clock match at 24 and 170 FPS",log);
  }
 }
}
