using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 // Only one frame request may be in flight. Late frames never build up a queue.
 sealed class FramePump : IDisposable {
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateWaitableTimerExW(IntPtr attributes,string name,uint flags,uint access);
  [DllImport("kernel32.dll")] static extern bool SetWaitableTimer(IntPtr timer,ref long due,int period,IntPtr callback,IntPtr argument,bool resume);
  [DllImport("kernel32.dll")] static extern uint WaitForMultipleObjects(uint count,IntPtr[] handles,bool all,uint milliseconds);
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
  readonly Func<bool> post;readonly AutoResetEvent changed=new AutoResetEvent(false);readonly Thread worker;
  volatile bool running,disposed;int rate=60,pending;
  public bool Running {get{return running;}}
  public int Rate {get{return Volatile.Read(ref rate);}set{Interlocked.Exchange(ref rate,Math.Max(1,value));changed.Set();}}
  public FramePump(Func<bool> postFrame){post=postFrame;worker=new Thread(Run){IsBackground=true,Name="QuietCrossing frame pacing"};worker.Start();}
  public void Start(){running=true;changed.Set();}
  public void Stop(){running=false;changed.Set();}
  public void Presented(){Interlocked.Exchange(ref pending,0);if(!disposed)changed.Set();}
  void Run(){
   var clock=Stopwatch.StartNew();double deadline=0;int previousRate=0;
   IntPtr precisionTimer=CreateWaitableTimerExW(IntPtr.Zero,null,2,0x100002);
   IntPtr[] handles={changed.SafeWaitHandle.DangerousGetHandle(),precisionTimer};
   try{while(!disposed){
    if(!running){changed.WaitOne();deadline=0;continue;}
    int currentRate=Rate;double now=clock.Elapsed.TotalSeconds;
    if(currentRate!=previousRate){deadline=now;previousRate=currentRate;}
    double remaining=deadline-now;
    if(remaining>0){
     long due=-(long)Math.Max(1,Math.Ceiling(remaining*10000000));
     if(precisionTimer!=IntPtr.Zero&&SetWaitableTimer(precisionTimer,ref due,0,IntPtr.Zero,IntPtr.Zero,false))WaitForMultipleObjects(2,handles,false,1000);
     else changed.WaitOne(Math.Max(1,(int)(remaining*1000)));
     continue;
    }
    if(Interlocked.CompareExchange(ref pending,1,0)!=0){changed.WaitOne(1);continue;}
    if(!post())Interlocked.Exchange(ref pending,0);
    deadline=NextDeadline(deadline,now,currentRate);
   }}finally{if(precisionTimer!=IntPtr.Zero)CloseHandle(precisionTimer);}
  }
  internal static double NextDeadline(double previous,double now,int rate){
   double interval=1.0/Math.Max(1,rate),next=previous+interval;
   // Preserve cadence while dropping obsolete deadlines after a stall.
   return next>now?next:now+interval;
  }
  public void Dispose(){if(disposed)return;disposed=true;running=false;changed.Set();worker.Join();changed.Dispose();}
 }
}
