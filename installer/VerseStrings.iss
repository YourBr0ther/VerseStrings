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
; Populates the installer exe's version-info block. Reduces "unattributed
; binary" heuristic weight on AV scanners and shows real values in the
; right-click Properties dialog instead of "VerseStrings" with empty fields.
VersionInfoCompany={#AppPublisher}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}
VersionInfoVersion={#AppVersion}
VersionInfoDescription=VerseStrings installer
VersionInfoCopyright=Copyright (c) 2026 {#AppPublisher}
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
Name: "trayshortcut"; Description: "Tray app (background, auto-installs new pack releases)"; GroupDescription: "Run modes:"
Name: "standaloneshortcut"; Description: "Standalone app (open it manually to sync)"; GroupDescription: "Run modes:"; Flags: unchecked
Name: "desktopicon"; Description: "Create a &Desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
; Start menu shortcuts — one per selected mode.
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: trayshortcut
Name: "{group}\{#AppName} Standalone"; Filename: "{app}\{#AppExeName}"; Parameters: "--standalone"; Tasks: standaloneshortcut
; Desktop shortcut — points at whichever mode is installed. If both are
; installed, prefer tray (matches v0.1.15 behavior).
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon and trayshortcut
Name: "{userdesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Parameters: "--standalone"; Tasks: desktopicon and standaloneshortcut and not trayshortcut

[Registry]
; Why (install-time, conditional): a previous tray install may have enabled
; autostart, leaving an HKCU Run-key value pointing at our exe. If the user
; reinstalls and opts out of the tray task, that orphaned Run-key would
; still launch VerseStrings (in tray mode by default) at every login.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "{#AppName}"; ValueType: none; Flags: deletevalue; Tasks: not trayshortcut
; Why (uninstall, unconditional): if the user enabled autostart while using
; the app, the Run-key value persists after uninstall pointing at a deleted
; exe. Windows silently ignores it at login but it sticks around in Task
; Manager → Startup apps as a disabled-looking entry. Scrub it.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "{#AppName}"; ValueType: none; Flags: uninsdeletevalue

[Run]
; Post-install launch honors the user's mode pick. If tray is selected,
; launch tray (regardless of standalone). If standalone-only, launch
; standalone. If neither (blocked by NextButtonClick anyway), nothing runs.
Filename: "{app}\{#AppExeName}"; Parameters: "--pack={code:GetSelectedPackId}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent; Tasks: trayshortcut
Filename: "{app}\{#AppExeName}"; Parameters: "--standalone --pack={code:GetSelectedPackId}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent; Tasks: standaloneshortcut and not trayshortcut

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
    'You can switch packs anytime after install.',
    True,  { Exclusive: True => radio buttons. }
    False); { ListBox: False => stacked radios, not a listbox. }
  PackPage.Add('StarStrings (MrKraken)');
  PackPage.Add('ScCompLangPack (ExoAE)');
  PackPage.Add('ScCompLangPackRemix (BeltaKoda)');
  PackPage.Add('ScCompLangPackRemix2 (ExoAE)');
  PackPage.SelectedValueIndex := 0;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if CurPageID = wpSelectTasks then
  begin
    if not WizardIsTaskSelected('trayshortcut') and not WizardIsTaskSelected('standaloneshortcut') then
    begin
      MsgBox('Please choose at least one run mode (Tray or Standalone).', mbError, MB_OK);
      Result := False;
    end;
  end;
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
