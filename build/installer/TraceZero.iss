; Inno Setup — installeur TraceZero (Phase 19).
; Compilé par build\scripts\publish-installer.ps1, qui passe MyAppVersion et StageDir.
; Installation par défaut PER-USER (sans admin, cohérent avec « l'app n'est jamais admin ») :
; l'utilisateur peut choisir une installation pour tous (Program Files) dans l'assistant.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef StageDir
  #define StageDir "..\..\artifacts\installer\stage"
#endif

#define MyAppName "TraceZero"
#define MyAppPublisher "Sharkade02"
#define MyAppURL "https://github.com/Sharkade02/tracezero"
#define MyAppExeName "TraceZero.App.exe"

[Setup]
AppId={{B2E3F1A4-5C6D-4E7F-8A9B-0C1D2E3F4A5B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
LicenseFile={#SourcePath}\..\..\LICENSE
OutputDir={#SourcePath}\..\..\artifacts\installer
OutputBaseFilename=TraceZeroSetup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#StageDir}\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent
