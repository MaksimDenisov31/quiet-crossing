using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Windows.Forms;

namespace QuietCrossing {
 static class Program {
  [STAThread] static int Main(string[] args) {
   if(args.Length>0 && args[0]=="--self-test") return Tests.Run();
   Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
   if(args.Length>0 && args[0]=="--ui-test") return Window.RunUiTests();
   Application.Run(new Window(args.Length>0 && args[0]=="--preview"));return 0;
  }
 }
 sealed class Hit {
  public RectangleF Rect; public Action Action; public string Label;
  public Hit(RectangleF r,string label,Action a) {Rect=r;Label=label;Action=a;}
 }
 public sealed partial class Window : Form {
  Settings settings; Game game; string page="menu",returnPage="menu";
  readonly HashSet<Keys> keys=new HashSet<Keys>();
  readonly List<Hit> hits=new List<Hit>();
  readonly Timer timer=new Timer(); readonly Stopwatch watch=Stopwatch.StartNew();
  readonly Bitmap scene=new Bitmap(480,320);
  SoundPlayer soundPlayer; MemoryStream soundStream;
  PointF mouse=new PointF(-1,-1); int focus=-1; double previous; float ambient;
  Rectangle viewport; bool preview; string notice="";
  double crosswalkNoticeUntil;
  readonly Color ink=ColorTranslator.FromHtml("#283c39"), cream=ColorTranslator.FromHtml("#f4f1df"),mint=ColorTranslator.FromHtml("#c4dfac");
  readonly Color[] carColors={Color.FromArgb(218,135,110),Color.FromArgb(228,191,107),Color.FromArgb(132,170,181),Color.FromArgb(193,183,209),Color.FromArgb(208,218,190)};
  public Window(bool previewMode) : this(previewMode,true) { }
  Window(bool previewMode,bool capture) {
   preview=previewMode;settings=preview?new Settings():Settings.Load(Settings.PathName);
   Text="Тихий переход"; MinimumSize=new Size(760,560);StartPosition=FormStartPosition.CenterScreen;
   Size=new Size(Math.Min(Screen.PrimaryScreen.WorkingArea.Width,Math.Max(760,settings.Width)),Math.Min(Screen.PrimaryScreen.WorkingArea.Height,Math.Max(560,settings.Height)));
   if(settings.Maximized) WindowState=FormWindowState.Maximized;
   DoubleBuffered=true;KeyPreview=true;BackColor=ink;
   using(var stream=System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("QuietCrossing.Icon")) if(stream!=null) Icon=new Icon(stream);
   game=new Game(1,1234);
   timer.Interval=16;timer.Tick+=Tick;timer.Start();
   Deactivate+=delegate {EndMouseTurn();keys.Clear();if(page=="play") {page="pause";focus=-1;Invalidate();}};
   FormClosing+=delegate {if(!preview) {Rectangle r=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;settings.Width=r.Width;settings.Height=r.Height;settings.Maximized=WindowState==FormWindowState.Maximized;settings.Save(Settings.PathName);}};
   if(preview && capture) Shown+=delegate {CapturePreviews();Close();};
  }
  void Save() {if(!preview && !settings.Save(Settings.PathName)) notice="Не удалось сохранить настройки.";}
  void StartGame() {EndMouseTurn();game=new Game(settings.Difficulty,Environment.TickCount);yaw=0;crosswalkNoticeUntil=0;keys.Clear();page="play";focus=-1;}
  void ChooseCamera(CameraMode mode) {settings.Camera=mode;Save();StartGame();}
  void CycleCamera() {EndMouseTurn();settings.Camera=(CameraMode)(((int)settings.Camera+1)%3);Save();}
  void Go(string value) {EndMouseTurn();page=value;keys.Clear();focus=value=="camera"?(settings.Camera==CameraMode.FirstPerson?0:settings.Camera==CameraMode.ThirdPerson?1:2):-1;Invalidate();}
  void Tick(object sender,EventArgs e) {
   double now=watch.Elapsed.TotalSeconds;float dt=(float)Math.Min(0.05,now-previous);previous=now;ambient+=dt;
   if(page=="play") {
    bool perspective=settings.Camera!=CameraMode.Bird;
    if(perspective)yaw=PlayerControls.Wrap(yaw+((Down(Keys.E,Keys.Right)?1:0)-(Down(Keys.Q,Keys.Left)?1:0))*PlayerControls.TurnSpeed*dt);
    float forward=(Down(Keys.Up,Keys.W)?1:0)-(Down(Keys.Down,Keys.S)?1:0);
    float strafe=(keys.Contains(Keys.D)||!perspective&&keys.Contains(Keys.Right)?1:0)-(keys.Contains(Keys.A)||!perspective&&keys.Contains(Keys.Left)?1:0);
    MapPoint movement=PlayerControls.Movement(settings.Camera,yaw,forward,strafe);game.Update(dt,movement.X,movement.Y);
    if(game.Result!=Outcome.Playing) {
     if(game.Loss==LossReason.OutsideCrosswalk){StartGame();crosswalkNoticeUntil=now+2.5;Tone(150);}
     else {
      Go(game.Result==Outcome.Lost?"lost":"won");
      if(game.Result==Outcome.Won) {settings.Wins++;Save();}
      Tone(game.Result==Outcome.Won?660:150);
     }
    }
   } else if(page=="menu") {foreach(Car c in game.Cars) {c.X+=c.Speed*dt;if(c.X>CityMap.Width+50)c.X=-50;if(c.X< -50)c.X=CityMap.Width+50;}}
   Invalidate();
  }
  bool Down(Keys a,Keys b) {return keys.Contains(a)||keys.Contains(b);}
  void Tone(int hz) {
   if(!settings.Sound)return;
   try {
    if(soundPlayer!=null)soundPlayer.Dispose();if(soundStream!=null)soundStream.Dispose();
    soundStream=new MemoryStream();var stream=soundStream;
    {
     using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)) {
      const int count=6600;writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(22050);writer.Write(44100);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
      for(int i=0;i<count;i++)writer.Write((short)(Math.Sin(i*hz*Math.PI*2/22050)*2800*(1.0-i/(double)count)));
     }
     stream.Position=0;soundPlayer=new SoundPlayer(stream);soundPlayer.Load();soundPlayer.Play();
    }
   } catch(Exception ex) {if(!(ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception || ex is FileNotFoundException))throw;}
  }
  protected override bool IsInputKey(Keys keyData){Keys key=keyData&Keys.KeyCode;return key==Keys.Left||key==Keys.Right||key==Keys.Up||key==Keys.Down||key==Keys.Tab||key==Keys.Enter||key==Keys.Space||base.IsInputKey(keyData);}
  protected override void OnKeyDown(KeyEventArgs e) {
   base.OnKeyDown(e);bool firstPress=keys.Add(e.KeyCode);
   if(e.KeyCode==Keys.Escape || e.KeyCode==Keys.P) {
    if(page=="play")Go("pause");else if(page=="pause")Go("play");else if(page=="map")Go(mapReturn);else if(page=="settings")Go(returnPage);else if(page!="menu")Go("menu");
   } else if(page!="play" && (e.KeyCode==Keys.Tab || e.KeyCode==Keys.Down || e.KeyCode==Keys.Up)) {
    if(hits.Count>0)focus=(focus+(e.KeyCode==Keys.Up?-1:1)+hits.Count)%hits.Count;
   } else if(page!="play" && (e.KeyCode==Keys.Enter || e.KeyCode==Keys.Space)) {
    if(hits.Count>0)hits[Math.Max(0,Math.Min(focus,hits.Count-1))].Action();
   } else if(e.KeyCode==Keys.R && (page=="lost"||page=="won"))StartGame();
   else if(firstPress && e.KeyCode==Keys.V && (page=="play"||page=="pause"))CycleCamera();
   else if(firstPress && e.KeyCode==Keys.M && (page=="play"||page=="pause"||page=="map")){if(page=="map")Go(mapReturn);else OpenMap();}
   e.Handled=true;e.SuppressKeyPress=true;Invalidate();
  }
  protected override void OnKeyUp(KeyEventArgs e) {keys.Remove(e.KeyCode);base.OnKeyUp(e);}
  PointF Logical(Point p) {return new PointF((p.X-viewport.X)*480f/Math.Max(1,viewport.Width),(p.Y-viewport.Y)*320f/Math.Max(1,viewport.Height));}
  protected override void OnMouseMove(MouseEventArgs e) {TurnMouse(e);mouse=Logical(e.Location);Cursor=hits.Exists(h=>h.Rect.Contains(mouse))?Cursors.Hand:Cursors.Default;base.OnMouseMove(e);}
  protected override void OnMouseLeave(EventArgs e) {mouse=new PointF(-1,-1);base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e) {base.OnMouseDown(e);if(e.Button==MouseButtons.Right&&page=="play"&&settings.Camera!=CameraMode.Bird){BeginMouseTurn(e);return;}if(e.Button!=MouseButtons.Left||mouseTurning)return;PointF p=Logical(e.Location);foreach(Hit h in hits.ToArray())if(h.Rect.Contains(p)){h.Action();break;}}
  void Rect(Graphics g,Color c,float x,float y,float w,float h) {using(var b=new SolidBrush(c))g.FillRectangle(b,x,y,w,h);}
  void TextAt(Graphics g,string text,float x,float y,float size,Color c,bool bold=false) {
   using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel)) using(var b=new SolidBrush(c))g.DrawString(text,font,b,x,y,StringFormat.GenericTypographic);
  }
  void Center(Graphics g,string text,float y,float size,Color c,bool bold=false) {
   using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular,GraphicsUnit.Pixel))using(var b=new SolidBrush(c)) {SizeF s=g.MeasureString(text,font);g.DrawString(text,font,b,(480-s.Width)/2,y);}
  }
  void Button(Graphics g,string text,float x,float y,float w,Action action,bool primary=false) {
   var r=new RectangleF(x,y,w,25);int index=hits.Count;bool hot=r.Contains(mouse)||focus==index;
   Rect(g,Color.FromArgb(26,40,38),x,y+3,w,25);
   Rect(g,hot?Color.FromArgb(228,234,195):primary?mint:Color.FromArgb(76,96,85),x,y,w,25);
   if(hot)Rect(g,cream,x+3,y+3,2,19);
   using(var font=new Font("Segoe UI",10,FontStyle.Bold,GraphicsUnit.Pixel))using(var b=new SolidBrush(primary||hot?ink:cream)) {SizeF s=g.MeasureString(text,font);g.DrawString(text,font,b,x+(w-s.Width)/2,y+5);}
   hits.Add(new Hit(r,text,action));
  }
  void Tree(Graphics g,int x,int y) {
   Rect(g,Color.FromArgb(107,145,111),x-13,y+17,31,7);Rect(g,Color.FromArgb(106,100,76),x,y,4,20);
   Rect(g,Color.FromArgb(65,108,85),x-12,y-8,27,22);Rect(g,Color.FromArgb(83,128,94),x-16,y-4,31,13);Rect(g,Color.FromArgb(110,151,106),x-9,y-12,19,11);Rect(g,Color.FromArgb(136,168,115),x-6,y-10,9,4);
  }
  void Person(Graphics g,float px,float py,bool fallen=false) {
   int x=(int)px,y=(int)py;
   Rect(g,Color.FromArgb(91,104,85),x-7,y-1,15,4);
   if(fallen) {Rect(g,cream,x-9,y-6,7,7);Rect(g,carColors[0],x-2,y-7,10,7);Rect(g,ink,x+8,y-7,5,3);return;}
   int stride=page=="play" && keys.Count>0 ? ((int)(game.Time*10)%2)*2:0;
   Rect(g,ink,x-4,y-5,3,5-stride);Rect(g,ink,x+2,y-5,3,3+stride);
   Rect(g,carColors[0],x-5,y-13,11,9);Rect(g,Color.FromArgb(244,193,146),x-4,y-21,8,9);
   Rect(g,ink,x-4,y-23,9,5);Rect(g,ink,x-5,y-21,3,4);Rect(g,cream,x-2,y-12,3,4);Rect(g,Color.FromArgb(244,193,146),x-7,y-10,2,5);
  }
  void Draw(Graphics g) {
   g.SmoothingMode=SmoothingMode.None;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;hits.Clear();
   if(page=="map"){DrawMapPage(g);return;}
   if(settings.Camera!=CameraMode.Bird && page!="menu" && page!="camera") DrawPerspective(g);else DrawScene(g);
   if(page=="play") {
    Rect(g,ink,12,12,132,28);TextAt(g,"ТИХИЙ ПЕРЕХОД",21,19,10,cream,true);
    Button(g,CameraName(settings.Camera)+"  [V]",153,13,214,CycleCamera);
    Button(g,"Пауза  II",376,13,91,delegate{Go("pause");});
    Rect(g,ink,12,293,456,19);Center(g,settings.Camera==CameraMode.Bird?"WASD / стрелки — идти     M — карта     V — вид     Esc — пауза":"WASD — идти    Q/E, ←/→ — поворот    ПКМ + мышь — взгляд",296,9,cream);
    DrawRouteHud(g);
    if(watch.Elapsed.TotalSeconds<crosswalkNoticeUntil){Rect(g,ink,45,273,338,16);TextAt(g,"Вышел за зебру — новая попытка с точки А",54,276,9,cream,true);}
    return;
   }
   using(var b=new SolidBrush(Color.FromArgb(172,25,43,39)))g.FillRectangle(b,0,0,480,320);
   if(page=="menu") {
    Center(g,"МАЛЕНЬКАЯ ПИКСЕЛЬНАЯ ИГРА",38,8,mint,true);
    Center(g,"ТИХИЙ",56,35,cream,true);Center(g,"ПЕРЕХОД",89,35,cream,true);
    Center(g,"От точки А до точки Б. Через целый город.",137,11,Color.FromArgb(202,216,191));
    Button(g,"Играть  →",151,167,178,delegate{Go("camera");},true);
    Button(g,"Настройки",151,201,178,delegate{returnPage="menu";Go("settings");});
    Button(g,"Выход",151,235,178,Close);
    Center(g,"WASD / стрелки • Esc — пауза",282,9,mint);
    TextAt(g,"v1.5.0",12,303,7,mint);TextAt(g,"Побед: "+settings.Wins,398,303,8,mint);
   } else if(page=="camera") {
    Center(g,"ВЫБЕРИ ВИД",35,24,cream,true);
    Center(g,"360 переходов · короткие тротуары · свободный поворот",72,10,mint);
    Button(g,"От первого лица",105,106,270,delegate{ChooseCamera(CameraMode.FirstPerson);},settings.Camera==CameraMode.FirstPerson);
    Center(g,"Q/E или ПКМ + мышь — поворот, W — вперёд",137,9,mint);
    Button(g,"От третьего лица",105,163,270,delegate{ChooseCamera(CameraMode.ThirdPerson);},settings.Camera==CameraMode.ThirdPerson);
    Center(g,"Камера и движение следуют за поворотом",194,9,mint);
    Button(g,"С высоты птичьего полёта",105,220,270,delegate{ChooseCamera(CameraMode.Bird);},settings.Camera==CameraMode.Bird);
    Center(g,"Вид сверху с камерой, следующей за тобой",251,9,mint);
    Button(g,"Назад",175,281,130,delegate{Go("menu");});
   } else if(page=="settings") {
    Center(g,"НАСТРОЙКИ",23,24,cream,true);
    TextAt(g,"Камера (можно менять во время игры)",63,65,10,cream,true);
    for(int i=0;i<3;i++){CameraMode mode=(CameraMode)i;Button(g,CameraShort(mode),63+i*120,85,114,delegate{EndMouseTurn();settings.Camera=mode;Save();},settings.Camera==mode);}
    TextAt(g,"Поток машин",84,125,10,cream,true);
    string[] labels={"Спокойный","Обычный","Плотный"};
    for(int i=0;i<3;i++){int d=i;Button(g,labels[i],84+i*106,145,100,delegate{settings.Difficulty=d;Save();},settings.Difficulty==i);}
    Center(g,"Изменение сложности применяется к новой игре.",179,9,mint);
    Button(g,"Звуки: "+(settings.Sound?"включены":"выключены"),121,205,238,delegate{settings.Sound=!settings.Sound;Save();if(settings.Sound)Tone(440);});
    Button(g,"Готово",151,244,178,delegate{Go(returnPage);},true);
    Center(g,notice,285,8,cream);
   } else {
    bool lost=page=="lost",won=page=="won";
    Center(g,lost?"НЕ В ЭТОТ РАЗ":won?"ТЫ ДОШЁЛ ДО ТОЧКИ Б!":"ПАУЗА",68,22,cream,true);
    Center(g,lost?"Пройдено "+game.CompletedRoads+" из "+CityMap.Streets.Length+" переходов. Начнём маршрут заново?":won?"Весь город позади. Маршрут пройден.":"Выдохни. Город подождёт. M — открыть карту.",107,10,mint);
    if(lost||won)Center(g,"Время: "+Game.Duration(game.Time),132,10,cream);
    Button(g,page=="pause"?"Продолжить":"Ещё раз  →",151,167,178,page=="pause"?(Action)(()=>Go("play")):StartGame,true);
    if(page=="pause")Button(g,"Настройки",151,201,178,delegate{returnPage="pause";Go("settings");});
    Button(g,"Главное меню",151,page=="pause"?235:205,178,delegate{Go("menu");});
    Center(g,lost||won?"R — начать заново":"Esc — продолжить",282,9,mint);
   }
  }
  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);using(Graphics g=Graphics.FromImage(scene))Draw(g);
   int scale=Math.Max(1,Math.Min(ClientSize.Width/480,ClientSize.Height/320));
   viewport=new Rectangle((ClientSize.Width-480*scale)/2,(ClientSize.Height-320*scale)/2,480*scale,320*scale);
   e.Graphics.InterpolationMode=InterpolationMode.NearestNeighbor;e.Graphics.PixelOffsetMode=PixelOffsetMode.Half;e.Graphics.DrawImage(scene,viewport);
  }
  void CapturePreviews() {
   Directory.CreateDirectory("docs");
   settings=new Settings{Sound=false};focus=-1;mouse=new PointF(-1,-1);keys.Clear();
   foreach(string p in new string[]{"menu","camera","play","settings","pause","lost","won","map","map-local","city-middle","first-person","third-person","look-left","first-person-road","third-person-road","first-person-right","third-person-right","crosswalk-restart"}) {
    bool firstView=p.StartsWith("first-person")||p=="look-left",thirdView=p.StartsWith("third-person");
    settings.Camera=firstView?CameraMode.FirstPerson:thirdView?CameraMode.ThirdPerson:CameraMode.Bird;
    page=firstView||thirdView||p=="city-middle"||p=="crosswalk-restart"?"play":p;
    crosswalkNoticeUntil=p=="crosswalk-restart"?watch.Elapsed.TotalSeconds+2.5:0;
    mapLocal=p=="map-local";if(mapLocal)page="map";
    yaw=p=="look-left"?-1.25f:0;
    game=new Game(1,1234);if(p.EndsWith("-road"))game.Y=CityMap.Streets[0].Bottom-20;
    if(p.EndsWith("-right")){Tests.VisitRoads(game,1);yaw=(float)Math.PI/2;}
    if(p=="city-middle"){int half=CityMap.Streets.Length/2;Tests.VisitRoads(game,half);Street middle=CityMap.Streets[half];game.X=middle.CrossX;game.Y=middle.Bottom+28;game.Cars.Add(new Car(middle.CrossX+60,middle.Top+16,60,29,0));}
    if(p=="lost")game.Result=Outcome.Lost;if(p=="won")Tests.WalkRoute(game);
    using(Graphics g=Graphics.FromImage(scene))Draw(g);
    using(var output=new Bitmap(960,640))using(Graphics g=Graphics.FromImage(output)){g.InterpolationMode=InterpolationMode.NearestNeighbor;g.PixelOffsetMode=PixelOffsetMode.Half;g.DrawImage(scene,0,0,960,640);output.Save("docs/"+p+".png",ImageFormat.Png);}
   }
  }
  public static int RunUiTests() {
   var log=new System.Text.StringBuilder();
   try {
    using(var w=new Window(true,false)) {
     w.settings.Sound=false;w.Show();Application.DoEvents();
     Action render=()=> {using(var bitmap=new Bitmap(w.ClientSize.Width,w.ClientSize.Height))using(Graphics g=Graphics.FromImage(bitmap))w.OnPaint(new PaintEventArgs(g,w.ClientRectangle));};
     render();
     Action<string> click=label=>{
      render();Hit h=w.hits.Find(item=>item.Label==label);
      if(h==null)throw new Exception("Missing button: "+label);
      Rectangle area=w.viewport;
      int x=area.X+(int)((h.Rect.X+h.Rect.Width/2)*area.Width/480);
      int y=area.Y+(int)((h.Rect.Y+h.Rect.Height/2)*area.Height/320);
      w.OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,x,y,0));render();
     };
     click("Настройки");if(w.page!="settings")throw new Exception("Settings navigation");
     click("Плотный");if(w.settings.Difficulty!=2)throw new Exception("Difficulty control");
     click("Готово");click("Играть  →");if(w.page!="camera")throw new Exception("Camera selection screen");
     click("С высоты птичьего полёта");if(w.page!="play"||w.game.Difficulty!=2||w.settings.Camera!=CameraMode.Bird)throw new Exception("Start game");
     float before=w.game.Y;w.OnKeyDown(new KeyEventArgs(Keys.W));w.previous=w.watch.Elapsed.TotalSeconds-0.05;w.Tick(null,EventArgs.Empty);w.OnKeyUp(new KeyEventArgs(Keys.W));
     if(w.game.Y>=before)throw new Exception("Movement input");log.AppendLine("PASS mouse menu, settings, difficulty, keyboard movement");
     w.OnKeyDown(new KeyEventArgs(Keys.Escape));float frozen=w.game.Time;
     w.Tick(null,EventArgs.Empty);if(w.page!="pause"||w.game.Time!=frozen)throw new Exception("Pause");
     click("Настройки");click("Готово");if(w.page!="pause")throw new Exception("Settings return");
     click("Продолжить");w.OnDeactivate(EventArgs.Empty);if(w.page!="pause"||w.keys.Count!=0)throw new Exception("Focus loss pause");
     log.AppendLine("PASS pause freezes simulation, settings return, focus loss pause");
     click("Продолжить");w.game.Cars.Clear();w.game.Y=CityMap.Streets[0].Top+16;w.game.Cars.Add(new Car(w.game.X,w.game.Y,1,29,0));w.previous=w.watch.Elapsed.TotalSeconds-0.02;w.Tick(null,EventArgs.Empty);
     if(w.page!="lost")throw new Exception("Collision screen");w.Refresh();click("Ещё раз  →");if(w.page!="play"||w.game.Y!=CityMap.Start.Y||w.game.CompletedRoads!=0)throw new Exception("Restart");
     Tests.VisitRoads(w.game,CityMap.Streets.Length);w.game.X=CityMap.Finish.X;w.game.Y=CityMap.Finish.Y;w.previous=w.watch.Elapsed.TotalSeconds-0.02;w.Tick(null,EventArgs.Empty);if(w.page!="won"||w.settings.Wins!=1)throw new Exception("Victory screen");
     w.Refresh();click("Главное меню");if(w.page!="menu")throw new Exception("Menu return");
     w.OnKeyDown(new KeyEventArgs(Keys.Tab));w.OnKeyDown(new KeyEventArgs(Keys.Enter));if(w.page!="camera")throw new Exception("Keyboard menu");render();
     w.OnKeyDown(new KeyEventArgs(Keys.Enter));if(w.page!="play")throw new Exception("Keyboard camera selection");
     log.AppendLine("PASS collision screen, retry, victory counter, menu, keyboard activation");
     foreach(CameraMode mode in Enum.GetValues(typeof(CameraMode))) {
      w.Go("menu");click("Играть  →");click(CameraName(mode));
      if(w.settings.Camera!=mode||w.page!="play")throw new Exception("Camera mode start "+mode);
      Game routeGame=w.game;float mapTime=w.game.Time;
      click("Карта [M]");w.Tick(null,EventArgs.Empty);if(w.page!="map"||w.game.Time!=mapTime)throw new Exception("Map must freeze game");
      click("Район");if(!w.mapLocal)throw new Exception("Local map toggle");click("Весь путь");if(w.mapLocal)throw new Exception("Campaign map toggle");
      w.OnKeyDown(new KeyEventArgs(Keys.M));w.OnKeyUp(new KeyEventArgs(Keys.M));if(w.page!="play"||w.game!=routeGame)throw new Exception("Map keyboard return");
      render();float y=w.game.Y;Game sameGame=w.game;
      w.OnKeyDown(new KeyEventArgs(Keys.V));render();
      if(w.settings.Camera==mode||w.game!=sameGame||w.game.Y!=y||w.game.Time!=0)throw new Exception("Camera cycle must not reset simulation");
      w.settings.Camera=mode;
      w.OnKeyDown(new KeyEventArgs(Keys.Q));w.previous=w.watch.Elapsed.TotalSeconds-.02;w.Tick(null,EventArgs.Empty);w.OnKeyUp(new KeyEventArgs(Keys.Q));
      if(mode!=CameraMode.Bird&&w.yaw>=0)throw new Exception("Turn left");render();
      w.OnKeyDown(new KeyEventArgs(Keys.E));w.previous=w.watch.Elapsed.TotalSeconds-.05;w.Tick(null,EventArgs.Empty);w.OnKeyUp(new KeyEventArgs(Keys.E));
      if(mode!=CameraMode.Bird&&w.yaw<=0)throw new Exception("Turn right");render();
      w.OnKeyDown(new KeyEventArgs(Keys.Escape));float elapsed=w.game.Time;
      click("Настройки");click("Третье лицо");click("Готово");if(w.page!="pause"||w.game!=sameGame||w.game.Time!=elapsed||w.settings.Camera!=CameraMode.ThirdPerson)throw new Exception("Camera setting during pause");
      click("Настройки");click(CameraShort(mode));click("Готово");
      w.OnKeyDown(new KeyEventArgs(Keys.M));w.OnKeyUp(new KeyEventArgs(Keys.M));if(w.page!="map")throw new Exception("Map from pause");click("Продолжить");if(w.page!="pause")throw new Exception("Map returns to pause");
      click("Продолжить");
      w.game.Cars.Clear();w.game.Y=CityMap.Streets[0].Top+16;w.game.Cars.Add(new Car(w.game.X,w.game.Y,1,29,0));w.previous=w.watch.Elapsed.TotalSeconds-.02;w.Tick(null,EventArgs.Empty);
      if(w.page!="lost")throw new Exception("Collision in camera "+mode);render();
      click("Ещё раз  →");if(w.settings.Camera!=mode||w.game.CompletedRoads!=0)throw new Exception("Retry preserves camera and resets route");Tests.VisitRoads(w.game,CityMap.Streets.Length);w.game.X=CityMap.Finish.X;w.game.Y=CityMap.Finish.Y;w.previous=w.watch.Elapsed.TotalSeconds-.02;w.Tick(null,EventArgs.Empty);
      if(w.page!="won")throw new Exception("Victory in camera "+mode);render();
     }
     log.AppendLine("PASS all three cameras, V preserves route, Q/E look, map pauses and returns, full-route win and retry");
     w.Go("menu");click("Играть  →");click("От первого лица");
     for(int i=0;i<15;i++){w.OnKeyDown(new KeyEventArgs(Keys.E));w.previous=w.watch.Elapsed.TotalSeconds-.05;w.Tick(null,EventArgs.Empty);w.OnKeyUp(new KeyEventArgs(Keys.E));}
     float facing=w.yaw;w.previous=w.watch.Elapsed.TotalSeconds-.05;w.Tick(null,EventArgs.Empty);
     if(Math.Abs(w.yaw-facing)>.0001f||Math.Abs(facing-(float)Math.PI/2)>.02f)throw new Exception("Keyboard turn persists after release");
     float turnX=w.game.X,turnY=w.game.Y;w.OnKeyDown(new KeyEventArgs(Keys.W));w.previous=w.watch.Elapsed.TotalSeconds-.05;w.Tick(null,EventArgs.Empty);w.OnKeyUp(new KeyEventArgs(Keys.W));
     if(w.game.X<turnX+3||Math.Abs(w.game.Y-turnY)>.1f)throw new Exception("W must follow the right turn");
     Game turnedGame=w.game;w.CycleCamera();if(w.settings.Camera!=CameraMode.ThirdPerson||w.game!=turnedGame||w.yaw!=facing)throw new Exception("Camera switch must preserve facing");
     render();w.OnMouseDown(new MouseEventArgs(MouseButtons.Right,1,200,200,0));w.OnMouseMove(new MouseEventArgs(MouseButtons.Right,0,300,200,0));w.OnMouseUp(new MouseEventArgs(MouseButtons.Right,1,300,200,0));
     if(Math.Abs(PlayerControls.Wrap(w.yaw-facing))<.1f)throw new Exception("RMB mouse turn");
     facing=w.yaw;w.OnMouseMove(new MouseEventArgs(MouseButtons.None,0,400,200,0));if(w.yaw!=facing)throw new Exception("Free cursor must not turn camera");
     w.OnMouseDown(new MouseEventArgs(MouseButtons.Right,1,200,200,0));w.OnKeyDown(new KeyEventArgs(Keys.M));w.OnKeyUp(new KeyEventArgs(Keys.M));
     if(w.page!="map"||w.mouseTurning||w.Capture)throw new Exception("Opening map releases mouse turn");click("Продолжить");if(w.yaw!=facing)throw new Exception("Map preserves facing");
     w.OnMouseDown(new MouseEventArgs(MouseButtons.Right,1,200,200,0));w.OnDeactivate(EventArgs.Empty);if(w.page!="pause"||w.mouseTurning||w.Capture)throw new Exception("Focus loss releases mouse turn");
     log.AppendLine("PASS persistent keyboard rotation, heading-relative W, camera/map preserve facing, RMB mouse rotation, capture release on map and focus loss");
     click("Продолжить");
     foreach(Keys turnKey in new Keys[]{Keys.E,Keys.Right}){
      float nativeBefore=w.yaw;NativeTestInput.Post(w.Handle,0x100,(int)turnKey,0);Application.DoEvents();
      if(!w.keys.Contains(turnKey))throw new Exception("Native key down "+turnKey);
      w.previous=w.watch.Elapsed.TotalSeconds-.05;w.Tick(null,EventArgs.Empty);
      if(w.yaw==nativeBefore)throw new Exception("Native keyboard turn "+turnKey);
      NativeTestInput.Post(w.Handle,0x101,(int)turnKey,0);Application.DoEvents();if(w.keys.Contains(turnKey))throw new Exception("Native key up "+turnKey);
     }
     NativeTestInput.Post(w.Handle,0x204,2,200|(200<<16));Application.DoEvents();float nativeMouse=w.yaw;
     NativeTestInput.Post(w.Handle,0x200,2,300|(200<<16));Application.DoEvents();
     if(w.yaw==nativeMouse)throw new Exception("Native mouse turn");NativeTestInput.Post(w.Handle,0x205,0,300|(200<<16));Application.DoEvents();
     if(w.mouseTurning||w.Capture)throw new Exception("Native mouse release");log.AppendLine("PASS real WinForms message pipeline: E/right key down and up, RMB drag and release");
     foreach(CameraMode mode in Enum.GetValues(typeof(CameraMode))){
      w.Go("menu");click("Играть  →");click(CameraName(mode));Street first=CityMap.Streets[0];
      Game overlap=Tests.CrosswalkCase(first,first.CrosswalkRight+Game.BodyHalfWidth-.25f,first.Top+16);w.game=overlap;w.previous=w.watch.Elapsed.TotalSeconds-.02;w.Tick(null,EventArgs.Empty);
      if(w.game!=overlap||w.page!="play")throw new Exception("Partial crosswalk overlap must not restart: "+mode);
      Tests.VisitRoads(w.game,2);Game old=w.game;int beforeWins=w.settings.Wins;int difficulty=w.settings.Difficulty;Street road=CityMap.Streets[2];
      w.game.Cars.Clear();w.game.X=road.CrosswalkRight+Game.BodyHalfWidth+.5f;w.game.Y=road.Top+16;w.yaw=.6f;w.OnKeyDown(new KeyEventArgs(Keys.W));w.previous=w.watch.Elapsed.TotalSeconds-.02;w.Tick(null,EventArgs.Empty);
      if(w.game==old||w.page!="play"||w.game.Result!=Outcome.Playing||w.game.CompletedRoads!=0||w.game.Time!=0||w.game.X!=CityMap.Start.X||w.game.Y!=CityMap.Start.Y||w.yaw!=0||w.keys.Count!=0||w.mouseTurning||w.Capture)throw new Exception("Full crosswalk exit must restart at A: "+mode);
      if(w.settings.Camera!=mode||w.settings.Difficulty!=difficulty||w.settings.Wins!=beforeWins||w.crosswalkNoticeUntil<=w.watch.Elapsed.TotalSeconds)throw new Exception("Crosswalk restart must preserve settings and explain cause: "+mode);render();
     }
     log.AppendLine("PASS all three cameras: partial zebra overlap continues, full exit restarts entire route at A, settings preserved and reason shown");
     w.CapturePreviews();w.Close();log.AppendLine("PASS eighteen rendered screen previews");
    }
    File.WriteAllText("ui-test-results.txt",log.ToString());return 0;
   } catch(Exception ex) {File.WriteAllText("ui-test-results.txt",log+"FAIL "+ex);return 1;}
  }
  protected override void Dispose(bool disposing) {if(disposing){timer.Dispose();scene.Dispose();world.Dispose();if(soundPlayer!=null)soundPlayer.Dispose();if(soundStream!=null)soundStream.Dispose();}base.Dispose(disposing);}
 }
}
