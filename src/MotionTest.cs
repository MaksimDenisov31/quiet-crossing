using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 public sealed partial class Window {
  bool replayingMouse;
  public static int RunMotionTest(){
   var log=new System.Text.StringBuilder();
   using(var w=new Window(true,false))try{
    w.replayingMouse=true;w.Show();w.Activate();w.Focus();Application.DoEvents();w.SetFullscreen(true);w.preciseTimer=timeBeginPeriod(1)==0;
    foreach(CameraMode mode in new[]{CameraMode.FirstPerson,CameraMode.ThirdPerson})foreach(int pollRate in new[]{1000,8000}){
     w.settings.Camera=mode;w.StartGame();Tests.VisitRoads(w.game,180);w.game.Y=CityMap.Streets[180].Bottom+28;
     int header=Marshal.SizeOf(typeof(RawHeader));IntPtr packet=Marshal.AllocHGlobal(header+24);
     for(int i=0;i<header+24;i++)Marshal.WriteByte(packet,i,0);Marshal.WriteInt32(packet,4,header+24);
     var intervals=new List<double>();var clock=Stopwatch.StartNew();double previous=0;int sampleIndex=0,lastX=0,lastY=0;bool activeThroughout=true;
     PaintEventHandler sample=(sender,args)=>{
      double now=clock.Elapsed.TotalSeconds;if(previous>0)intervals.Add((now-previous)*1000);previous=now;
      if(w.page!="play"||!w.mouseTurning)activeThroughout=false;
      int wanted=(int)(now*pollRate);
      while(sampleIndex<wanted){
       double t=++sampleIndex/(double)pollRate;int x=(int)Math.Round((t*1.8+Math.Sin(t*2)*.7)/MouseYawGain),y=(int)Math.Round(Math.Sin(t*2.3)*.5/MousePitchGain);
       Marshal.WriteInt32(packet,header+12,x-lastX);Marshal.WriteInt32(packet,header+16,y-lastY);w.ConsumeRawPacket(packet,header);lastX=x;lastY=y;
      }
      w.FlushMouseMotion();
     };
     w.Paint+=sample;
     try{while(clock.Elapsed.TotalSeconds<6){Application.DoEvents();MsgWaitForMultipleObjectsEx(0,IntPtr.Zero,20,0x4FF,4);}}
     finally{w.Paint-=sample;Marshal.FreeHGlobal(packet);}
     intervals.Sort();if(intervals.Count<30)throw new Exception("Too few motion frames");
     if(!activeThroughout||Math.Abs(PlayerControls.Wrap(w.yaw-PlayerControls.Wrap(lastX*MouseYawGain)))>.002f)throw new Exception("Motion replay interrupted: active="+activeThroughout+", mode="+mode+", rate="+pollRate+", yaw="+w.yaw+", expected="+PlayerControls.Wrap(lastX*MouseYawGain));
     log.AppendLine(mode+" synthetic mouse "+pollRate+" Hz, packets="+sampleIndex+" FPS="+(intervals.Count/clock.Elapsed.TotalSeconds).ToString("0.0")+" interval ms median/p95/p99/max="+intervals[intervals.Count/2].ToString("0.00")+" / "+intervals[(int)(intervals.Count*.95)].ToString("0.00")+" / "+intervals[(int)(intervals.Count*.99)].ToString("0.00")+" / "+intervals[intervals.Count-1].ToString("0.00"));
     if(intervals[(int)(intervals.Count*.99)]>50)throw new Exception("Motion test has persistent long stalls");
     w.Go("pause");
    }
    w.Close();File.WriteAllText("motion-test-results.txt",log.ToString());return 0;
   }catch(Exception ex){File.WriteAllText("motion-test-results.txt",log+"FAIL "+ex);return 1;}
  }
 }
}
