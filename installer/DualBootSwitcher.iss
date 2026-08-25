#ifndef MyAppVersion
  #error MyAppVersion must be provided by build.ps1.
#endif

#define MyAppExeName "DualBootSwitcher.exe"

[Setup]
AppId={{7D3F8B5A-BF2D-4BD8-A6C0-1B2B90F9E2C1}
AppName=多系统切换
AppVersion={#MyAppVersion}
AppPublisher=称心
AppComments=用于管理 Windows 启动菜单中的默认系统和启动等待时间。
AppContact=https://github.com/168chenxin/DuoXiTongQieHuan
DefaultDirName={code:GetDefaultInstallDir}
DisableDirPage=no
DisableProgramGroupPage=yes
DisableWelcomePage=no
OutputDir=..\release
OutputBaseFilename=DualBootSwitcher-Setup
SetupIconFile=..\build\DualBootSwitcher.ico
WizardImageFile=..\build\DualBootSwitcher-wizard.bmp
WizardSmallImageFile=..\build\DualBootSwitcher-wizard-small.bmp
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

[Icons]
Name: "{autoprograms}\多系统切换"; Filename: "{app}\DualBootSwitcher.exe"
Name: "{autodesktop}\多系统切换"; Filename: "{app}\DualBootSwitcher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动多系统切换"; Flags: nowait postinstall skipifsilent

[Code]
function GetDefaultInstallDir(Param: String): String;
begin
  if DirExists('D:\') then
    Result := 'D:\DXTQH'
  else
    Result := ExpandConstant('{autopf}\DXTQH');
end;

procedure InitializeWizard();
begin
  WizardForm.WelcomeLabel1.Caption := '欢迎使用多系统切换';
  WizardForm.WelcomeLabel2.Caption :=
    '多系统切换用于管理 Windows 启动菜单中的默认系统和启动等待时间。' + #13#10 + #13#10 +
    '作者：称心';
end;
