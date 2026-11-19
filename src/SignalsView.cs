using System;
using System.Drawing;

namespace QuietCrossing {
 static class SignalPaint {
  public static readonly Color Red=Color.FromArgb(234,105,86),Yellow=Color.FromArgb(244,204,103),Green=Color.FromArgb(143,222,151),Off=Color.FromArgb(65,81,75);
  public static bool RedOn(SignalState s){return s.Traffic==SignalColor.Red||s.Traffic==SignalColor.RedYellow;}
  public static bool YellowOn(SignalState s){return s.Traffic==SignalColor.Yellow||s.Traffic==SignalColor.RedYellow;}
 }
 public sealed partial class Window {
  void SignalCircle(Graphics g,Color color,float x,float y,float size){using(var brush=new SolidBrush(color))g.FillEllipse(brush,x,y,size,size);}
  void DrawRoadSignals(Graphics g,Street road){
   if(!road.HasLights)return;SignalState s=TrafficSignals.At(road,game.Time);
   float split=road.Top+((road.Lanes+1)/2)*32;
   Rect(g,cream,road.CrosswalkLeft-11,road.Top+2,2,split-road.Top-4);Rect(g,cream,road.CrosswalkRight+9,split+2,2,road.Bottom-split-4);
   foreach(float y in new float[]{road.Top-7,road.Bottom+14})foreach(float x in new float[]{road.CrossX-44,road.CrossX+43}){
    Rect(g,ink,x,y-16,2,17);Rect(g,ink,x-4,y-39,10,26);
    SignalCircle(g,SignalPaint.RedOn(s)?SignalPaint.Red:SignalPaint.Off,x-2,y-37,6);
    SignalCircle(g,SignalPaint.YellowOn(s)?SignalPaint.Yellow:SignalPaint.Off,x-2,y-29,6);
    SignalCircle(g,s.Traffic==SignalColor.Green?SignalPaint.Green:SignalPaint.Off,x-2,y-21,6);
    Rect(g,ink,x+9,y-14,9,17);SignalCircle(g,!s.PedestrianGreen?SignalPaint.Red:SignalPaint.Off,x+11,y-12,5);SignalCircle(g,s.PedestrianLampOn?SignalPaint.Green:SignalPaint.Off,x+11,y-5,5);
   }
  }
  void DrawSignalHud(Graphics g){
   if(game.CompletedRoads>=CityMap.Streets.Length)return;Street road=CityMap.Streets[game.CompletedRoads];if(!road.HasLights)return;
   SignalState s=TrafficSignals.At(road,game.Time);Color color=s.PedestrianGreen?SignalPaint.Green:SignalPaint.Red;
   string action=s.PedestrianGreen?(s.Phase==SignalPhase.PedestrianFlashing?"Заканчивай переход":"Можно идти"):"Стой на тротуаре";
   Rect(g,ink,12,251,338,36);SignalCircle(g,color,20,259,9);TextAt(g,"Пешеходам: "+action,35,256,10,color,true);
   string traffic=s.Traffic==SignalColor.Green?"зелёный":s.Traffic==SignalColor.Yellow?"жёлтый":s.Traffic==SignalColor.RedYellow?"красный + жёлтый":"красный";
   TextAt(g,"Машинам: "+traffic+" · фаза ещё "+(int)Math.Ceiling(s.SecondsLeft)+" с",35,273,8,cream);
  }
 }
 sealed partial class PixelWorld {
  void DrawRoadSignals(Street road,Game game){
   if(!road.HasLights)return;SignalState s=TrafficSignals.At(road,game.Time);float split=road.Top+((road.Lanes+1)/2)*32;
   Ground(road.CrosswalkLeft-11,road.Top+2,road.CrosswalkLeft-9,split-2,.17f,Cream);Ground(road.CrosswalkRight+9,split+2,road.CrosswalkRight+11,road.Bottom-2,.17f,Cream);
   foreach(float y in new float[]{road.Top-7,road.Bottom+14})foreach(float x in new float[]{road.CrossX-44,road.CrossX+43}){
    Box(x,y,0,x+2,y+2,41,Ink);Box(x-3,y-2,25,x+5,y+4,49,Ink);
    Box(x-2,y-2.2f,42,x+4,y+4.2f,47,SignalPaint.RedOn(s)?SignalPaint.Red:SignalPaint.Off);
    Box(x-2,y-2.2f,34,x+4,y+4.2f,39,SignalPaint.YellowOn(s)?SignalPaint.Yellow:SignalPaint.Off);
    Box(x-2,y-2.2f,26,x+4,y+4.2f,31,s.Traffic==SignalColor.Green?SignalPaint.Green:SignalPaint.Off);
    Box(x+7,y-1,16,x+15,y+3,31,Ink);Box(x+8,y-1.2f,24,x+14,y+3.2f,29,!s.PedestrianGreen?SignalPaint.Red:SignalPaint.Off);
    Box(x+8,y-1.2f,17,x+14,y+3.2f,22,s.PedestrianLampOn?SignalPaint.Green:SignalPaint.Off);
   }
  }
 }
}
