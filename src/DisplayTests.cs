using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace QuietCrossing {
 public sealed partial class Window {
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
     var clock=Stopwatch.StartNew();int frames=0;w.Paint+=(sender,args)=>frames++;
     while(clock.Elapsed.TotalSeconds<3){Application.DoEvents();Thread.Sleep(1);}
     if(w.measuredFps<=0||frames<45)throw new Exception("Real paint loop FPS "+mode);
     float fps=w.measuredFps;log.AppendLine("PASS fullscreen live loop "+mode+": "+(frames/clock.Elapsed.TotalSeconds).ToString("0.0")+" FPS, HUD="+fps.ToString("0"));
     if(mode==CameraMode.FirstPerson){using(var shot=new Bitmap(w.ClientSize.Width,w.ClientSize.Height)){w.DrawToBitmap(shot,w.ClientRectangle);Directory.CreateDirectory("docs");shot.Save("docs/fullscreen.png");}}
     w.Go("pause");
    }
    w.Close();File.WriteAllText("display-test-results.txt",log.ToString());return 0;
   }catch(Exception ex){File.WriteAllText("display-test-results.txt",log+"FAIL "+ex);return 1;}
  }
 }
}
