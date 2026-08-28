#ifndef MyAppVersion
  #error MyAppVersion must be provided by build.ps1.
#endif

#define MyAppExeName "SysSwitch.exe"
#define LegacyAppName "多系统切换"
#define LegacyExeName "DualBootSwitcher.exe"

[Setup]
AppId={{7D3F8B5A-BF2D-4BD8-A6C0-1B2B90F9E2C1}
AppName=系统切换大师
AppVersion={#MyAppVersion}
AppPublisher=称心
AppComments=用于管理 Windows 启动菜单中的默认系统和启动等待时间。
AppContact=https://github.com/168chenxin/SysSwitch-Master
VersionInfoDescription=系统切换大师安装程序
VersionInfoProductName=系统切换大师
DefaultDirName={code:GetDefaultInstallDir}
AppendDefaultDirName=no
DisableDirPage=no
DisableProgramGroupPage=yes
DisableWelcomePage=no
OutputDir=..\release
OutputBaseFilename=系统切换大师-安装包
SetupIconFile=..\build\SysSwitch.ico
WizardImageFile=..\build\SysSwitch-wizard.bmp
WizardSmallImageFile=..\build\SysSwitch-wizard-small.bmp
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=..\LICENSE
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"

[Files]
Source: "..\release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
Type: files; Name: "{app}\{#LegacyExeName}"
Type: files; Name: "{autoprograms}\{#LegacyAppName}.lnk"
Type: files; Name: "{autodesktop}\{#LegacyAppName}.lnk"

[Icons]
Name: "{autoprograms}\系统切换大师"; Filename: "{app}\SysSwitch.exe"
Name: "{autodesktop}\系统切换大师"; Filename: "{app}\SysSwitch.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动系统切换大师"; Flags: nowait postinstall skipifsilent

[Code]
function GetDefaultInstallDir(Param: String): String;
begin
  if DirExists('D:\') then
    Result := 'D:\SysSwitch'
  else
    Result := ExpandConstant('{autopf}\SysSwitch');
end;

procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel1.Caption := '欢迎使用系统切换大师';
  WizardForm.WelcomeLabel2.Caption :=
    '系统切换大师用于管理 Windows 启动菜单中的默认系统和启动等待时间。' + #13#10 + #13#10 +
    '作者：称心';
end;
