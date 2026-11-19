using System;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 static class MonitorRefresh {
  [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct DisplayMode {
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Device;
   public ushort Spec,Driver,Size,Extra;public uint Fields;
   public int X,Y;public uint Orientation,FixedOutput;
   public short Color,Duplex,YResolution,TTOption,Collate;
   [MarshalAs(UnmanagedType.ByValTStr,SizeConst=32)] public string Form;
   public ushort LogPixels;public uint BitsPerPixel,Width,Height,Flags,Frequency;
   public uint IcmMethod,IcmIntent,Media,Dither,Reserved1,Reserved2,PanningWidth,PanningHeight;
  }
  [DllImport("user32.dll",CharSet=CharSet.Unicode,EntryPoint="EnumDisplaySettingsExW")]
  [return:MarshalAs(UnmanagedType.Bool)] static extern bool EnumDisplaySettings(string device,int mode,ref DisplayMode settings,uint flags);
  internal static int Normalize(uint frequency){return frequency>1&&frequency<=int.MaxValue?(int)frequency:60;}
  public static int Read(string device){
   var mode=new DisplayMode{Size=(ushort)Marshal.SizeOf(typeof(DisplayMode))};
   return EnumDisplaySettings(device,-1,ref mode,0)&&(mode.Fields&0x400000)!=0?Normalize(mode.Frequency):60;
  }
 }
}
