#define AppVersion "1.8.0"
[Setup]
AppId=QuietCrossing.MaksimDenisov31
AppName=Тихий переход
AppVersion={#AppVersion}
AppPublisher=MaksimDenisov31
AppPublisherURL=https://github.com/MaksimDenisov31/quiet-crossing
AppUpdatesURL=https://github.com/MaksimDenisov31/quiet-crossing/releases
DefaultDirName={localappdata}\Programs\QuietCrossing
DefaultGroupName=Тихий переход
PrivilegesRequired=lowest
MinVersion=10.0
UninstallDisplayIcon={app}\QuietCrossing.exe
SetupIconFile=..\assets\app.ico
OutputDir=..\installer-output
OutputBaseFilename=QuietCrossing-Setup-{#AppVersion}
VersionInfoVersion=1.8.0.0
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
Source: "..\QuietCrossing.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\QuietCrossing.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\README.md"; DestDir: "{app}"; Flags: ignoreversion
[Icons]
Name: "{userprograms}\Тихий переход"; Filename: "{app}\QuietCrossing.exe"
Name: "{userdesktop}\Тихий переход"; Filename: "{app}\QuietCrossing.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\QuietCrossing.exe"; Description: "Запустить игру"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('Для игры требуется .NET Framework 4.8 или новее.', mbError, MB_OK);
end;
