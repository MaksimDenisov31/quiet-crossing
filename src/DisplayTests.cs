using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 public sealed partial class Window {
  [DllImport("user32.dll")] static extern uint MsgWaitForMultipleObjectsEx(uint count,IntPtr handles,uint milliseconds,uint wakeMask,uint flags);
  public static int RunDisplayTest(){
   var log=new System.Text.StringBuilder();
   using(var w=new Window(true,false))try{
    w.Show();w.Activate();Application.DoEvents();
    Rectangle original=w.Bounds;Game same=w.game;
    w.OnKeyDown(new KeyEventArgs(Keys.F11));w.OnKeyUp(new KeyEventArgs(Keys.F11));Application.DoEvents();
    if(!w.fullscreen||w.FormBorderStyle!=FormBorderStyle.None||w.Bounds!=Screen.FromControl(w).Bounds||w.game!=same)throw new Exception("Fullscreen bounds/state");
    w.Refresh();Rectangle viewport=w.viewport;if(Math.Abs(viewport.Width/(double)viewport.Height-1.5)>.005)throw new Exception("Pixel viewport aspect ratio");
    w.OnKeyDown(new KeyEventArgs(Keys.Enter|Keys.Alt));w.OnKeyUp(new KeyEventArgs(Keys.Enter));Application.DoEvents();
    if(w.fullscreen||w.Bounds!=original||w.FormBorderStyle!=FormBorderStyle.Sizable||w.game!=same)throw new Exception("Window restore");
    log.AppendLine("PASS F11 fullscreen covers monitor, Alt+Enter restores exact window bounds, aspect ratio and game preserved");
    w.SetFullscreen(true);w.preciseTimer=timeBeginPeriod(1)==0;
    foreach(CameraMode mode in Enum.GetValues(typeof(CameraMode))){
     w.settings.Camera=mode;w.StartGame();w.game.Cars.Clear();w.Activate();w.Focus();
     w.frameWork=w.paintWork=0;w.presentedFrames=0;
     w.sceneWork=w.pixelWork=w.textWork=0;
     var clock=Stopwatch.StartNew();int frames=0;PaintEventHandler countFrame=(sender,args)=>frames++;w.Paint+=countFrame;
     while(clock.Elapsed.TotalSeconds<3){Application.DoEvents();MsgWaitForMultipleObjectsEx(0,IntPtr.Zero,20,0x4FF,4);}
     if(w.measuredFps<=0||frames/clock.Elapsed.TotalSeconds<Math.Min(75,w.targetFps*.6))throw new Exception("Real paint loop FPS "+mode);
     float fps=w.measuredFps;log.AppendLine("PASS fullscreen live loop "+mode+": "+(frames/clock.Elapsed.TotalSeconds).ToString("0.0")+" FPS, HUD="+fps.ToString("0"));
     log.AppendLine("INFO target="+w.targetFps+" monitor="+w.monitorDevice+" frame work ms="+(w.frameWork*1000/Math.Max(1,w.presentedFrames)).ToString("0.00")+" paint ms="+(w.paintWork*1000/Math.Max(1,frames)).ToString("0.00"));
     log.AppendLine("INFO scene/pixels/text ms="+(w.sceneWork*1000/Math.Max(1,frames)).ToString("0.00")+" / "+(w.pixelWork*1000/Math.Max(1,frames)).ToString("0.00")+" / "+(w.textWork*1000/Math.Max(1,frames)).ToString("0.00"));
     if(mode==CameraMode.FirstPerson){using(var shot=new Bitmap(w.ClientSize.Width,w.ClientSize.Height)){w.DrawToBitmap(shot,w.ClientRectangle);Directory.CreateDirectory("docs");shot.Save("docs/fullscreen.png");}}
     w.Paint-=countFrame;
     w.Go("pause");
    }
    int detected=MonitorRefresh.Read(Screen.FromControl(w).DeviceName);
    if(w.targetFps!=detected||w.timer.Rate!=detected)throw new Exception("Selected frame rate differs from current Windows mode");
    log.AppendLine("PASS current Windows monitor rate selected: "+detected+" Hz");
    w.settings.Camera=CameraMode.Bird;w.StartGame();int fakeRate=120;w.monitorRateReader=device=>fakeRate;w.RefreshMonitor();
    var limitedClock=Stopwatch.StartNew();int limitedFrames=0;PaintEventHandler countLimited=(sender,args)=>limitedFrames++;w.Paint+=countLimited;
    while(limitedClock.Elapsed.TotalSeconds<2){Application.DoEvents();MsgWaitForMultipleObjectsEx(0,IntPtr.Zero,20,0x4FF,4);}
    w.Paint-=countLimited;double limitedFps=limitedFrames/limitedClock.Elapsed.TotalSeconds;
    if(limitedFps<90||limitedFps>126||w.timer.Rate!=120)throw new Exception("120 Hz simulated rate pacing: "+limitedFps);
    log.AppendLine("PASS simulated 120 Hz monitor rate, real window loop: "+limitedFps.ToString("0.0")+" FPS (Windows settings unchanged)");
    w.Go("pause");Game preserved=w.game;float preservedTime=w.game.Time;
    foreach(int rate in new[]{60,144,165,170,240}){
     fakeRate=rate;Message display=Message.Create(w.Handle,0x007E,IntPtr.Zero,IntPtr.Zero);w.WndProc(ref display);w.Tick(null,EventArgs.Empty);
     if(w.targetFps!=rate||w.timer.Rate!=rate||w.game!=preserved||w.game.Time!=preservedTime)throw new Exception("Display change must update rate without resetting/advancing paused game");
    }
    w.monitorRateReader=MonitorRefresh.Read;w.RefreshMonitor();
    log.AppendLine("PASS simulated display changes 60/144/165/170/240 Hz update pacing without resetting game; physical Windows rate restored");
    w.SetFullscreen(false);
    foreach(Screen screen in Screen.AllScreens){
     w.Bounds=new Rectangle(screen.WorkingArea.X+20,screen.WorkingArea.Y+20,Math.Min(1000,screen.WorkingArea.Width-40),Math.Min(720,screen.WorkingArea.Height-40));
     w.RefreshMonitor();int rate=MonitorRefresh.Read(screen.DeviceName);
     if(w.monitorDevice!=screen.DeviceName||w.targetFps!=rate||w.timer.Rate!=rate||w.game!=preserved)throw new Exception("Physical monitor transfer selection");
     log.AppendLine("PASS physical window transfer: "+screen.DeviceName+" -> "+rate+" Hz, same game");
    }
    w.Close();File.WriteAllText("display-test-results.txt",log.ToString());return 0;
   }catch(Exception ex){File.WriteAllText("display-test-results.txt",log+"FAIL "+ex);return 1;}
  }
 }
}
