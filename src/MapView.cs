using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace QuietCrossing {
 public sealed partial class Window {
  string mapReturn="play";
  bool mapLocal;
  void OpenMap(){mapReturn=page=="pause"?"pause":"play";Go("map");}
  void DrawScene(Graphics g) {
   Rect(g,Color.FromArgb(150,180,128),0,0,480,320);
   float ox=Math.Max(0,Math.Min(CityMap.Width-480,game.X-240));
   float oy=Math.Max(0,Math.Min(CityMap.Height-320,game.Y-205));
   GraphicsState state=g.Save();g.TranslateTransform(-(int)ox,-(int)oy);
   for(int i=0;i<600;i++){
    int x=(i*113+23)%(int)CityMap.Width,y=(int)(oy/300)*300+(i*71+13)%600-100;
    if(!CityMap.OnRoad(y))Rect(g,i%4==0?Color.FromArgb(217,207,153):Color.FromArgb(132,164,117),x,y,3,2);
   }
   for(int i=1;i<CityMap.Path.Length;i++){
    MapPoint a=CityMap.Path[i-1],b=CityMap.Path[i];
    if(Math.Max(a.Y,b.Y)<oy-30||Math.Min(a.Y,b.Y)>oy+350)continue;
    Rect(g,Color.FromArgb(199,195,160),Math.Min(a.X,b.X)-15,Math.Min(a.Y,b.Y)-15,Math.Abs(b.X-a.X)+30,Math.Abs(b.Y-a.Y)+30);
   }
   foreach(Street road in CityMap.Streets){
    if(road.Bottom<oy-40||road.Top>oy+360)continue;
    Rect(g,Color.FromArgb(194,197,171),0,road.Top-20,CityMap.Width,road.Bottom-road.Top+40);
    Rect(g,cream,0,road.Top-3,CityMap.Width,3);Rect(g,cream,0,road.Bottom,CityMap.Width,3);
    Rect(g,Color.FromArgb(65,78,80),0,road.Top,CityMap.Width,road.Bottom-road.Top);
    for(int lane=1;lane<road.Lanes;lane++)for(int x=0;x<CityMap.Width;x+=29)if(x<road.CrossX-36||x>road.CrossX+30)
     Rect(g,lane==road.Lanes/2?Color.FromArgb(203,181,119):Color.FromArgb(122,135,130),x,road.Top+lane*32,15,1);
    for(float y=road.Top+5;y<road.Bottom-4;y+=16)Rect(g,cream,road.CrosswalkLeft,y,CityMap.CrosswalkHalfWidth*2,9);
    foreach(float y in new float[]{road.Top-12,road.Bottom+8}){
     Rect(g,Color.FromArgb(216,196,128),road.CrossX-22,y,44,6);
     for(int x=-20;x<22;x+=6)Rect(g,Color.FromArgb(170,153,101),road.CrossX+x,y+1,2,2);
    }
    foreach(float x in new float[]{road.CrossX-43,road.CrossX+42}){
     Rect(g,ink,x,road.Top-28,2,25);Rect(g,Color.FromArgb(102,151,165),x-6,road.Top-32,14,13);
     Rect(g,cream,x-3,road.Top-29,8,7);Rect(g,ink,x,road.Top-28,2,5);
    }
   }
   for(int i=1;i<CityMap.Path.Length;i++){
    MapPoint a=CityMap.Path[i-1],b=CityMap.Path[i];float length=CityMap.Distance(a,b);
    if(Math.Max(a.Y,b.Y)<oy-30||Math.Min(a.Y,b.Y)>oy+350)continue;
    for(float t=0;t<length;t+=14){float x=a.X+(b.X-a.X)*t/length,y=a.Y+(b.Y-a.Y)*t/length;if(!CityMap.OnRoad(y))Rect(g,cream,x-1,y-1,3,3);}
   }
   foreach(House h in CityMap.Houses){
    if(h.Y+h.Depth<oy-30||h.Y>oy+350)continue;
    Rect(g,Color.FromArgb(124,157,113),h.X+3,h.Y+5,h.Width,h.Depth);
    Rect(g,Color.FromArgb(194,192,162),h.X,h.Y,h.Width,h.Depth);
    Rect(g,Color.FromArgb(112,136,119),h.X-2,h.Y-3,h.Width+4,h.Depth-6);
    Rect(g,Color.FromArgb(141,158,130),h.X+3,h.Y+1,h.Width-6,3);
    for(int i=0;i<3;i++)Rect(g,Color.FromArgb(108,145,147),h.X+8+i*18,h.Y+h.Depth-6,8,5);
   }
   foreach(MapPoint t in CityMap.Trees)if(t.Y>oy-50&&t.Y<oy+370)Tree(g,(int)t.X,(int)t.Y);
   for(int i=0;i<CityMap.Streets.Length;i++){
    Street street=CityMap.Streets[i];MapPoint p=street.Exit;Color color=i<game.CompletedRoads?mint:i==game.CompletedRoads?cream:Color.FromArgb(191,205,162);
    if(p.Y<oy-50||p.Y>oy+370)continue;
    using(var b=new SolidBrush(ink))g.FillEllipse(b,p.X-11,p.Y-11,22,22);
    using(var b=new SolidBrush(color))g.FillEllipse(b,p.X-8,p.Y-8,16,16);
    string marker=i<game.CompletedRoads?"✓":(i+1).ToString();TextAt(g,marker,p.X-marker.Length*2,p.Y-6,marker.Length>2?7:9,ink,true);
    TextAt(g,street.Name,p.X-55,p.Y-32,8,ink,true);
   }
   DrawEndpoint(g,CityMap.Start,"А · СТАРТ");DrawEndpoint(g,CityMap.Finish,"Б · ФИНИШ");
   foreach(Car c in game.Cars){
    if(c.Y<oy-20||c.Y>oy+340)continue;
    int x=(int)(c.X-c.Width/2),y=(int)c.Y,w=(int)c.Width;Color color=carColors[c.Color];
    Rect(g,Color.FromArgb(47,61,63),x+1,y+6,w,5);Rect(g,ink,x+4,y-10,6,3);Rect(g,ink,x+w-10,y-10,6,3);Rect(g,ink,x+4,y+7,6,3);Rect(g,ink,x+w-10,y+7,6,3);
    Rect(g,color,x,y-7,w,15);Rect(g,color,x+2,y-9,w-4,19);Rect(g,Color.FromArgb(64,93,100),x+8,y-6,w-16,12);Rect(g,color,x+12,y-7,w-24,14);
    Rect(g,Color.FromArgb(175,207,200),c.Speed>0?x+w-9:x+5,y-5,3,10);Rect(g,cream,c.Speed>0?x+w-2:x,y-5,2,3);Rect(g,cream,c.Speed>0?x+w-2:x,y+3,2,3);
   }
   Person(g,game.X,game.Y,page=="lost");g.Restore(state);
  }
  void DrawEndpoint(Graphics g,MapPoint p,string label){
   Rect(g,mint,p.X-18,p.Y-17,36,34);Rect(g,cream,p.X-15,p.Y-14,30,28);
   TextAt(g,label,p.X-26,p.Y-36,8,ink,true);
  }
  void DrawMapDiagram(Graphics g,RectangleF box,bool detailed){
   const float viewHeight=1200;
   float viewTop=Math.Max(0,Math.Min(CityMap.Height-viewHeight,game.Y-750));
   float scale=Math.Min(box.Width/CityMap.Width,box.Height/viewHeight);
   float width=CityMap.Width*scale,height=viewHeight*scale;
   float ox=box.X+(box.Width-width)/2,oy=box.Y+(box.Height-height)/2;
   GraphicsState state=g.Save();g.SetClip(box,CombineMode.Intersect);
   Rect(g,Color.FromArgb(142,171,122),ox,oy,width,height);
   foreach(Street road in CityMap.Streets){
    if(road.Bottom<viewTop||road.Top>viewTop+viewHeight)continue;
    Rect(g,Color.FromArgb(65,78,80),ox,oy+(road.Top-viewTop)*scale,width,(road.Bottom-road.Top)*scale);
    Rect(g,cream,ox+road.CrosswalkLeft*scale,oy+(road.Top-viewTop)*scale,CityMap.CrosswalkHalfWidth*2*scale,(road.Bottom-road.Top)*scale);
   }
   foreach(House h in CityMap.Houses)if(h.Y+h.Depth>=viewTop&&h.Y<=viewTop+viewHeight)Rect(g,Color.FromArgb(98,129,103),ox+h.X*scale,oy+(h.Y-viewTop)*scale,h.Width*scale,h.Depth*scale);
   using(var pen=new Pen(Color.FromArgb(234,223,170),detailed?2:1))for(int i=1;i<CityMap.Path.Length;i++){
    MapPoint a=CityMap.Path[i-1],b=CityMap.Path[i];if(Math.Max(a.Y,b.Y)<viewTop||Math.Min(a.Y,b.Y)>viewTop+viewHeight)continue;
    g.DrawLine(pen,ox+a.X*scale,oy+(a.Y-viewTop)*scale,ox+b.X*scale,oy+(b.Y-viewTop)*scale);
   }
   for(int i=0;i<CityMap.Streets.Length;i++){
    MapPoint p=CityMap.Streets[i].Exit;if(p.Y<viewTop||p.Y>viewTop+viewHeight)continue;
    Color c=i<game.CompletedRoads?mint:i==game.CompletedRoads?Color.FromArgb(246,201,104):ink;Rect(g,c,ox+p.X*scale-2,oy+(p.Y-viewTop)*scale-2,4,4);
   }
   foreach(MapPoint p in new MapPoint[]{CityMap.Start,CityMap.Finish})if(p.Y>=viewTop&&p.Y<=viewTop+viewHeight)TextAt(g,p.Y==CityMap.Start.Y?"А":"Б",ox+p.X*scale+4,oy+(p.Y-viewTop)*scale-5,8,ink,true);
   Rect(g,ink,ox+game.X*scale-3,oy+(game.Y-viewTop)*scale-3,6,6);Rect(g,cream,ox+game.X*scale-2,oy+(game.Y-viewTop)*scale-2,4,4);
   if(settings.Camera!=CameraMode.Bird)using(var pen=new Pen(cream,detailed?2:1))g.DrawLine(pen,ox+game.X*scale,oy+(game.Y-viewTop)*scale,ox+game.X*scale+(float)Math.Sin(yaw)*9,oy+(game.Y-viewTop)*scale-(float)Math.Cos(yaw)*9);
   if(settings.Camera!=CameraMode.Bird&&Math.Abs(peekYaw)>.02f)using(var pen=new Pen(Color.FromArgb(246,201,104),detailed?2:1))g.DrawLine(pen,ox+game.X*scale,oy+(game.Y-viewTop)*scale,ox+game.X*scale+(float)Math.Sin(yaw+peekYaw)*11,oy+(game.Y-viewTop)*scale-(float)Math.Cos(yaw+peekYaw)*11);
   g.Restore(state);
  }
  void DrawCampaignDiagram(Graphics g,RectangleF box){
   int active=CityMap.DistrictIndex(game.CompletedRoads);PointF previous=PointF.Empty;
   for(int i=0;i<CityMap.Districts.Length;i++){
    PointF point=new PointF(box.X+45+(i%2)*65,box.Bottom-17-i*(box.Height-34)/11);
    if(i>0)using(var pen=new Pen(i<=active?mint:Color.FromArgb(112,136,119),2))g.DrawLine(pen,previous,point);
    Color c=i<active?mint:i==active?Color.FromArgb(246,201,104):Color.FromArgb(112,136,119);
    Rect(g,ink,point.X-8,point.Y-8,16,16);Rect(g,c,point.X-6,point.Y-6,12,12);
    TextAt(g,(i+1).ToString(),point.X-(i>=9?5:3),point.Y-5,7,ink,true);
    if(i==active){Rect(g,cream,point.X+12,point.Y-2,4,4);TextAt(g,"ТЫ",point.X+18,point.Y-5,7,cream);}
    if(i==0)TextAt(g,"А",point.X-20,point.Y-5,8,cream,true);
    if(i==CityMap.Districts.Length-1)TextAt(g,"Б",point.X-20,point.Y-5,8,cream,true);
    previous=point;
   }
  }
  void DrawRouteHud(Graphics g){
   Rect(g,ink,12,48,259,39);
   TextAt(g,"А → Б   "+game.CompletedRoads+" / "+CityMap.Streets.Length+"   "+(int)(game.Progress*100)+"%   "+Game.Duration(game.Time),20,53,9,cream,true);
   TextAt(g,"Далее: "+game.TargetName,20,69,8,mint);
   if(settings.Camera!=CameraMode.Bird){Rect(g,ink,12,95,101,17);TextAt(g,"Курс: "+PlayerControls.Compass(yaw),18,98,8,cream);if(Math.Abs(peekYaw)>.02f){Rect(g,ink,12,113,101,17);TextAt(g,"Взгляд: "+PlayerControls.Compass(yaw+peekYaw),18,116,8,mint);}}
   Rect(g,Color.FromArgb(76,96,85),12,87,259,3);Rect(g,mint,12,87,259*game.Progress,3);
   Rect(g,ink,391,100,81,153);TextAt(g,"Район "+(CityMap.DistrictIndex(game.CompletedRoads)+1)+" / 12",397,105,7,mint);DrawMapDiagram(g,new RectangleF(397,119,69,124),false);
   Button(g,"Карта [M]",391,260,81,OpenMap);
  }
  void DrawMapPage(Graphics g){
   Rect(g,ink,0,0,480,320);Center(g,"КАРТА МАРШРУТА",15,21,cream,true);
   Rect(g,Color.FromArgb(76,96,85),20,53,190,230);
   if(mapLocal)DrawMapDiagram(g,new RectangleF(32,64,166,205),true);else DrawCampaignDiagram(g,new RectangleF(32,64,166,205));
   int district=CityMap.DistrictIndex(game.CompletedRoads);
   TextAt(g,(district+1)+" / 12 · "+CityMap.Districts[district],233,60,12,cream,true);
   TextAt(g,Game.Duration(game.Time)+" · "+game.CompletedRoads+" / "+CityMap.Streets.Length+" · "+(int)(game.Progress*100)+"%",233,84,9,mint);
   int listStart=Math.Min(CityMap.Streets.Length-5,game.CompletedRoads/5*5);
   for(int i=listStart;i<Math.Min(CityMap.Streets.Length,listStart+5);i++){
    Color c=i<game.CompletedRoads?mint:i==game.CompletedRoads?Color.FromArgb(246,201,104):Color.FromArgb(144,165,145);
    TextAt(g,(i<game.CompletedRoads?"✓ ":(i+1)+". ")+CityMap.Streets[i].Name,233,109+(i-listStart)*22,9,c,i==game.CompletedRoads);
   }
   TextAt(g,"Короткие тротуары · 2–5 полос",233,221,9,mint);
   Button(g,mapLocal?"Весь путь":"Район",233,253,107,delegate{mapLocal=!mapLocal;});
   Button(g,"Продолжить",346,253,112,delegate{Go(mapReturn);},true);
   Center(g,"M / Esc — закрыть карту. Игра на паузе.",296,9,mint);
  }
 }
}
