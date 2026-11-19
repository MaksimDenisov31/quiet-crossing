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
 public sealed class Window : Form {
  Settings settings; Game game; string page="menu",returnPage="menu";
  readonly HashSet<Keys> keys=new HashSet<Keys>();
  readonly List<Hit> hits=new List<Hit>();
  readonly Timer timer=new Timer(); readonly Stopwatch watch=Stopwatch.StartNew();
  readonly Bitmap scene=new Bitmap(480,320);
  SoundPlayer soundPlayer; MemoryStream soundStream;
  PointF mouse=new PointF(-1,-1); int focus=-1; double previous; float ambient;
  Rectangle viewport; bool preview; string notice="";
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
   Deactivate+=delegate {keys.Clear();if(page=="play") {page="pause";focus=-1;Invalidate();}};
   FormClosing+=delegate {if(!preview) {Rectangle r=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;settings.Width=r.Width;settings.Height=r.Height;settings.Maximized=WindowState==FormWindowState.Maximized;settings.Save(Settings.PathName);}};
   if(preview && capture) Shown+=delegate {CapturePreviews();Close();};
  }
  void Save() {if(!preview && !settings.Save(Settings.PathName)) notice="Не удалось сохранить настройки.";}
  void StartGame() {game=new Game(settings.Difficulty,Environment.TickCount);keys.Clear();page="play";focus=-1;}
  void Go(string value) {page=value;keys.Clear();focus=-1;Invalidate();}
  void Tick(object sender,EventArgs e) {
   double now=watch.Elapsed.TotalSeconds;float dt=(float)Math.Min(0.05,now-previous);previous=now;ambient+=dt;
   if(page=="play") {
    game.Update(dt,(Down(Keys.Right,Keys.D)?1:0)-(Down(Keys.Left,Keys.A)?1:0),(Down(Keys.Down,Keys.S)?1:0)-(Down(Keys.Up,Keys.W)?1:0));
    if(game.Result!=Outcome.Playing) {
     Go(game.Result==Outcome.Lost?"lost":"won");
     if(game.Result==Outcome.Won) {settings.Wins++;Save();}
     Tone(game.Result==Outcome.Won?660:150);
    }
   } else if(page=="menu") {foreach(Car c in game.Cars) {c.X+=c.Speed*dt;if(c.X>530)c.X=-50;if(c.X< -50)c.X=530;}}
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
  protected override void OnKeyDown(KeyEventArgs e) {
   base.OnKeyDown(e);keys.Add(e.KeyCode);
   if(e.KeyCode==Keys.Escape || e.KeyCode==Keys.P) {
    if(page=="play")Go("pause");else if(page=="pause")Go("play");else if(page=="settings")Go(returnPage);else if(page!="menu")Go("menu");
   } else if(page!="play" && (e.KeyCode==Keys.Tab || e.KeyCode==Keys.Down || e.KeyCode==Keys.Up)) {
    if(hits.Count>0)focus=(focus+(e.KeyCode==Keys.Up?-1:1)+hits.Count)%hits.Count;
   } else if(page!="play" && (e.KeyCode==Keys.Enter || e.KeyCode==Keys.Space)) {
    if(hits.Count>0)hits[Math.Max(0,Math.Min(focus,hits.Count-1))].Action();
   } else if(e.KeyCode==Keys.R && (page=="lost"||page=="won"))StartGame();
   e.Handled=true;e.SuppressKeyPress=true;Invalidate();
  }
  protected override void OnKeyUp(KeyEventArgs e) {keys.Remove(e.KeyCode);base.OnKeyUp(e);}
  PointF Logical(Point p) {return new PointF((p.X-viewport.X)*480f/Math.Max(1,viewport.Width),(p.Y-viewport.Y)*320f/Math.Max(1,viewport.Height));}
  protected override void OnMouseMove(MouseEventArgs e) {mouse=Logical(e.Location);Cursor=hits.Exists(h=>h.Rect.Contains(mouse))?Cursors.Hand:Cursors.Default;base.OnMouseMove(e);}
  protected override void OnMouseLeave(EventArgs e) {mouse=new PointF(-1,-1);base.OnMouseLeave(e);}
  protected override void OnMouseDown(MouseEventArgs e) {base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;PointF p=Logical(e.Location);foreach(Hit h in hits.ToArray())if(h.Rect.Contains(p)){h.Action();break;}}
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
  void DrawScene(Graphics g) {
   Rect(g,Color.FromArgb(150,180,128),0,0,480,320);
   for(int i=0;i<100;i++){int x=(i*113+23)%480,y=(i*71+13)%320;if(y<76||y>254)Rect(g,i%4==0?Color.FromArgb(217,207,153):Color.FromArgb(132,164,117),x,y,3,2);}
   Rect(g,Color.FromArgb(194,197,171),0,77,480,20);Rect(g,Color.FromArgb(194,197,171),0,231,480,20);
   Rect(g,Color.FromArgb(229,223,192),0,78,480,3);Rect(g,Color.FromArgb(229,223,192),0,232,480,3);
   Rect(g,Color.FromArgb(65,78,80),0,98,480,132);Rect(g,Color.FromArgb(53,69,72),0,98,480,3);
   for(int lane=1;lane<4;lane++)for(int x=0;x<480;x+=29)if(x<204||x>273)Rect(g,lane==2?Color.FromArgb(203,181,119):Color.FromArgb(122,135,130),x,99+lane*32,15,1);
   for(int y=103;y<228;y+=16)Rect(g,Color.FromArgb(226,223,197),216,y,48,9);
   Rect(g,Color.FromArgb(216,196,128),218,86,44,6);Rect(g,Color.FromArgb(216,196,128),218,239,44,6);
   for(int x=219;x<262;x+=6){Rect(g,Color.FromArgb(170,153,101),x,87,2,2);Rect(g,Color.FromArgb(170,153,101),x,240,2,2);}
   Tree(g,35,42);Tree(g,100,25);Tree(g,407,42);Tree(g,450,13);Tree(g,58,278);Tree(g,395,290);Tree(g,443,267);
   // Crossing signs and small park benches.
   foreach(int x in new int[]{194,283}) {Rect(g,ink,x,65,2,26);Rect(g,Color.FromArgb(102,151,165),x-6,61,14,13);Rect(g,cream,x-3,64,8,7);Rect(g,ink,x,65,2,5);}
   Rect(g,Color.FromArgb(138,109,80),115,56,29,5);Rect(g,ink,118,61,2,7);Rect(g,ink,139,61,2,7);
   foreach(Car c in game.Cars) {
    int x=(int)(c.X-c.Width/2),y=(int)c.Y,w=(int)c.Width;Color color=carColors[c.Color];
    Rect(g,Color.FromArgb(47,61,63),x+1,y+6,w,5);Rect(g,ink,x+4,y-10,6,3);Rect(g,ink,x+w-10,y-10,6,3);Rect(g,ink,x+4,y+7,6,3);Rect(g,ink,x+w-10,y+7,6,3);
    Rect(g,color,x,y-7,w,15);Rect(g,color,x+2,y-9,w-4,19);
    Rect(g,Color.FromArgb(64,93,100),x+8,y-6,w-16,12);Rect(g,color,x+12,y-7,w-24,14);Rect(g,Color.FromArgb(175,207,200),c.Speed>0?x+w-9:x+5,y-5,3,10);
    Rect(g,cream,c.Speed>0?x+w-2:x,y-5,2,3);Rect(g,cream,c.Speed>0?x+w-2:x,y+3,2,3);
   }
   Person(g,game.X,game.Y,page=="lost");
   if(page=="play") {TextAt(g,"ФИНИШ",221,46,8,ink,true);Rect(g,ink,238,60,4,6);Rect(g,ink,235,60,10,2);}
  }
  void Draw(Graphics g) {
   g.SmoothingMode=SmoothingMode.None;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;hits.Clear();DrawScene(g);
   if(page=="play") {
    Rect(g,ink,12,12,132,28);TextAt(g,"ТИХИЙ ПЕРЕХОД",21,19,10,cream,true);
    Button(g,"Пауза  II",376,13,91,delegate{Go("pause");});
    Rect(g,ink,120,293,240,19);Center(g,"WASD / стрелки — идти     Esc — пауза",296,9,cream);
    return;
   }
   using(var b=new SolidBrush(Color.FromArgb(172,25,43,39)))g.FillRectangle(b,0,0,480,320);
   if(page=="menu") {
    Center(g,"МАЛЕНЬКАЯ ПИКСЕЛЬНАЯ ИГРА",38,8,mint,true);
    Center(g,"ТИХИЙ",56,35,cream,true);Center(g,"ПЕРЕХОД",89,35,cream,true);
    Center(g,"Одна дорога. Выбери свой момент.",137,11,Color.FromArgb(202,216,191));
    Button(g,"Играть  →",151,167,178,StartGame,true);
    Button(g,"Настройки",151,201,178,delegate{returnPage="menu";Go("settings");});
    Button(g,"Выход",151,235,178,Close);
    Center(g,"WASD / стрелки • Esc — пауза",282,9,mint);
    TextAt(g,"v1.0.0",12,303,7,mint);TextAt(g,"Переходов: "+settings.Wins,379,303,8,mint);
   } else if(page=="settings") {
    Center(g,"НАСТРОЙКИ",44,24,cream,true);Center(g,"Настрой свой ритм",78,10,mint);
    TextAt(g,"Поток машин",84,115,10,cream,true);
    string[] labels={"Спокойный","Обычный","Плотный"};
    for(int i=0;i<3;i++){int d=i;Button(g,labels[i],84+i*106,137,100,delegate{settings.Difficulty=d;Save();},settings.Difficulty==i);}
    Center(g,"Изменение сложности применяется к новой игре.",174,9,mint);
    Button(g,"Звуки: "+(settings.Sound?"включены":"выключены"),121,200,238,delegate{settings.Sound=!settings.Sound;Save();if(settings.Sound)Tone(440);});
    Button(g,"Готово",151,244,178,delegate{Go(returnPage);},true);
    Center(g,notice,285,8,cream);
   } else {
    bool lost=page=="lost",won=page=="won";
    Center(g,lost?"НЕ В ЭТОТ РАЗ":won?"ТЫ НА ДРУГОЙ СТОРОНЕ!":"ПАУЗА",68,22,cream,true);
    Center(g,lost?"Машина оказалась быстрее. Попробуем ещё?":won?"Дорога позади. Отличный момент для победы.":"Выдохни. Дорога подождёт.",107,10,mint);
    if(lost||won)Center(g,"Время: "+game.Time.ToString("0.0")+" с",132,10,cream);
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
   foreach(string p in new string[]{"menu","play","settings","pause","lost","won"}) {
    page=p;game=new Game(1,1234);if(p=="lost")game.Result=Outcome.Lost;if(p=="won"){game.Y=73;game.Result=Outcome.Won;}
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
     click("Готово");click("Играть  →");if(w.page!="play"||w.game.Difficulty!=2)throw new Exception("Start game");
     float before=w.game.Y;w.OnKeyDown(new KeyEventArgs(Keys.W));w.previous=w.watch.Elapsed.TotalSeconds-0.05;w.Tick(null,EventArgs.Empty);w.OnKeyUp(new KeyEventArgs(Keys.W));
     if(w.game.Y>=before)throw new Exception("Movement input");log.AppendLine("PASS mouse menu, settings, difficulty, keyboard movement");
     w.OnKeyDown(new KeyEventArgs(Keys.Escape));float frozen=w.game.Time;
     w.Tick(null,EventArgs.Empty);if(w.page!="pause"||w.game.Time!=frozen)throw new Exception("Pause");
     click("Настройки");click("Готово");if(w.page!="pause")throw new Exception("Settings return");
     click("Продолжить");w.OnDeactivate(EventArgs.Empty);if(w.page!="pause"||w.keys.Count!=0)throw new Exception("Focus loss pause");
     log.AppendLine("PASS pause freezes simulation, settings return, focus loss pause");
     click("Продолжить");w.game.Cars.Clear();w.game.Y=116;w.game.Cars.Add(new Car(w.game.X,116,1,29,0));w.previous=w.watch.Elapsed.TotalSeconds-0.02;w.Tick(null,EventArgs.Empty);
     if(w.page!="lost")throw new Exception("Collision screen");w.Refresh();click("Ещё раз  →");if(w.page!="play"||w.game.Y!=275)throw new Exception("Restart");
     w.game.Cars.Clear();w.game.Y=76;w.previous=w.watch.Elapsed.TotalSeconds-0.02;w.Tick(null,EventArgs.Empty);if(w.page!="won"||w.settings.Wins!=1)throw new Exception("Victory screen");
     w.Refresh();click("Главное меню");if(w.page!="menu")throw new Exception("Menu return");
     w.OnKeyDown(new KeyEventArgs(Keys.Tab));w.OnKeyDown(new KeyEventArgs(Keys.Enter));if(w.page!="play")throw new Exception("Keyboard menu");
     log.AppendLine("PASS collision screen, retry, victory counter, menu, keyboard activation");
     w.CapturePreviews();w.Close();log.AppendLine("PASS six rendered screen previews");
    }
    File.WriteAllText("ui-test-results.txt",log.ToString());return 0;
   } catch(Exception ex) {File.WriteAllText("ui-test-results.txt",log+"FAIL "+ex);return 1;}
  }
  protected override void Dispose(bool disposing) {if(disposing){timer.Dispose();scene.Dispose();if(soundPlayer!=null)soundPlayer.Dispose();if(soundStream!=null)soundStream.Dispose();}base.Dispose(disposing);}
 }
}
