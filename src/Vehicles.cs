using System;
using System.Drawing;

namespace QuietCrossing {
 public enum VehicleKind{Sedan,Hatchback,Suv,Van,Taxi,Bus,BoxTruck,SemiTruck,DumpTruck,Police,Ambulance,FireEngine,Utility,TowTruck,Motorcycle,Bicycle,MotorcadeEscort,SecuritySuv,PresidentialLimousine}
 public sealed class VehicleSpec {
  public readonly string Name;public readonly float Length,Depth,Height,SpeedFactor;
  public VehicleSpec(string name,float length,float depth,float height,float speed){Name=name;Length=length;Depth=depth;Height=height;SpeedFactor=speed;}
 }
 public static class Vehicles {
  static readonly VehicleSpec[] specs={
   new VehicleSpec("Седан",29,14,18,1),new VehicleSpec("Хэтчбек",25,13,17,1),new VehicleSpec("Внедорожник",35,16,22,.95f),new VehicleSpec("Фургон",39,15,26,.9f),
   new VehicleSpec("Такси",30,14,18,1),new VehicleSpec("Автобус",62,17,30,.8f),new VehicleSpec("Грузовик",54,17,32,.8f),new VehicleSpec("Фура",76,18,34,.75f),
   new VehicleSpec("Самосвал",53,18,29,.75f),new VehicleSpec("Полиция",31,14,19,1.05f),new VehicleSpec("Скорая помощь",43,16,28,1),new VehicleSpec("Пожарная машина",58,18,29,.85f),
   new VehicleSpec("Дорожная служба",40,16,25,.85f),new VehicleSpec("Эвакуатор",51,17,23,.85f),new VehicleSpec("Мотоциклист",19,6,19,1.05f),new VehicleSpec("Велосипедист",18,5,21,.5f),
   new VehicleSpec("Сопровождение",33,15,20,1),new VehicleSpec("Охрана · SUV",37,17,24,1),new VehicleSpec("Президентский лимузин",58,16,21,1)
  };
  public static VehicleSpec Spec(VehicleKind kind){return specs[(int)kind];}
  public static bool Rider(VehicleKind kind){return kind==VehicleKind.Bicycle||kind==VehicleKind.Motorcycle;}
  public static bool Beacon(VehicleKind kind){return kind==VehicleKind.Police||kind==VehicleKind.Ambulance||kind==VehicleKind.FireEngine||kind==VehicleKind.Utility||kind==VehicleKind.TowTruck||kind==VehicleKind.MotorcadeEscort||kind==VehicleKind.SecuritySuv;}
  public static bool Amber(VehicleKind kind){return kind==VehicleKind.Utility||kind==VehicleKind.TowTruck;}
  public static VehicleKind RandomKind(Random random){
   int n=random.Next(100);
   if(n<23)return VehicleKind.Sedan;if(n<35)return VehicleKind.Hatchback;if(n<44)return VehicleKind.Suv;if(n<52)return VehicleKind.Van;if(n<59)return VehicleKind.Taxi;
   if(n<65)return VehicleKind.BoxTruck;if(n<69)return VehicleKind.SemiTruck;if(n<73)return VehicleKind.Bus;if(n<76)return VehicleKind.DumpTruck;if(n<80)return VehicleKind.Police;
   if(n<83)return VehicleKind.Ambulance;if(n<85)return VehicleKind.FireEngine;if(n<88)return VehicleKind.Utility;if(n<90)return VehicleKind.TowTruck;if(n<95)return VehicleKind.Motorcycle;return VehicleKind.Bicycle;
  }
  public static Color Paint(VehicleKind kind,Color civilian){
   switch(kind){
    case VehicleKind.Taxi:return Color.FromArgb(231,190,83);
    case VehicleKind.Police:return Color.FromArgb(219,230,222);
    case VehicleKind.Ambulance:return Color.FromArgb(241,232,209);
    case VehicleKind.FireEngine:return Color.FromArgb(201,91,75);
    case VehicleKind.Utility:case VehicleKind.TowTruck:return Color.FromArgb(221,171,77);
    case VehicleKind.MotorcadeEscort:return Color.FromArgb(46,64,77);
    case VehicleKind.SecuritySuv:case VehicleKind.PresidentialLimousine:return Color.FromArgb(38,47,52);
    default:return civilian;
   }
  }
 }
}
