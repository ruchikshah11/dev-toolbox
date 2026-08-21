#define MyAppName "DevToolbox"
#define MyAppVersion "1.1.0"
#define MyAppExeName "DevToolbox.exe"
#define MyAppPublisher "Ruchik Shah"

[Setup]
; Fixed GUID so re-running a newer installer upgrades in place instead of creating a duplicate entry.
AppId={{2F6FED4A-7C1B-4E2A-9A2D-3B5B8C7E1F90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={localappdata}\Programs\{#MyAppName}
DefaultGroupName={#MyAppName}
; Per-user install, no UAC prompt - matches the app's own "just run it" distribution philosophy.
PrivilegesRequired=lowest
DisableProgramGroupPage=yes
OutputDir=Output
OutputBaseFilename=DevToolboxSetup
SetupIconFile=..\Assets\AppIcon.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; The SingleFile publish profile (dotnet publish -c Release -p:PublishProfile=SingleFile)
; already bundles every dependency - and the .NET runtime itself, since it's self-contained -
; into the exe, so this is the only file the installer needs to carry - no loose DLLs to list.
Source: "..\bin\Publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{userdesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
