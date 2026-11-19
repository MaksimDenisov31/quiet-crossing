using System;

namespace QuietCrossing {
 static class PlayerControls {
  public const float TurnSpeed=2.1f;
  public static float Wrap(float angle){float full=(float)(Math.PI*2);angle%=full;if(angle>(float)Math.PI)angle-=full;if(angle<-(float)Math.PI)angle+=full;return angle;}
  public static MapPoint Movement(CameraMode mode,float yaw,float forward,float strafe){
   if(mode==CameraMode.Bird)return new MapPoint(strafe,-forward);
   float sin=(float)Math.Sin(yaw),cos=(float)Math.Cos(yaw);
   return new MapPoint(strafe*cos+forward*sin,strafe*sin-forward*cos);
  }
  public static string Compass(float yaw){string[] names={"Север","Восток","Юг","Запад"};int i=((int)Math.Floor((yaw+Math.PI/4)/(Math.PI/2))%4+4)%4;return names[i];}
 }
}
