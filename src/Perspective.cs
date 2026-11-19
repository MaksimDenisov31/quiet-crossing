using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 // The original simulation remains in road coordinates. Both perspective cameras
 // render that same world, including the exact car positions and crossing bounds.
 public sealed partial class Window {
  readonly PixelWorld world=new PixelWorld();
  float yaw;
  static string CameraName(CameraMode mode) {
   return mode==CameraMode.FirstPerson?"От первого лица":mode==CameraMode.ThirdPerson?"От третьего лица":"С высоты птичьего полёта";
  }
  static string CameraShort(CameraMode mode) {
   return mode==CameraMode.FirstPerson?"Первое лицо":mode==CameraMode.ThirdPerson?"Третье лицо":"Сверху";
  }
  void DrawPerspective(Graphics g) {
   world.Render(g,game,settings.Camera,yaw,carColors,page=="play"&&WalkingInput,page=="lost",peekYaw,lookPitch);
   if(settings.Camera==CameraMode.FirstPerson && page=="play") {
    // Small sleeves anchor the eye-level view without hiding the traffic.
    int bob=WalkingInput?(int)(Math.Sin(game.Time*10)*2):0;
    Rect(g,ink,29,286+bob,25,7);Rect(g,carColors[0],31,280+bob,21,10);
    Rect(g,Color.FromArgb(244,193,146),39,271+bob,10,11);
    Rect(g,ink,426,286-bob,25,7);Rect(g,carColors[0],428,280-bob,21,10);
    Rect(g,Color.FromArgb(244,193,146),430,271-bob,10,11);
   }
  }
 }
 struct Vertex {
  public float X,Y,Z;
  public Vertex(float x,float y,float z){X=x;Y=y;Z=z;}
 }
 sealed partial class PixelWorld : IDisposable {
  const int W=480,H=320;
  readonly int[] pixels=new int[W*H];
  readonly float[] depth=new float[W*H];
  readonly Bitmap frame=new Bitmap(W,H,PixelFormat.Format32bppArgb);
  readonly Vertex[] clipA=new Vertex[8],clipB=new Vertex[8];
  float cameraX,cameraY,cameraHeight,sy,cy,sp,cp;
  const float Focal=240,Near=2;
  static readonly Color Grass=Color.FromArgb(150,180,128),Road=Color.FromArgb(65,78,80),Cream=Color.FromArgb(226,223,197),Ink=Color.FromArgb(40,60,57);
  public void Render(Graphics graphics,Game game,CameraMode mode,float yaw,Color[] colors,bool walking,bool lost,float peek=0,float verticalLook=0) {
   sy=(float)Math.Sin(yaw+peek);cy=(float)Math.Cos(yaw+peek);
   bool third=mode==CameraMode.ThirdPerson;
   float pitch=(third?.35f:.12f)+verticalLook;sp=(float)Math.Sin(pitch);cp=(float)Math.Cos(pitch);
   cameraX=game.X-(third?(float)Math.Sin(yaw)*55:0);cameraY=game.Y+(third?(float)Math.Cos(yaw)*55:0);cameraHeight=third?46:19;
   Array.Clear(depth,0,depth.Length);
   for(int y=0;y<H;y++) {
    float t=y/(float)H;
    int color=Color.FromArgb(197+(int)(30*t),220+(int)(7*t),210-(int)(23*t)).ToArgb();
    for(int x=0;x<W;x++)pixels[y*W+x]=color;
   }
   DrawCity(game,colors);
   if(third)DrawPerson(game,colors[0],walking,lost,yaw);
   BitmapData data=frame.LockBits(new Rectangle(0,0,W,H),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
   try {Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{frame.UnlockBits(data);}
   graphics.DrawImageUnscaled(frame,0,0);
  }
  void Tree(float x,float y) {
   Ground(x-17,y-11,x+19,y+16,.04f,Color.FromArgb(124,157,113));
   Box(x,y,0,x+4,y+4,23,Color.FromArgb(106,100,76));
   Box(x-13,y-10,19,x+16,y+15,35,Color.FromArgb(65,108,85));
   Box(x-9,y-6,35,x+12,y+11,44,Color.FromArgb(110,151,106));
  }
  void DrawPerson(Game game,Color shirt,bool walking,bool lost,float facing) {
   float x=game.X,y=game.Y;
   Ground(x-7,y-4,x+7,y+5,.2f,Color.FromArgb(111,139,105));
   if(lost){AvatarBox(game,facing,x-10,y-4,1,x+3,y+4,6,shirt);AvatarBox(game,facing,x+3,y-3,1,x+10,y+3,7,Cream);return;}
   float stride=walking?(float)Math.Sin(game.Time*12)*2:0;
   AvatarBox(game,facing,x-4,y-2+stride,0,x-1,y+3+stride,8,Ink);AvatarBox(game,facing,x+1,y-2-stride,0,x+4,y+3-stride,8,Ink);
   AvatarBox(game,facing,x-5,y-3,8,x+5,y+3,17,shirt);
   AvatarBox(game,facing,x-7,y-2-stride,7,x-5,y+2-stride,14,Color.FromArgb(244,193,146));
   AvatarBox(game,facing,x+5,y-2+stride,7,x+7,y+2+stride,14,Color.FromArgb(244,193,146));
   AvatarBox(game,facing,x-4,y-3,17,x+4,y+3,24,Color.FromArgb(244,193,146));
   AvatarBox(game,facing,x-4.5f,y-3.5f,22,x+4.5f,y+3.5f,26,Ink);AvatarBox(game,facing,x-4.5f,y+1,19,x+4.5f,y+3.5f,23,Ink);
  }
  Vertex AvatarVertex(Game game,float facing,float x,float y,float h){float dx=x-game.X,dy=y-game.Y,s=(float)Math.Sin(facing),c=(float)Math.Cos(facing);return new Vertex(game.X+dx*c-dy*s,game.Y+dx*s+dy*c,h);}
  void AvatarBox(Game game,float facing,float x1,float y1,float h1,float x2,float y2,float h2,Color color){
   Vertex a=AvatarVertex(game,facing,x1,y1,h1),b=AvatarVertex(game,facing,x2,y1,h1),c=AvatarVertex(game,facing,x2,y2,h1),d=AvatarVertex(game,facing,x1,y2,h1);
   Vertex aa=AvatarVertex(game,facing,x1,y1,h2),bb=AvatarVertex(game,facing,x2,y1,h2),cc=AvatarVertex(game,facing,x2,y2,h2),dd=AvatarVertex(game,facing,x1,y2,h2);
   Quad(aa,bb,cc,dd,color);Quad(a,b,bb,aa,Shade(color,.82f));Quad(d,c,cc,dd,Shade(color,.94f));Quad(a,d,dd,aa,Shade(color,.73f));Quad(b,c,cc,bb,Shade(color,.88f));
  }
  static Color Shade(Color c,float factor) {return Color.FromArgb((int)(c.R*factor),(int)(c.G*factor),(int)(c.B*factor));}
  void Ground(float x1,float y1,float x2,float y2,float h,Color c) {
   Quad(new Vertex(x1,y1,h),new Vertex(x2,y1,h),new Vertex(x2,y2,h),new Vertex(x1,y2,h),c);
  }
  void Box(float x1,float y1,float h1,float x2,float y2,float h2,Color c) {
   Vertex a=new Vertex(x1,y1,h1),b=new Vertex(x2,y1,h1),cc=new Vertex(x2,y2,h1),d=new Vertex(x1,y2,h1);
   Vertex aa=new Vertex(x1,y1,h2),bb=new Vertex(x2,y1,h2),ccc=new Vertex(x2,y2,h2),dd=new Vertex(x1,y2,h2);
   // Axis-aligned boxes only need faces visible from the camera. Avoid shading
   // hidden walls/vehicle panels and their depth-buffer overdraw.
   if(cameraHeight>=h2)Quad(aa,bb,ccc,dd,c);
   else if(cameraHeight<h1)Quad(a,d,cc,b,Shade(c,.65f));
   if(cameraY<y1)Quad(a,b,bb,aa,Shade(c,.82f));
   if(cameraY>y2)Quad(d,cc,ccc,dd,Shade(c,.94f));
   if(cameraX<x1)Quad(a,d,dd,aa,Shade(c,.73f));
   if(cameraX>x2)Quad(b,cc,ccc,bb,Shade(c,.88f));
  }
  Vertex View(Vertex p) {
   float dx=p.X-cameraX,dy=p.Y-cameraY,up=p.Z-cameraHeight;
   float forward=dx*sy-dy*cy;
   return new Vertex(dx*cy+dy*sy,up*cp+forward*sp,forward*cp-up*sp);
  }
  void Quad(Vertex a,Vertex b,Vertex c,Vertex d,Color color) {
   clipA[0]=View(a);clipA[1]=View(b);clipA[2]=View(c);clipA[3]=View(d);
   int count=0;
   // Clip geometry to the near plane before projection, including objects beside
   // or behind the player. A depth buffer handles cars crossing in front of trees.
   Vertex previous=clipA[3];bool previousInside=previous.Z>=Near;
   for(int i=0;i<4;i++) {
    Vertex current=clipA[i];bool inside=current.Z>=Near;
    if(inside!=previousInside) {
     float t=(Near-previous.Z)/(current.Z-previous.Z);
     clipB[count++]=new Vertex(previous.X+(current.X-previous.X)*t,previous.Y+(current.Y-previous.Y)*t,Near);
    }
    if(inside)clipB[count++]=current;
    previous=current;previousInside=inside;
   }
   if(count<3)return;
   for(int i=0;i<count;i++){Vertex p=clipB[i];float inv=1/p.Z;clipB[i]=new Vertex(240+p.X*Focal*inv,145-p.Y*Focal*inv,inv);}
   for(int i=1;i<count-1;i++)Triangle(clipB[0],clipB[i],clipB[i+1],color.ToArgb());
  }
  void Triangle(Vertex a,Vertex b,Vertex c,int color) {
   float area=(b.X-a.X)*(c.Y-a.Y)-(b.Y-a.Y)*(c.X-a.X);
   if(Math.Abs(area)<.0001f)return;
   int left=Math.Max(0,(int)Math.Floor(Math.Min(a.X,Math.Min(b.X,c.X)))),right=Math.Min(W-1,(int)Math.Ceiling(Math.Max(a.X,Math.Max(b.X,c.X))));
   int top=Math.Max(0,(int)Math.Floor(Math.Min(a.Y,Math.Min(b.Y,c.Y)))),bottom=Math.Min(H-1,(int)Math.Ceiling(Math.Max(a.Y,Math.Max(b.Y,c.Y))));
   float inv=1/area;
   float dax=(b.Y-c.Y)*inv,day=(c.X-b.X)*inv,dbx=(c.Y-a.Y)*inv,dby=(a.X-c.X)*inv;
   float wa=((b.Y-c.Y)*(left+.5f-c.X)+(c.X-b.X)*(top+.5f-c.Y))*inv;
   float wb=((c.Y-a.Y)*(left+.5f-c.X)+(a.X-c.X)*(top+.5f-c.Y))*inv;
   for(int y=top;y<=bottom;y++,wa+=day,wb+=dby) {
    // Clip each scanline to its triangle span instead of testing the entire
    // bounding rectangle, especially expensive for long clipped road surfaces.
    float start=0,end=right-left;
    if(!ClipSpan(wa,dax,ref start,ref end)||!ClipSpan(wb,dbx,ref start,ref end)||!ClipSpan(1-wa-wb,-dax-dbx,ref start,ref end))continue;
    int first=left+(int)Math.Ceiling(start),last=left+(int)Math.Floor(end);
    float u=wa+(first-left)*dax,v=wb+(first-left)*dbx;
    for(int x=first;x<=last;x++,u+=dax,v+=dbx) {
     float w=1-u-v;
     float z=u*a.Z+v*b.Z+w*c.Z;int index=y*W+x;
     if(z>depth[index]){depth[index]=z;pixels[index]=color;}
    }
   }
  }
  static bool ClipSpan(float value,float slope,ref float start,ref float end){
   if(Math.Abs(slope)<1e-12f)return value>=-.00001f;
   float crossing=(-.00001f-value)/slope;
   if(slope>0)start=Math.Max(start,crossing);else end=Math.Min(end,crossing);
   return start<=end;
  }
  public void Dispose(){frame.Dispose();}
 }
}
