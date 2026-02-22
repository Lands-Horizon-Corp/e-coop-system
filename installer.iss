; Inno Setup Script for ECoopSystem
; Lands Horizon Corporation
; https://github.com/Lands-Horizon-Corp/e-coop-system

#define MyAppName "ECoopSystem"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Lands Horizon Corporation"
#define MyAppURL "https://github.com/Lands-Horizon-Corp/e-coop-system"
#define MyAppExeName "ECoopSystem.exe"
#define MyAppCopyright "Copyright ©2026 Lands Horizon"

[Setup]
; NOTE: The value of AppId uniquely identifies this application. Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId={{8F5A3D2C-1B4E-4C9A-A8F3-2D6E8C9B1A7F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
AppCopyright={#MyAppCopyright}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} Desktop Application
VersionInfoCopyright={#MyAppCopyright}

; Default installation directory
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
LicenseFile=LICENSE.md

; Output configuration
OutputDir=output\installer
OutputBaseFilename=ECoopSystem-Setup-{#MyAppVersion}-win-x64
SetupIconFile=Assets\Images\logo.png
UninstallDisplayIcon={app}\{#MyAppExeName}

; Compression
Compression=lzma2/ultra64
SolidCompression=yes

; Windows version requirements
MinVersion=10.0.17763
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

; Privileges
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Visual appearance
WizardStyle=modern
WizardImageFile=compiler:WizModernImage-IS.bmp
WizardSmallImageFile=compiler:WizModernSmallImage-IS.bmp

; Uninstall configuration
UninstallDisplayName={#MyAppName}
UninstallFilesDir={app}\uninst

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; Check: not IsAdminInstallMode

[Files]
; Main application executable
Source: "bin\Release\net9.0\win-x64\publish\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

; All other files from publish directory
Source: "bin\Release\net9.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Configuration files (if not already included above)
Source: "bin\Release\net9.0\win-x64\publish\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{app}\appsettings.json'))
Source: "bin\Release\net9.0\win-x64\publish\appsettings.Production.json"; DestDir: "{app}"; Flags: ignoreversion; Check: FileExists(ExpandConstant('{app}\appsettings.Production.json'))

; Documentation
Source: "README.md"; DestDir: "{app}"; Flags: ignoreversion isreadme
Source: "LICENSE.md"; DestDir: "{app}"; Flags: ignoreversion

; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userappdata}\Microsoft\Internet Explorer\Quick Launch\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: quicklaunchicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// Custom code for checking .NET runtime
function IsDotNetInstalled(): Boolean;
var
  ResultCode: Integer;
  DotNetVersion: String;
begin
  Result := False;
  
  // Check if dotnet command is available
  if Exec('cmd.exe', '/c dotnet --version', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    if ResultCode = 0 then
    begin
      Result := True;
    end;
  end;
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  
  // Check for .NET 9 runtime (optional check)
  // Uncomment the following lines if you want to enforce .NET 9 runtime check
  {
  if not IsDotNetInstalled() then
  begin
    if MsgBox('.NET 9 Runtime does not appear to be installed. ' +
              'This application requires .NET 9 to run.' + #13#10#13#10 +
              'Would you like to download and install .NET 9 now?' + #13#10 +
              '(The installation will continue, but the application may not run without .NET 9)',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
        'https://dotnet.microsoft.com/download/dotnet/9.0',
        '', '', SW_SHOW, ewNoWait, ErrorCode);
    end;
  end;
  }
end;

// Clean up user data on uninstall (optional)
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  AppDataDir: String;
  ResultCode: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    AppDataDir := ExpandConstant('{userappdata}\ECoopSystem');
    
    if DirExists(AppDataDir) then
    begin
      if MsgBox('Do you want to remove all application data and settings?' + #13#10 +
                'This includes configuration files, data protection keys, and cached data.' + #13#10#13#10 +
                'Location: ' + AppDataDir,
                mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(AppDataDir, True, True, True);
      end;
    end;
  end;
end;

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
