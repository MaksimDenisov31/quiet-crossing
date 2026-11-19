using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuietCrossing {
 public sealed partial class Window {
  [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
  [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
  bool fullscreen,preciseTimer;Rectangle windowBounds;bool windowMaximized;
  double nextFrame,fpsSince;int fpsFrames;float measuredFps;
  void InitializeDisplay(){
   windowBounds=Bounds;windowMaximized=WindowState==FormWindowState.Maximized;
   if(!preview){preciseTimer=timeBeginPeriod(1)==0;if(settings.Fullscreen)SetFullscreen(true);}
  }
  void StoreWindowBounds(){
   Rectangle bounds=fullscreen?windowBounds:WindowState==FormWindowState.Normal?Bounds:RestoreBounds;
   settings.Width=bounds.Width;settings.Height=bounds.Height;
   settings.Maximized=fullscreen?windowMaximized:WindowState==FormWindowState.Maximized;
  }
  void ToggleFullscreen(){SetFullscreen(!fullscreen);settings.Fullscreen=fullscreen;Save();}
  void SetFullscreen(bool value){
   if(fullscreen==value)return;
   EndMouseTurn();keys.Clear();
   if(value){
    windowBounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;windowMaximized=WindowState==FormWindowState.Maximized;
    Rectangle screen=Screen.FromControl(this).Bounds;
    WindowState=FormWindowState.Normal;FormBorderStyle=FormBorderStyle.None;Bounds=screen;fullscreen=true;
   }else{
    fullscreen=false;WindowState=FormWindowState.Normal;FormBorderStyle=FormBorderStyle.Sizable;
    Bounds=windowBounds;if(windowMaximized)WindowState=FormWindowState.Maximized;
   }
   BeginMouseLook();Invalidate();
  }
  void ScheduledTick(object sender,EventArgs e){
   double now=watch.Elapsed.TotalSeconds;if(now<nextFrame)return;
   // Keep a stable 60 Hz deadline without adding a full timer interval to rendering time.
   nextFrame+=1.0/60;if(nextFrame<now)nextFrame=now+1.0/60;
   Tick(sender,e);
  }
  void RecordFrame(){
   double now=watch.Elapsed.TotalSeconds;fpsFrames++;
   if(now-fpsSince>=.5){measuredFps=(float)(fpsFrames/(now-fpsSince));fpsFrames=0;fpsSince=now;}
  }
  void DrawFps(Graphics g){
   float y=page=="play"?70:12;
   Rect(g,ink,394,y,73,17);TextAt(g,"FPS "+(measuredFps>0?measuredFps.ToString("0"):"—"),401,y+3,9,mint,true);
  }
  void DisposeDisplay(){if(preciseTimer){timeEndPeriod(1);preciseTimer=false;}}
 }
}
