using System;

namespace QuietCrossing {
 public struct MapPoint {
  public readonly float X,Y;
  public MapPoint(float x,float y){X=x;Y=y;}
 }
 public sealed class Street {
  public readonly float Top,CrossX,Pace;
  public readonly int Lanes;
  public readonly string Name;
  public float Bottom {get{return Top+Lanes*32;}}
  public MapPoint Exit {get{return new MapPoint(CrossX,Top-35);}}
  public Street(string name,float top,int lanes,float crossX,float pace){Name=name;Top=top;Lanes=lanes;CrossX=crossX;Pace=pace;}
 }
 public sealed class House {
  public readonly float X,Y,Width,Depth,Height;
  public House(float x,float y,float w,float d,float h){X=x;Y=y;Width=w;Depth=d;Height=h;}
 }
 public static class CityMap {
  public const float Width=640,Height=1420;
  public static readonly MapPoint Start=new MapPoint(220,1350),Finish=new MapPoint(440,92);
  public static readonly Street[] Streets={
   new Street("Тихая улица",1160,2,220,.80f),new Street("Рыночная улица",920,3,420,.95f),
   new Street("Центральный проспект",660,4,170,1.12f),new Street("Парковая улица",440,2,340,.90f),
   new Street("Набережная",200,3,440,1.04f)
  };
  public static readonly MapPoint[] Path={
   Start,new MapPoint(220,1125),new MapPoint(420,1125),new MapPoint(420,885),
   new MapPoint(170,885),new MapPoint(170,625),new MapPoint(340,625),
   new MapPoint(340,405),new MapPoint(440,405),new MapPoint(440,165),Finish
  };
  public static readonly House[] Houses={
   new House(50,1280,75,40,38),new House(470,1285,90,45,46),new House(60,1050,80,42,43),new House(520,1060,80,44,38),
   new House(280,810,70,35,42),new House(40,820,70,40,35),new House(40,530,70,42,39),new House(480,550,90,48,48),
   new House(120,330,90,48,44),new House(515,340,70,50,40),new House(80,55,100,55,44),new House(525,50,70,65,50)
  };
  public static readonly MapPoint[] Trees={
   new MapPoint(32,1360),new MapPoint(122,1380),new MapPoint(370,1330),new MapPoint(590,1360),
   new MapPoint(30,1110),new MapPoint(125,1140),new MapPoint(505,1130),new MapPoint(600,1048),
   new MapPoint(28,885),new MapPoint(100,905),new MapPoint(510,858),new MapPoint(600,900),
   new MapPoint(28,620),new MapPoint(120,648),new MapPoint(440,630),new MapPoint(590,620),
   new MapPoint(30,400),new MapPoint(245,398),new MapPoint(495,405),new MapPoint(600,410),
   new MapPoint(32,140),new MapPoint(210,125),new MapPoint(320,145),new MapPoint(600,145)
  };
  public static bool OnRoad(float y){foreach(Street s in Streets)if(y>=s.Top&&y<=s.Bottom)return true;return false;}
  public static bool CanStand(float x,float y) {
   if(x<12||x>Width-12||y<24||y>Height-12)return false;
   foreach(House h in Houses)if(x+4>h.X&&x-4<h.X+h.Width&&y>h.Y&&y-6<h.Y+h.Depth)return false;
   return true;
  }
  public static float Distance(MapPoint a,MapPoint b){return Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);}
  public static float TotalDistance{get{float total=0;for(int i=1;i<Path.Length;i++)total+=Distance(Path[i-1],Path[i]);return total;}}
 }
}
