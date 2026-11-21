using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MouseCursor=System.Windows.Forms.Cursor;

namespace QuietCrossing {
 public sealed partial class Window {
  bool mouseTurning,mouseCursorFree,cursorHidden,clipSaved,rawRegistered;
  Point turnMousePoint,returnMousePoint;
  Rectangle previousMouseClip;
  IntPtr rawMouseBuffer=IntPtr.Zero;
  readonly MouseMotion mouseMotion=new MouseMotion();
  Rectangle rawPrimaryBounds,rawVirtualBounds;
  const int RawBufferBytes=65536;
  internal const float MouseYawGain=.002f,MousePitchGain=.002f;
  float peekYaw,lookPitch;
  [StructLayout(LayoutKind.Sequential)] struct RawDevice {public ushort Page,Usage;public uint Flags;public IntPtr Target;}
  [StructLayout(LayoutKind.Sequential)] struct RawHeader {public uint Type,Size;public IntPtr Device,Param;}
  [DllImport("user32.dll",SetLastError=true)] static extern bool RegisterRawInputDevices(RawDevice[] devices,uint count,uint size);
  [DllImport("user32.dll",SetLastError=true)] static extern uint GetRawInputData(IntPtr input,uint command,IntPtr data,ref uint size,uint headerSize);
  [DllImport("user32.dll",SetLastError=true)] static extern uint GetRawInputBuffer(IntPtr data,ref uint size,uint headerSize);
  bool Perspective {get{return settings.Camera!=CameraMode.Bird;}}
  Point MouseCenter {get{return new Point(ClientSize.Width/2,ClientSize.Height/2);}}
  void BeginMouseLook(){
   if(mouseTurning||page!="play"||!Perspective||mouseCursorFree||WindowState==FormWindowState.Minimized||(!preview&&!ContainsFocus))return;
   mouseTurning=true;turnMousePoint=MouseCenter;mouse=new PointF(-1,-1);mouseMotion.Clear();rawPrimaryBounds=Screen.PrimaryScreen.Bounds;rawVirtualBounds=SystemInformation.VirtualScreen;
   if(preview)return; // UI tests do not move or hide the user's system cursor.
   previousMouseClip=MouseCursor.Clip;returnMousePoint=MouseCursor.Position;clipSaved=true;
   try {
    rawRegistered=RegisterRawInputDevices(new RawDevice[]{new RawDevice{Page=1,Usage=2,Flags=0x30,Target=Handle}},1,(uint)Marshal.SizeOf(typeof(RawDevice)));
    if(rawRegistered&&rawMouseBuffer==IntPtr.Zero)rawMouseBuffer=Marshal.AllocHGlobal(RawBufferBytes);
    UpdateMouseClip();MouseCursor.Hide();cursorHidden=true;
    if(!rawRegistered)CenterMouse();
   } catch(System.ComponentModel.Win32Exception){EndMouseTurn(false);mouseCursorFree=true;}
  }
  void EndMouseTurn(){EndMouseTurn(true);}
  void EndMouseTurn(bool restorePosition){
   mouseTurning=false;
   if(rawRegistered){RegisterRawInputDevices(new RawDevice[]{new RawDevice{Page=1,Usage=2,Flags=1,Target=IntPtr.Zero}},1,(uint)Marshal.SizeOf(typeof(RawDevice)));rawRegistered=false;}
   mouseMotion.Clear();
   if(clipSaved){MouseCursor.Clip=previousMouseClip;clipSaved=false;if(restorePosition&&ContainsFocus)MouseCursor.Position=returnMousePoint;}
   if(cursorHidden){MouseCursor.Show();cursorHidden=false;}
   Capture=false;
  }
  void SyncMouseLook(){
   if(mouseTurning&&(page!="play"||!Perspective||mouseCursorFree||(!preview&&!ContainsFocus)))EndMouseTurn(false);
   if(!mouseTurning)BeginMouseLook();else if(!preview)UpdateMouseClip();
  }
  void UpdateMouseClip(){if(!mouseTurning||preview||!IsHandleCreated)return;Rectangle bounds=RectangleToScreen(ClientRectangle);bounds.Inflate(-2,-2);bounds=Rectangle.Intersect(bounds,SystemInformation.VirtualScreen);if(bounds.Width>0&&bounds.Height>0&&MouseCursor.Clip!=bounds)MouseCursor.Clip=bounds;}
  void CenterMouse(){turnMousePoint=MouseCenter;if(!preview&&IsHandleCreated)MouseCursor.Position=PointToScreen(turnMousePoint);}
  void ApplyMouseDelta(float dx,float dy){
   if(!mouseTurning||page!="play"||!Perspective||mouseCursorFree)return;
   yaw=PlayerControls.Wrap(yaw+dx*MouseYawGain*settings.MouseSensitivity);lookPitch=Math.Max(-.7f,Math.Min(.8f,lookPitch+dy*MousePitchGain*settings.MouseSensitivity));
  }
  void TurnMouse(MouseEventArgs e){
   if(!mouseTurning)return;
   if(page!="play"||!Perspective||mouseCursorFree){EndMouseTurn();return;}
   if(!preview&&rawRegistered)return; // WM_INPUT owns both relative and absolute devices.
   if(e.Location==turnMousePoint)return;
   ApplyMouseDelta(e.X-turnMousePoint.X,e.Y-turnMousePoint.Y);
   if(preview)turnMousePoint=e.Location;else CenterMouse();
  }
  void ReadRawMouse(IntPtr input){
   if(!mouseTurning||!rawRegistered||rawMouseBuffer==IntPtr.Zero||!ContainsFocus)return;
   uint size=RawBufferBytes,header=(uint)Marshal.SizeOf(typeof(RawHeader));
   uint copied=GetRawInputData(input,0x10000003,rawMouseBuffer,ref size,header);
   if(copied==uint.MaxValue||copied<header+24||Marshal.ReadInt32(rawMouseBuffer)!=0)return;
   ConsumeRawPacket(rawMouseBuffer,(int)header);DrainMouseQueue();FlushMouseMotion();
  }
  void ConsumeRawPacket(IntPtr packet,int header){
   if(Marshal.ReadInt32(packet)!=0)return;
   ushort flags=(ushort)Marshal.ReadInt16(packet,header);
   mouseMotion.Add(Marshal.ReadIntPtr(packet,8),flags,Marshal.ReadInt32(packet,header+12),Marshal.ReadInt32(packet,header+16),(flags&2)!=0?rawVirtualBounds:rawPrimaryBounds);
  }
  void DrainMouseQueue(){
   if(!mouseTurning||!rawRegistered||rawMouseBuffer==IntPtr.Zero||!ContainsFocus)return;
   // WOW64 has a distinct buffered layout. Its standard per-message reader is
   // kept as a fallback; native 32/64-bit processes use batched reads.
   if(IntPtr.Size==4&&Environment.Is64BitOperatingSystem)return;
   uint header=(uint)Marshal.SizeOf(typeof(RawHeader));
   for(int batch=0;batch<4;batch++){
    uint size=RawBufferBytes,count=GetRawInputBuffer(rawMouseBuffer,ref size,header);if(count==0||count==uint.MaxValue)break;
    int offset=0;
    for(uint i=0;i<count;i++){
     if(offset>RawBufferBytes-(int)header)break;
     IntPtr packet=IntPtr.Add(rawMouseBuffer,offset);int length=Marshal.ReadInt32(packet,4);
     if(length<(int)header||length>RawBufferBytes-offset)break;
     if(length>=header+24)ConsumeRawPacket(packet,(int)header);
     offset=(offset+length+IntPtr.Size-1)&~(IntPtr.Size-1);
    }
   }
  }
  void FlushMouseMotion(){float x,y;mouseMotion.Take(out x,out y);if(x!=0||y!=0)ApplyMouseDelta(x,y);}
  protected override void WndProc(ref Message message){if(HandleFrameMessage(ref message))return;if(message.Msg==0x007E){displayDirty=true;mouseMotion.Clear();}if(message.Msg==0x00FF)ReadRawMouse(message.LParam);base.WndProc(ref message);}
  protected override void OnResize(EventArgs e){base.OnResize(e);if(mouseTurning){if(WindowState==FormWindowState.Minimized)EndMouseTurn(false);else UpdateMouseClip();}}
  protected override void OnLocationChanged(EventArgs e){base.OnLocationChanged(e);displayDirty=true;if(mouseTurning)UpdateMouseClip();}
  void DisposeMouseLook(){EndMouseTurn(false);if(rawMouseBuffer!=IntPtr.Zero){Marshal.FreeHGlobal(rawMouseBuffer);rawMouseBuffer=IntPtr.Zero;}}
 }
}
