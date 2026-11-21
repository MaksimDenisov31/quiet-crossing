using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace QuietCrossing {
 public sealed partial class Window {
  [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
  [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
  bool fullscreen,preciseTimer;Rectangle windowBounds;bool windowMaximized;
  double fpsSince,nextMonitorCheck;int fpsFrames;float measuredFps;
  int targetFps=60;string monitorDevice="";bool displayDirty=true;
  Func<string,int> monitorRateReader=MonitorRefresh.Read;
  double frameWork,paintWork,sceneWork,pixelWork,textWork;int presentedFrames;
  const int FrameMessage=0x8001;
  [DllImport("user32.dll",EntryPoint="PostMessageW")]
  [return:MarshalAs(UnmanagedType.Bool)] static extern bool PostFrame(IntPtr handle,int message,IntPtr wParam,IntPtr lParam);
  void InitializeDisplay(){
   windowBounds=Bounds;windowMaximized=WindowState==FormWindowState.Maximized;
   if(!preview){preciseTimer=timeBeginPeriod(1)==0;if(settings.Fullscreen)SetFullscreen(true);}
   RefreshMonitor();
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
  void RefreshMonitor(){
   Screen display=Screen.FromControl(this);if(fullscreen&&Bounds!=display.Bounds)Bounds=display.Bounds;
   if(mouseTurning){rawPrimaryBounds=Screen.PrimaryScreen.Bounds;rawVirtualBounds=SystemInformation.VirtualScreen;}
   monitorDevice=display.DeviceName;targetFps=monitorRateReader(monitorDevice);
   if(timer!=null)timer.Rate=targetFps;
   displayDirty=false;nextMonitorCheck=watch.Elapsed.TotalSeconds+1;
  }
  bool HandleFrameMessage(ref Message message){
   if(message.Msg!=FrameMessage)return false;
   double start=watch.Elapsed.TotalSeconds;
   try{if(timer!=null&&timer.Running&&!IsDisposed){DrainMouseQueue();FlushMouseMotion();Tick(this,EventArgs.Empty);Update();frameWork+=watch.Elapsed.TotalSeconds-start;presentedFrames++;}}
   finally{if(timer!=null)timer.Presented();}
   message.Result=IntPtr.Zero;return true;
  }
  void RecordFrame(){
   double now=watch.Elapsed.TotalSeconds;fpsFrames++;
   if(now-fpsSince>=.5){measuredFps=(float)(fpsFrames/(now-fpsSince));fpsFrames=0;fpsSince=now;}
  }
  void DrawFps(Graphics g){
   float y=page=="play"?70:12;
   Rect(g,ink,380,y,87,27);TextAt(g,"FPS "+(measuredFps>0?measuredFps.ToString("0"):"—")+" / "+targetFps,386,y+3,9,mint,true);
   TextAt(g,"Монитор: "+targetFps+" Гц",386,y+16,7,cream);
  }
  void DisposeDisplay(){if(preciseTimer){timeEndPeriod(1);preciseTimer=false;}}
 }
}
