#define AppVersion "1.11.0"
[Setup]
AppId=QuietCrossing.MaksimDenisov31
AppName=пиксельный переход Игра
AppVersion={#AppVersion}
AppPublisher=MaksimDenisov31
AppPublisherURL=https://github.com/MaksimDenisov31/quiet-crossing
AppUpdatesURL=https://github.com/MaksimDenisov31/quiet-crossing/releases
DefaultDirName={localappdata}\Programs\пиксельный переход Игра
DefaultGroupName=пиксельный переход Игра
PrivilegesRequired=lowest
MinVersion=10.0
UninstallDisplayIcon={app}\пиксельный переход Игра.exe
SetupIconFile=..\assets\app.ico
OutputDir=..\installer-output
OutputBaseFilename=PixelCrossing-Setup-{#AppVersion}
VersionInfoVersion=1.11.0.0
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"
[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; Flags: unchecked
[Files]
Source: "..\пиксельный переход Игра.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\пиксельный переход Игра.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
[InstallDelete]
Type: files; Name: "{app}\QuietCrossing.exe"
Type: files; Name: "{app}\QuietCrossing.exe.config"
Type: files; Name: "{userprograms}\Тихий переход.lnk"
Type: files; Name: "{userdesktop}\Тихий переход.lnk"
[Icons]
Name: "{userprograms}\пиксельный переход Игра"; Filename: "{app}\пиксельный переход Игра.exe"
Name: "{userdesktop}\пиксельный переход Игра"; Filename: "{app}\пиксельный переход Игра.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\пиксельный переход Игра.exe"; Description: "Запустить игру"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('Для игры требуется .NET Framework 4.8 или новее.', mbError, MB_OK);
end;
