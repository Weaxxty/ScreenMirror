#ifndef AppVersion
#define AppVersion "1.0.0"
#endif
[Setup]
AppId={{D2AB5C6E-93D2-4F82-8DA0-08BB50157037}
AppName=ScreenMirror
AppVersion={#AppVersion}
AppPublisher=ScreenMirror contributors
DefaultDirName={localappdata}\Programs\ScreenMirror
DefaultGroupName=ScreenMirror
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
OutputDir=..\dist
OutputBaseFilename=ScreenMirror-Setup-{#AppVersion}-x64
SetupIconFile=..\dist\payload\ScreenMirror.ico
UninstallDisplayIcon={app}\ScreenMirror.exe
LicenseFile=..\LICENSE
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
CloseApplicationsFilter=ScreenMirror.exe,uxplay.exe
RestartApplications=no
DisableProgramGroupPage=yes
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
[Files]
Source: "..\dist\payload\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\ScreenMirror"; Filename: "{app}\ScreenMirror.exe"; IconFilename: "{app}\ScreenMirror.ico"
Name: "{autodesktop}\ScreenMirror"; Filename: "{app}\ScreenMirror.exe"; IconFilename: "{app}\ScreenMirror.ico"; Tasks: desktopicon
[Run]
Filename: "{app}\ScreenMirror.exe"; Description: "{cm:LaunchProgram,ScreenMirror}"; Flags: nowait postinstall skipifsilent
[Code]
function InitializeSetup(): Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('ScreenMirror requires Microsoft .NET Framework 4.8. Install it from https://dotnet.microsoft.com/download/dotnet-framework/net48 and run setup again.', mbError, MB_OK);
end;
