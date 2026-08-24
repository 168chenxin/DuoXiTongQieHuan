#ifndef MyAppVersion
  #error MyAppVersion must be provided by build.ps1.
#endif

#define MyAppName "多系统切换"
#define MyAppExeName "DualBootSwitcher.exe"

[Setup]
AppId={{7D3F8B5A-BF2D-4BD8-A6C0-1B2B90F9E2C1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=称心
DefaultDirName={autopf}\DualBootSwitcher
DisableDirPage=no
DisableProgramGroupPage=yes
OutputDir=..\release
OutputBaseFilename=DualBootSwitcher-Setup
SetupIconFile=..\build\DualBootSwitcher.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
LicenseFile=..\LICENSE
PrivilegesRequired=admin
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; Flags: unchecked

[Files]
Source: "..\release\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\多系统切换"; Filename: "{app}\DualBootSwitcher.exe"
Name: "{autodesktop}\多系统切换"; Filename: "{app}\DualBootSwitcher.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动多系统切换"; Flags: nowait postinstall skipifsilent
