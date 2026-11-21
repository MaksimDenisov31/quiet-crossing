using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MouseCursor=System.Windows.Forms.Cursor;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 public sealed partial class Window {
  [DllImport("user32.dll",SetLastError=true)] static extern uint GetRegisteredRawInputDevices([Out] RawDevice[] devices,ref uint count,uint size);
  void CheckNativeMousePackets(System.Text.StringBuilder log){
   uint count=0,size=(uint)Marshal.SizeOf(typeof(RawDevice));GetRegisteredRawInputDevices(null,ref count,size);
   var devices=new RawDevice[count];if(GetRegisteredRawInputDevices(devices,ref count,size)==uint.MaxValue)throw new Exception("Cannot query raw mouse registration");
   if(!Array.Exists(devices,d=>d.Page==1&&d.Usage==2&&d.Target==Handle&&(d.Flags&0x30)==0x30))throw new Exception("Captured mouse must suppress duplicate legacy events");
   int header=Marshal.SizeOf(typeof(RawHeader));IntPtr packet=Marshal.AllocHGlobal(header+24);
   try{
    for(int i=0;i<header+24;i++)Marshal.WriteByte(packet,i,0);
    Marshal.WriteInt32(packet,4,header+24);Marshal.WriteIntPtr(packet,8,new IntPtr(42));Marshal.WriteInt32(packet,header+12,1);
    foreach(CameraMode mode in new[]{CameraMode.FirstPerson,CameraMode.ThirdPerson}){
     settings.Camera=mode;yaw=0;lookPitch=0;mouseMotion.Clear();var clock=System.Diagnostics.Stopwatch.StartNew();
     for(int i=0;i<8000;i++)ConsumeRawPacket(packet,header);
     FlushMouseMotion();float expected=PlayerControls.Wrap(8000*MouseYawGain*settings.MouseSensitivity);
     if(Math.Abs(yaw-expected)>.0001f)throw new Exception("Native packet burst lost mouse counts");
     float before=yaw;Point center=MouseCenter;OnMouseMove(new MouseEventArgs(MouseButtons.None,0,center.X+80,center.Y+50,0));
     if(yaw!=before)throw new Exception("Legacy mouse message duplicated a raw turn");
     log.AppendLine("PASS "+mode+": 8000 synthetic native-format relative packets preserved; legacy duplicate ignored; burst ms="+clock.Elapsed.TotalMilliseconds.ToString("0.00"));
    }
    mouseMotion.Clear();float facing=yaw;Point cursor=MouseCursor.Position;Marshal.WriteInt16(packet,header,3);Marshal.WriteInt32(packet,header+12,50000);Marshal.WriteInt32(packet,header+16,40000);ConsumeRawPacket(packet,header);FlushMouseMotion();
    if(yaw!=facing||MouseCursor.Position!=cursor)throw new Exception("Absolute baseline must neither jump nor warp cursor");
    log.AppendLine("PASS absolute native-format first packet neither rotates camera nor warps cursor");
   }finally{Marshal.FreeHGlobal(packet);mouseMotion.Clear();settings.Camera=CameraMode.FirstPerson;}
  }
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
     if(w.rawRegistered)w.CheckNativeMousePackets(log);else throw new Exception("Raw Input registration failed");
     w.ClientSize=new Size(w.ClientSize.Width+20,w.ClientSize.Height+20);expected=w.RectangleToScreen(w.ClientRectangle);expected.Inflate(-2,-2);expected=Rectangle.Intersect(expected,SystemInformation.VirtualScreen);
     if(MouseCursor.Clip!=expected)throw new Exception("Resize did not update mouse bounds");
     w.SetFullscreen(true);Application.DoEvents();w.SyncMouseLook();expected=w.RectangleToScreen(w.ClientRectangle);expected.Inflate(-2,-2);expected=Rectangle.Intersect(expected,SystemInformation.VirtualScreen);
     if(MouseCursor.Clip!=expected||!w.cursorHidden)throw new Exception("Fullscreen did not update mouse bounds; page="+w.page+", focus="+w.ContainsFocus+", hidden="+w.cursorHidden+", expected="+expected+", actual="+MouseCursor.Clip);
     w.SetFullscreen(false);Application.DoEvents();w.SyncMouseLook();
     log.AppendLine("PASS fullscreen/windowed transitions update real mouse lock and preserve hidden cursor");
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
