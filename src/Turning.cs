using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MouseCursor=System.Windows.Forms.Cursor;

namespace QuietCrossing {
 public sealed partial class Window {
  bool mouseTurning,mouseCursorFree,cursorHidden,clipSaved,rawRegistered,rawAbsolute;
  Point turnMousePoint,returnMousePoint;
  Rectangle previousMouseClip;
  IntPtr rawMouseBuffer=IntPtr.Zero;
  float peekYaw,lookPitch;
  [StructLayout(LayoutKind.Sequential)] struct RawDevice {public ushort Page,Usage;public uint Flags;public IntPtr Target;}
  [StructLayout(LayoutKind.Sequential)] struct RawHeader {public uint Type,Size;public IntPtr Device,Param;}
  [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterRawInputDevices(RawDevice[] devices,uint count,uint size);
  [DllImport("user32.dll",SetLastError=true)] static extern uint GetRawInputData(IntPtr input,uint command,IntPtr data,ref uint size,uint headerSize);
  bool Perspective {get{return settings.Camera!=CameraMode.Bird;}}
  Point MouseCenter {get{return new Point(ClientSize.Width/2,ClientSize.Height/2);}}
  void BeginMouseLook(){
   if(mouseTurning||page!="play"||!Perspective||mouseCursorFree||WindowState==FormWindowState.Minimized||(!preview&&!ContainsFocus))return;
   mouseTurning=true;turnMousePoint=MouseCenter;mouse=new PointF(-1,-1);
   if(preview)return; // UI tests do not move or hide the user's system cursor.
   previousMouseClip=MouseCursor.Clip;returnMousePoint=MouseCursor.Position;clipSaved=true;
   try {
    rawRegistered=RegisterRawInputDevices(new RawDevice[]{new RawDevice{Page=1,Usage=2,Target=Handle}},1,(uint)Marshal.SizeOf(typeof(RawDevice)));
    rawAbsolute=false;
    if(rawRegistered&&rawMouseBuffer==IntPtr.Zero)rawMouseBuffer=Marshal.AllocHGlobal(128);
    UpdateMouseClip();MouseCursor.Hide();cursorHidden=true;
    if(!rawRegistered)CenterMouse();
   } catch(System.ComponentModel.Win32Exception){EndMouseTurn(false);mouseCursorFree=true;}
  }
  void EndMouseTurn(){EndMouseTurn(true);}
  void EndMouseTurn(bool restorePosition){
   mouseTurning=false;
   if(rawRegistered){RegisterRawInputDevices(new RawDevice[]{new RawDevice{Page=1,Usage=2,Flags=1,Target=IntPtr.Zero}},1,(uint)Marshal.SizeOf(typeof(RawDevice)));rawRegistered=false;}
   rawAbsolute=false;
   if(clipSaved){MouseCursor.Clip=previousMouseClip;clipSaved=false;if(restorePosition&&ContainsFocus)MouseCursor.Position=returnMousePoint;}
   if(cursorHidden){MouseCursor.Show();cursorHidden=false;}
   Capture=false;
  }
  void SyncMouseLook(){
   if(mouseTurning&&(page!="play"||!Perspective||mouseCursorFree||(!preview&&!ContainsFocus)))EndMouseTurn(false);
   if(!mouseTurning)BeginMouseLook();
  }
  void UpdateMouseClip(){if(!mouseTurning||preview||!IsHandleCreated)return;Rectangle bounds=RectangleToScreen(ClientRectangle);bounds.Inflate(-2,-2);bounds=Rectangle.Intersect(bounds,SystemInformation.VirtualScreen);if(bounds.Width>0&&bounds.Height>0)MouseCursor.Clip=bounds;}
  void CenterMouse(){turnMousePoint=MouseCenter;if(!preview&&IsHandleCreated)MouseCursor.Position=PointToScreen(turnMousePoint);}
  void ApplyMouseDelta(float dx,float dy){
   if(!mouseTurning||page!="play"||!Perspective||mouseCursorFree)return;
   yaw=PlayerControls.Wrap(yaw+dx*.005f);lookPitch=Math.Max(-.7f,Math.Min(.8f,lookPitch+dy*.0035f));Invalidate();
  }
  void TurnMouse(MouseEventArgs e){
   if(!mouseTurning)return;
   if(page!="play"||!Perspective||mouseCursorFree){EndMouseTurn();return;}
   if(!preview&&rawRegistered&&!rawAbsolute)return; // WM_INPUT owns the relative deltas.
   if(e.Location==turnMousePoint)return;
   float scale=480f/Math.Max(480,viewport.Width);
   ApplyMouseDelta((e.X-turnMousePoint.X)*scale,(e.Y-turnMousePoint.Y)*scale);
   if(preview)turnMousePoint=e.Location;else CenterMouse();
  }
  void ReadRawMouse(IntPtr input){
   if(!mouseTurning||!rawRegistered||rawMouseBuffer==IntPtr.Zero||!ContainsFocus)return;
   uint size=128,header=(uint)Marshal.SizeOf(typeof(RawHeader));
   uint copied=GetRawInputData(input,0x10000003,rawMouseBuffer,ref size,header);
   if(copied==uint.MaxValue||copied<header+24||Marshal.ReadInt32(rawMouseBuffer)!=0)return;
   int offset=(int)header;
   if((Marshal.ReadInt16(rawMouseBuffer,offset)&1)!=0){if(!rawAbsolute){rawAbsolute=true;CenterMouse();}return;}
   rawAbsolute=false;
   ApplyMouseDelta(Marshal.ReadInt32(rawMouseBuffer,offset+12),Marshal.ReadInt32(rawMouseBuffer,offset+16));
  }
  protected override void WndProc(ref Message message){if(message.Msg==0x00FF)ReadRawMouse(message.LParam);base.WndProc(ref message);}
  protected override void OnResize(EventArgs e){base.OnResize(e);if(mouseTurning){if(WindowState==FormWindowState.Minimized)EndMouseTurn(false);else UpdateMouseClip();}}
  protected override void OnLocationChanged(EventArgs e){base.OnLocationChanged(e);if(mouseTurning)UpdateMouseClip();}
  void DisposeMouseLook(){EndMouseTurn(false);if(rawMouseBuffer!=IntPtr.Zero){Marshal.FreeHGlobal(rawMouseBuffer);rawMouseBuffer=IntPtr.Zero;}}
 }
}
