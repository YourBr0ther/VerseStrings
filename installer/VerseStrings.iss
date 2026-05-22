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
; Same name as the single-instance mutex in App.xaml.cs. Lets Setup detect
; the running tray during a reinstall and prompt the user to close it,
; instead of letting NTFS replace the exe under a process that keeps
; running the old code in memory.
AppMutex=Global\{#AppName}.SingleInstance
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
Filename: "{app}\{#AppExeName}"; Parameters: "--pack={code:GetSelectedPackId}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
var
  PackPage: TInputOptionWizardPage;

procedure InitializeWizard;
begin
  { Pack picker. Sits after the License page, before Ready-to-Install. }
  PackPage := CreateInputOptionPage(
    wpLicense,
    'Localization pack',
    'Choose which community pack to start with.',
    'You can switch packs anytime from the tray menu after install.',
    True,  { Exclusive: True => radio buttons. }
    False); { ListBox: False => stacked radios, not a listbox. }
  PackPage.Add('StarStrings (MrKraken)');
  PackPage.Add('ScCompLangPack (ExoAE)');
  PackPage.Add('ScCompLangPackRemix (BeltaKoda)');
  PackPage.Add('ScCompLangPackRemix2 (ExoAE)');
  PackPage.SelectedValueIndex := 0;
end;

function GetSelectedPackId(Param: String): String;
begin
  case PackPage.SelectedValueIndex of
    0: Result := 'StarStrings';
    1: Result := 'ScCompLangPack';
    2: Result := 'ScCompLangPackRemix';
    3: Result := 'ScCompLangPackRemix2';
  else
    Result := 'StarStrings';
  end;
end;
