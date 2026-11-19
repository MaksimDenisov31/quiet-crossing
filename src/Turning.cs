using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuietCrossing {
 public sealed partial class Window {
  bool mouseTurning;
  int turnMouseX;
  void EndMouseTurn(){mouseTurning=false;Capture=false;}
  void BeginMouseTurn(MouseEventArgs e){mouseTurning=true;turnMouseX=e.X;Capture=true;}
  void TurnMouse(MouseEventArgs e){
   if(!mouseTurning)return;
   if(page!="play"||settings.Camera==CameraMode.Bird){EndMouseTurn();return;}
   yaw=PlayerControls.Wrap(yaw+(e.X-turnMouseX)*480f/Math.Max(480,viewport.Width)*.01f);turnMouseX=e.X;Invalidate();
  }
  protected override void OnMouseUp(MouseEventArgs e){if(e.Button==MouseButtons.Right)EndMouseTurn();base.OnMouseUp(e);}
  protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture)mouseTurning=false;base.OnMouseCaptureChanged(e);}
 }
}
