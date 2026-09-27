; Inno Setup script for the CRMS desktop app.
; Packages the published WinForms client (with its bundled .\api\ copy of
; CRMS.api, auto-started by Program.cs) into a real Windows installer with a
; Start Menu shortcut, optional Desktop icon, and an uninstaller entry.

#define MyAppName "CRMS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CRMS Repair Shop"
#define MyAppExeName "CRMS.winforms.exe"
#define PublishDir "..\publish\winforms"

[Setup]
AppId={{6F2B7B7A-6C0F-4F3E-9B0A-CRMS10001APP}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\publish\installer
OutputBaseFilename=CRMS-Setup
SetupIconFile=..\CRMS\Resources\app.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName} now"; Flags: nowait postinstall skipifsilent

[UninstallRun]
; Belt-and-suspenders: the app kills its own bundled API on normal exit, but
; if it was force-closed last session, make sure nothing's left running
; (and locking files) when the user uninstalls.
Filename: "{cmd}"; Parameters: "/C taskkill /IM CRMS.api.exe /F"; Flags: runhidden skipifdoesntexist; RunOnceId: "KillApi"
Filename: "{cmd}"; Parameters: "/C taskkill /IM CRMS.winforms.exe /F"; Flags: runhidden skipifdoesntexist; RunOnceId: "KillWinforms"
