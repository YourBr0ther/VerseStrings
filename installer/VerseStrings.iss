; VerseStrings installer (Inno Setup 6.3+)
; Build:
;   1. dotnet publish src\VerseStrings.App -c Release -r win-x64 --self-contained true ^
;        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
;   2. ISCC.exe installer\VerseStrings.iss
; Output: dist\VerseStringsSetup-<version>.exe

#define AppName       "VerseStrings"
#ifndef AppVersion
  #define AppVersion  "0.0.0-dev"
#endif
#define AppPublisher  "YourBr0ther"
#define AppExeName    "VerseStrings.exe"
#define AppId         "{B19E58A1-3A0B-4C5A-8C9D-7A1B2C3D4E5F}"
#define PublishDir    "..\src\VerseStrings.App\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish"

[Setup]
AppId={{#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
DisableDirPage=yes
UninstallDisplayIcon={app}\{#AppExeName}
UninstallDisplayName={#AppName}
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
OutputDir=..\dist
OutputBaseFilename={#AppName}Setup-{#AppVersion}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
LicenseFile=..\LICENSE
SetupIconFile=..\src\VerseStrings.App\Assets\icon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &Desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";        Filename: "{app}\{#AppExeName}"
Name: "{userdesktop}\{#AppName}";  Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent
