using System;

namespace QuietCrossing {
 public enum SignalColor{Green,Yellow,Red,RedYellow}
 public enum SignalPhase{TrafficGreen,TrafficYellow,ClearRoad,PedestrianGreen,PedestrianFlashing,ClearPedestrians,RedYellow}
 public struct SignalState {
  public SignalPhase Phase;public SignalColor Traffic;public bool PedestrianGreen,PedestrianLampOn;public float SecondsLeft;
 }
 public static class TrafficSignals {
  public const float Cycle=45;
  public static SignalState At(Street street,double time){
   double t=(time+street.Index*7)%Cycle;if(t<0)t+=Cycle;
   var s=new SignalState();double end;
   if(t<14){s.Phase=SignalPhase.TrafficGreen;s.Traffic=SignalColor.Green;end=14;}
   else if(t<17){s.Phase=SignalPhase.TrafficYellow;s.Traffic=SignalColor.Yellow;end=17;}
   else if(t<27){s.Phase=SignalPhase.ClearRoad;s.Traffic=SignalColor.Red;end=27;}
   else if(t<37){s.Phase=SignalPhase.PedestrianGreen;s.Traffic=SignalColor.Red;s.PedestrianGreen=true;end=37;}
   else if(t<40){s.Phase=SignalPhase.PedestrianFlashing;s.Traffic=SignalColor.Red;s.PedestrianGreen=true;end=40;}
   else if(t<44){s.Phase=SignalPhase.ClearPedestrians;s.Traffic=SignalColor.Red;end=44;}
   else{s.Phase=SignalPhase.RedYellow;s.Traffic=SignalColor.RedYellow;end=45;}
   s.PedestrianLampOn=s.PedestrianGreen&&(s.Phase!=SignalPhase.PedestrianFlashing||((int)(t*4)%2)==0);
   s.SecondsLeft=(float)(end-t);return s;
  }
 }
}
