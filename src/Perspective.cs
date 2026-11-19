using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace QuietCrossing {
 // The original simulation remains in road coordinates. Both perspective cameras
 // render that same world, including the exact car positions and crossing bounds.
 public sealed partial class Window {
  readonly PixelWorld world=new PixelWorld();
  float lookAngle;
  static string CameraName(CameraMode mode) {
   return mode==CameraMode.FirstPerson?"От первого лица":mode==CameraMode.ThirdPerson?"От третьего лица":"С высоты птичьего полёта";
  }
  static string CameraShort(CameraMode mode) {
   return mode==CameraMode.FirstPerson?"Первое лицо":mode==CameraMode.ThirdPerson?"Третье лицо":"Сверху";
  }
  void DrawPerspective(Graphics g) {
   world.Render(g,game,settings.Camera,lookAngle,carColors,page=="play"&&keys.Count>0,page=="lost");
   if(settings.Camera==CameraMode.FirstPerson && page=="play") {
    // Small sleeves anchor the eye-level view without hiding the traffic.
    int bob=keys.Count>0?(int)(Math.Sin(game.Time*10)*2):0;
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
 sealed class PixelWorld : IDisposable {
  const int W=480,H=320;
  readonly int[] pixels=new int[W*H];
  readonly float[] depth=new float[W*H];
  readonly Bitmap frame=new Bitmap(W,H,PixelFormat.Format32bppArgb);
  readonly Vertex[] clipA=new Vertex[8],clipB=new Vertex[8];
  float cameraX,cameraY,cameraHeight,sy,cy,sp,cp;
  const float Focal=240,Near=2;
  static readonly Color Grass=Color.FromArgb(150,180,128),Road=Color.FromArgb(65,78,80),Cream=Color.FromArgb(226,223,197),Ink=Color.FromArgb(40,60,57);
  public void Render(Graphics graphics,Game game,CameraMode mode,float yaw,Color[] colors,bool walking,bool lost) {
   sy=(float)Math.Sin(yaw);cy=(float)Math.Cos(yaw);
   bool third=mode==CameraMode.ThirdPerson;
   float pitch=third?.35f:.12f;sp=(float)Math.Sin(pitch);cp=(float)Math.Cos(pitch);
   cameraX=game.X-(third?sy*55:0);cameraY=game.Y+(third?cy*55:0);cameraHeight=third?46:19;
   Array.Clear(depth,0,depth.Length);
   for(int y=0;y<H;y++) {
    float t=y/(float)H;
    int color=Color.FromArgb(197+(int)(30*t),220+(int)(7*t),210-(int)(23*t)).ToArgb();
    for(int x=0;x<W;x++)pixels[y*W+x]=color;
   }
   Ground(-1800,-1600,1800,1800,-.05f,Grass);
   Ground(-1600,98,1600,230,0,Road);
   Ground(-1600,77,1600,98,.05f,Color.FromArgb(194,197,171));
   Ground(-1600,230,1600,251,.05f,Color.FromArgb(194,197,171));
   Ground(-1600,78,1600,81,.1f,Cream);Ground(-1600,232,1600,235,.1f,Cream);
   for(int lane=1;lane<4;lane++)for(int x=-800;x<1200;x+=29)if(x<204||x>273)
    Ground(x,99+lane*32,x+15,100+lane*32,.12f,lane==2?Color.FromArgb(203,181,119):Color.FromArgb(122,135,130));
   for(int y=103;y<228;y+=16)Ground(216,y,264,y+9,.15f,Cream);
   Ground(218,86,262,92,.15f,Color.FromArgb(216,196,128));Ground(218,239,262,245,.15f,Color.FromArgb(216,196,128));
   for(int i=0;i<110;i++) {
    int x=(i*113+23)%700-110,y=(i*71+13)%450-70;
    if(y<74||y>254)Ground(x,y,x+3,y+2,.02f,i%4==0?Color.FromArgb(217,207,153):Color.FromArgb(132,164,117));
   }
   // Quiet houses and a treeline give eye-level views a readable horizon.
   for(int i=0;i<9;i++) {
    float x=-180+i*108;Color wall=i%2==0?Color.FromArgb(194,192,162):Color.FromArgb(171,187,161);
    Box(x,-115,0,x+63,-70,38+(i%3)*9,wall);
    Box(x-2,-117,38+(i%3)*9,x+65,-68,43+(i%3)*9,Color.FromArgb(112,136,119));
    for(int j=0;j<3;j++)Box(x+8+j*18,-69.8f,17,x+16+j*18,-69,28,Color.FromArgb(108,145,147));
   }
   foreach(int x in new int[]{-130,-40,35,100,407,450,545,630}) {Tree(x,42);Tree(x+18,290);}
   foreach(int x in new int[]{194,283}) {
    Box(x,89,0,x+2,91,30,Ink);Box(x-6,88,23,x+8,90,36,Color.FromArgb(102,151,165));
    Box(x-3,90.1f,26,x+5,90.3f,33,Cream);Box(x,90.4f,27,x+2,90.6f,32,Ink);
   }
   Box(115,56,7,144,64,10,Color.FromArgb(138,109,80));
   Box(118,58,0,120,62,7,Ink);Box(139,58,0,141,62,7,Ink);
   // Two small flags mark the same finish line as the top-down view.
   foreach(int x in new int[]{213,267}) {
    Box(x,65,0,x+1.5f,67,29,Ink);
    Box(x+1.5f,65,20,x+13,66,29,Color.FromArgb(196,223,172));
    for(int i=0;i<3;i++)for(int j=0;j<2;j++)if((i+j)%2==0)Box(x+2+i*3.5f,66.1f,21+j*3.5f,x+5.5f+i*3.5f,66.3f,24.5f+j*3.5f,Ink);
   }
   foreach(Car car in game.Cars)DrawCar(car,colors[car.Color]);
   if(third)DrawPerson(game,colors[0],walking,lost);
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
  void DrawCar(Car car,Color color) {
   float x=car.X,y=car.Y,w=car.Width;
   Ground(x-w/2-1,y-9,x+w/2+2,y+10,.18f,Color.FromArgb(50,66,64));
   foreach(float axle in new float[]{x-w/2+6,x+w/2-6}) {
    Box(axle-3,y-9,1,axle+3,y-6,6,Ink);Box(axle-3,y+6,1,axle+3,y+9,6,Ink);
   }
   Box(x-w/2,y-7,4,x+w/2,y+7,11,color);
   Box(x-w*.23f,y-6,11,x+w*.23f,y+6,17,Color.FromArgb(64,93,100));
   Box(x-w*.15f,y-6.2f,11,x+w*.15f,y+6.2f,17,color);
   Box(x-w*.25f,y-6.5f,17,x+w*.25f,y+6.5f,18,color);
   float front=x+(car.Speed>0?w/2:-w/2);
   foreach(float yy in new float[]{y-5,y+3})Box(front-.3f,yy,6,front+.3f,yy+2,9,Cream);
   float back=x+(car.Speed>0?-w/2:w/2);
   foreach(float yy in new float[]{y-5,y+3})Box(back-.3f,yy,6,back+.3f,yy+2,8,Color.FromArgb(182,81,69));
  }
  void DrawPerson(Game game,Color shirt,bool walking,bool lost) {
   float x=game.X,y=game.Y;
   Ground(x-7,y-4,x+7,y+5,.2f,Color.FromArgb(111,139,105));
   if(lost){Box(x-10,y-4,1,x+3,y+4,6,shirt);Box(x+3,y-3,1,x+10,y+3,7,Cream);return;}
   float stride=walking?(float)Math.Sin(game.Time*12)*2:0;
   Box(x-4,y-2+stride,0,x-1,y+3+stride,8,Ink);Box(x+1,y-2-stride,0,x+4,y+3-stride,8,Ink);
   Box(x-5,y-3,8,x+5,y+3,17,shirt);
   Box(x-7,y-2-stride,7,x-5,y+2-stride,14,Color.FromArgb(244,193,146));
   Box(x+5,y-2+stride,7,x+7,y+2+stride,14,Color.FromArgb(244,193,146));
   Box(x-4,y-3,17,x+4,y+3,24,Color.FromArgb(244,193,146));
   Box(x-4.5f,y-3.5f,22,x+4.5f,y+3.5f,26,Ink);Box(x-4.5f,y+1,19,x+4.5f,y+3.5f,23,Ink);
  }
  static Color Shade(Color c,float factor) {return Color.FromArgb((int)(c.R*factor),(int)(c.G*factor),(int)(c.B*factor));}
  void Ground(float x1,float y1,float x2,float y2,float h,Color c) {
   Quad(new Vertex(x1,y1,h),new Vertex(x2,y1,h),new Vertex(x2,y2,h),new Vertex(x1,y2,h),c);
  }
  void Box(float x1,float y1,float h1,float x2,float y2,float h2,Color c) {
   Vertex a=new Vertex(x1,y1,h1),b=new Vertex(x2,y1,h1),cc=new Vertex(x2,y2,h1),d=new Vertex(x1,y2,h1);
   Vertex aa=new Vertex(x1,y1,h2),bb=new Vertex(x2,y1,h2),ccc=new Vertex(x2,y2,h2),dd=new Vertex(x1,y2,h2);
   Quad(aa,bb,ccc,dd,c);Quad(a,b,bb,aa,Shade(c,.82f));Quad(d,cc,ccc,dd,Shade(c,.94f));
   Quad(a,d,dd,aa,Shade(c,.73f));Quad(b,cc,ccc,bb,Shade(c,.88f));
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
    float u=wa,v=wb;
    for(int x=left;x<=right;x++,u+=dax,v+=dbx) {
     float w=1-u-v;if(u<-.00001f||v<-.00001f||w<-.00001f)continue;
     float z=u*a.Z+v*b.Z+w*c.Z;int index=y*W+x;
     if(z>depth[index]){depth[index]=z;pixels[index]=color;}
    }
   }
  }
  public void Dispose(){frame.Dispose();}
 }
}
