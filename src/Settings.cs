using System;
using System.IO;
using System.Xml.Serialization;

namespace QuietCrossing {
 public enum CameraMode { Bird, FirstPerson, ThirdPerson }
 public sealed class Settings {
  public int Difficulty=1, Width=1000, Height=720, Wins=0;
  public CameraMode Camera=CameraMode.Bird;
  public bool Sound=true, Maximized=false;
  public static string PathName {get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"QuietCrossing","settings.xml");}}
  public static Settings Load(string path) {
   try {using(var stream=File.OpenRead(path)) {var s=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);s.Difficulty=Math.Max(0,Math.Min(2,s.Difficulty));if(!Enum.IsDefined(typeof(CameraMode),s.Camera))s.Camera=CameraMode.Bird;return s;}}
   catch(IOException) {return new Settings();} catch(InvalidOperationException) {return new Settings();} catch(UnauthorizedAccessException) {return new Settings();}
  }
  public bool Save(string path) {
   try {Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";
    using(var stream=File.Create(temp)) new XmlSerializer(typeof(Settings)).Serialize(stream,this);
    if(File.Exists(path)) File.Replace(temp,path,null);else File.Move(temp,path);return true;
   } catch(IOException) {return false;} catch(UnauthorizedAccessException) {return false;}
  }
 }
}
