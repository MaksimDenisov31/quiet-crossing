using System;
using System.Collections.Generic;

namespace QuietCrossing {
 public struct MapPoint {public readonly float X,Y;public MapPoint(float x,float y){X=x;Y=y;}}
 public sealed class Street {
  public readonly float Top,CrossX,Pace;public readonly int Lanes,District;public readonly string Name;
  public float Bottom{get{return Top+Lanes*32;}}
  public MapPoint Exit{get{return new MapPoint(CrossX,Top-35);}}
  public Street(string name,float top,int lanes,float crossX,float pace,int district){Name=name;Top=top;Lanes=lanes;CrossX=crossX;Pace=pace;District=district;}
 }
 public sealed class House {
  public readonly float X,Y,Width,Depth,Height;
  public House(float x,float y,float w,float d,float h){X=x;Y=y;Width=w;Depth=d;Height=h;}
 }
 public static class CityMap {
  public const float Width=1280,Height=82000;
  public const int StreetsPerDistrict=5;
  public static readonly string[] Districts={"Стартовый квартал","Старый город","Солнечный район","Рыночный квартал","Городские сады","Университет","Мастерские","Новый город","Сосновый парк","Район каналов","Прибрежный район","Набережная"};
  public static readonly MapPoint Start=new MapPoint(220,81920),Finish=new MapPoint(370,92);
  public static readonly Street[] Streets;
  public static readonly MapPoint[] Path;
  public static readonly House[] Houses;
  public static readonly MapPoint[] Trees;
  // This is a lower bound even if every road and obstacle is removed and all
  // diagonal shortcuts are allowed. The finish tolerance is included.
  public static double MinimumSeconds{get{return (Start.Y-Finish.Y-20)/Game.MoveSpeed;}}
  static CityMap(){
   float[] crossings={220,930,480,1060,370};int[] laneCounts={2,3,4,2,3};float[] paces={.8f,.95f,1.12f,.9f,1.04f};
   string[] names={"Тихая улица","Торговая улица","Центральный проспект","Зелёная улица","Прибрежная улица"};
   var roads=new List<Street>();var route=new List<MapPoint>();var houses=new List<House>();var trees=new List<MapPoint>();route.Add(Start);
   for(int i=0;i<Districts.Length*StreetsPerDistrict;i++){
    int district=i/StreetsPerDistrict,k=i%StreetsPerDistrict;float top=81720-i*1344;
    var road=new Street(names[k]+" · "+(district+1),top,laneCounts[k],crossings[k],paces[k],district);roads.Add(road);
    if(i>0)route.Add(new MapPoint(road.CrossX,roads[i-1].Exit.Y));route.Add(road.Exit);
    // Buildings avoid every crossing corridor, so the marked route remains open.
    for(int row=0;row<3;row++)for(int col=0;col<3;col++){
     float x=col==0?55:col==1?610:1140,y=top-210-row*390;
     houses.Add(new House(x,y,70+(i+col)%3*8,48+(i+row)%3*7,32+(i+row+col)%4*7));
    }
    foreach(float x in new float[]{30,150,555,760,1240})for(int row=0;row<3;row++)trees.Add(new MapPoint(x,top-120-row*400));
   }
   route.Add(Finish);Streets=roads.ToArray();Path=route.ToArray();Houses=houses.ToArray();Trees=trees.ToArray();
  }
  public static int DistrictIndex(int completed){return Math.Min(Districts.Length-1,completed/StreetsPerDistrict);}
  public static bool OnRoad(float y){foreach(Street s in Streets)if(y>=s.Top&&y<=s.Bottom)return true;return false;}
  public static bool CanStand(float x,float y){
   if(x<12||x>Width-12||y<24||y>Height-12)return false;
   foreach(House h in Houses)if(y>h.Y&&y-6<h.Y+h.Depth&&x+4>h.X&&x-4<h.X+h.Width)return false;
   return true;
  }
  public static float Distance(MapPoint a,MapPoint b){return Math.Abs(a.X-b.X)+Math.Abs(a.Y-b.Y);}
  public static float TotalDistance{get{float total=0;for(int i=1;i<Path.Length;i++)total+=Distance(Path[i-1],Path[i]);return total;}}
 }
}
