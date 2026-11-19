using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MouseCursor=System.Windows.Forms.Cursor;

namespace QuietCrossing {
 public sealed partial class Window {
  public static int RunPointerTest(){
   var log=new System.Text.StringBuilder();
   using(var w=new Window(true,false)){
    Rectangle originalClip=MouseCursor.Clip;Point originalPosition=MouseCursor.Position;
    try {
     w.Show();w.Activate();w.Focus();Application.DoEvents();
     originalClip=MouseCursor.Clip;
     w.settings.Camera=CameraMode.FirstPerson;w.Go("play");w.EndMouseTurn();
     if(!w.ContainsFocus)throw new Exception("Test window does not have focus");
     w.preview=false;w.BeginMouseLook();
     Rectangle expected=w.RectangleToScreen(w.ClientRectangle);expected.Inflate(-2,-2);expected=Rectangle.Intersect(expected,SystemInformation.VirtualScreen);
     if(!w.mouseTurning||!w.cursorHidden||MouseCursor.Clip!=expected)throw new Exception("System mouse lock/hide failed");
     log.AppendLine("PASS real cursor clip and hide; raw input registered="+w.rawRegistered);
     w.ClientSize=new Size(w.ClientSize.Width+20,w.ClientSize.Height+20);expected=w.RectangleToScreen(w.ClientRectangle);expected.Inflate(-2,-2);expected=Rectangle.Intersect(expected,SystemInformation.VirtualScreen);
     if(MouseCursor.Clip!=expected)throw new Exception("Resize did not update mouse bounds");
     w.Go("map");if(w.mouseTurning||w.cursorHidden||MouseCursor.Clip!=originalClip)throw new Exception("Map did not restore cursor");
     w.Go("play");if(!w.mouseTurning)throw new Exception("Resume did not recapture cursor");
     w.OnKeyDown(new KeyEventArgs(Keys.Tab));w.OnKeyUp(new KeyEventArgs(Keys.Tab));
     if(w.mouseTurning||w.cursorHidden||MouseCursor.Clip!=originalClip)throw new Exception("Tab did not release system cursor");
     w.OnKeyDown(new KeyEventArgs(Keys.Tab));w.OnKeyUp(new KeyEventArgs(Keys.Tab));if(!w.mouseTurning)throw new Exception("Tab did not recapture cursor");
     w.OnDeactivate(EventArgs.Empty);if(w.mouseTurning||w.cursorHidden||MouseCursor.Clip!=originalClip)throw new Exception("Focus loss did not restore system cursor");
     w.Go("play");w.settings.Camera=CameraMode.Bird;w.SyncMouseLook();
     if(w.mouseTurning||w.cursorHidden||MouseCursor.Clip!=originalClip)throw new Exception("Bird view did not release system cursor");
     log.AppendLine("PASS map, Tab, focus loss, bird view restore real system cursor; resume recaptures");
     File.WriteAllText("pointer-test-results.txt",log.ToString());return 0;
    }catch(Exception error){File.WriteAllText("pointer-test-results.txt",log+"FAIL "+error);return 1;}
    finally{w.EndMouseTurn(false);w.preview=true;MouseCursor.Clip=originalClip;if(w.ContainsFocus)MouseCursor.Position=originalPosition;w.Close();}
   }
  }
 }
}
