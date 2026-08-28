$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDirectory = Join-Path $root 'build'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$testExecutable = Join-Path $outputDirectory 'BcdParserTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$testExecutable" `
    /r:System.dll `
    "$root\src\BcdModels.cs" `
    "$root\src\BcdCommandResult.cs" `
    "$root\src\BcdParser.cs" `
    "$root\src\BcdService.cs" `
    "$root\src\BootNameValidator.cs" `
    "$root\src\BootNameStore.cs" `
    "$root\src\BootTimeoutWorkflow.cs" `
    "$root\tests\BcdParserTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $testExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

$announcementTestExecutable = Join-Path $outputDirectory 'AnnouncementParserTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$announcementTestExecutable" `
    /r:System.dll `
    "$root\src\AnnouncementModels.cs" `
    "$root\src\AnnouncementParser.cs" `
    "$root\tests\AnnouncementParserTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Announcement parser test compilation failed with exit code $LASTEXITCODE."
}

& $announcementTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Announcement parser tests failed with exit code $LASTEXITCODE."
}

$uiTestExecutable = Join-Path $outputDirectory 'UiMotionTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$uiTestExecutable" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$root\src\UiTheme.cs" `
    "$root\src\UiMotion.cs" `
    "$root\src\UiMotionExtensions.cs" `
    "$root\src\UiControls.cs" `
    "$root\tests\UiMotionTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "UI test compilation failed with exit code $LASTEXITCODE."
}

& $uiTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "UI tests failed with exit code $LASTEXITCODE."
}

$dependencyDirectory = Join-Path $outputDirectory 'dependencies'
$antdUiAssembly = Join-Path $dependencyDirectory 'AntdUI.dll'
if (-not (Test-Path -LiteralPath $antdUiAssembly)) {
    & (Join-Path $root 'tools\restore-ui-dependencies.ps1') -OutputDirectory $dependencyDirectory
}

$antdUiTestExecutable = Join-Path $dependencyDirectory 'AntdUiThemeTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$antdUiTestExecutable" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/r:$antdUiAssembly" `
    "$root\src\UiTheme.cs" `
    "$root\src\UiMotion.cs" `
    "$root\src\UiMotionExtensions.cs" `
    "$root\src\UiControls.cs" `
    "$root\src\AntdUiTheme.cs" `
    "$root\src\StyledDialogForm.cs" `
    "$root\src\ApplicationDialog.cs" `
    "$root\tests\AntdUiThemeTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "AntdUI theme test compilation failed with exit code $LASTEXITCODE."
}

& $antdUiTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "AntdUI theme tests failed with exit code $LASTEXITCODE."
}

$updateTestExecutable = Join-Path $outputDirectory 'UpdateServiceTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$updateTestExecutable" `
    /r:System.dll /r:System.Runtime.Serialization.dll `
    "$root\src\UpdateService.cs" `
    "$root\tests\UpdateServiceTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Update service test compilation failed with exit code $LASTEXITCODE."
}

& $updateTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Update service tests failed with exit code $LASTEXITCODE."
}

$remarkTestExecutable = Join-Path $outputDirectory 'BootRemarkStoreTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$remarkTestExecutable" `
    /r:System.dll `
    "$root\src\BootRemarkStore.cs" `
    "$root\tests\BootRemarkStoreTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Boot remark test compilation failed with exit code $LASTEXITCODE."
}

& $remarkTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Boot remark tests failed with exit code $LASTEXITCODE."
}

$nameStoreTestExecutable = Join-Path $outputDirectory 'BootNameStoreTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$nameStoreTestExecutable" `
    /r:System.dll /r:System.Core.dll `
    "$root\src\BootNameStore.cs" `
    "$root\tests\BootNameStoreTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Boot name store test compilation failed with exit code $LASTEXITCODE."
}

& $nameStoreTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Boot name store tests failed with exit code $LASTEXITCODE."
}

$brandMigrationTestExecutable = Join-Path $outputDirectory 'BrandMigrationTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$brandMigrationTestExecutable" `
    /r:System.dll /r:System.Windows.Forms.dll `
    "$root\src\BrandMigration.cs" `
    "$root\tests\BrandMigrationTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Brand migration test compilation failed with exit code $LASTEXITCODE."
}

& $brandMigrationTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Brand migration tests failed with exit code $LASTEXITCODE."
}

$logoPath = Join-Path $root 'assets\SysSwitch-logo.png'
if (-not (Test-Path -LiteralPath $logoPath)) {
    throw 'The embedded logo source is missing.'
}

Write-Host 'Embedded logo source test passed.'

$antdLicensePath = Join-Path $root 'assets\licenses\AntdUI-Apache-2.0.txt'
if (-not (Test-Path -LiteralPath $antdLicensePath)) {
    throw 'The AntdUI Apache 2.0 license is missing.'
}

if (-not (Select-String -Path $antdLicensePath -Pattern 'Apache License' -Quiet)) {
    throw 'The AntdUI license file does not contain the Apache License text.'
}

$thirdPartyNotices = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'assets\THIRD_PARTY_NOTICES.md')
if (-not $thirdPartyNotices.Contains('AntdUI 2.4.4')) {
    throw 'Third-party notices must identify the embedded AntdUI version.'
}

Write-Host 'Embedded UI dependency license test passed.'

$orbienLicensePath = Join-Path $root 'assets\licenses\OrbiEn-Apache-2.0.txt'
if (-not (Test-Path -LiteralPath $orbienLicensePath)) {
    throw 'The OrbiEn Apache 2.0 license is missing.'
}

if (-not (Select-String -Path $orbienLicensePath -Pattern 'Apache License' -Quiet)) {
    throw 'The OrbiEn license file does not contain the Apache License text.'
}

if (-not $thirdPartyNotices.Contains('OrbiEn Desktop')) {
    throw 'Third-party notices must identify the adapted OrbiEn Desktop design.'
}

Write-Host 'OrbiEn design attribution test passed.'

[xml]$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\app.manifest')
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$namespaceManager.AddNamespace('asmv3', 'urn:schemas-microsoft-com:asm.v3')
$elevationNode = $manifest.SelectSingleNode('//asmv3:requestedExecutionLevel', $namespaceManager)

if ($null -eq $elevationNode -or $elevationNode.GetAttribute('level') -ne 'requireAdministrator') {
    throw 'The application manifest must request administrator privileges.'
}

Write-Host 'Manifest elevation test passed.'

Write-Host 'Boot timeout save behavior test passed.'

$installerScriptPath = Join-Path $root 'installer\SysSwitch.iss'
if (-not (Test-Path -LiteralPath $installerScriptPath)) {
    throw 'The Inno Setup installer script is missing.'
}

$installerScript = Get-Content -Raw -Encoding UTF8 $installerScriptPath
$requiredInstallerSettings = @(
    '#define MyAppExeName "SysSwitch.exe"',
    'AppName=系统切换大师',
    'AppPublisher=称心',
    'AppContact=https://github.com/168chenxin/SysSwitch-Master',
    'VersionInfoDescription=系统切换大师安装程序',
    'VersionInfoProductName=系统切换大师',
    'DefaultDirName={code:GetDefaultInstallDir}',
    'AppendDefaultDirName=no',
    'DisableDirPage=no',
    'DisableWelcomePage=no',
    'OutputBaseFilename=系统切换大师-安装包',
    'WizardImageFile=..\build\SysSwitch-wizard.bmp',
    'WizardSmallImageFile=..\build\SysSwitch-wizard-small.bmp',
    'MessagesFile: "ChineseSimplified.isl"',
    'Name: "desktopicon"; Description: "创建桌面快捷方式"',
    'Name: "{autoprograms}\系统切换大师"; Filename: "{app}\SysSwitch.exe"',
    'Name: "{autodesktop}\系统切换大师"; Filename: "{app}\SysSwitch.exe"; Tasks: desktopicon',
    'function GetDefaultInstallDir(Param: String): String;',
    "Result := 'D:\SysSwitch'",
    "Result := ExpandConstant('{autopf}\SysSwitch');",
    '用于管理 Windows 启动菜单中的默认系统和启动等待时间。',
    '欢迎使用系统切换大师',
    '作者：称心'
)

foreach ($setting in $requiredInstallerSettings) {
    if (-not $installerScript.Contains($setting)) {
        throw "Installer script must include: $setting"
    }
}

if ($installerScript.Contains('Name: "desktopicon"; Description: "创建桌面快捷方式"; Flags: unchecked')) {
    throw 'The desktop shortcut task must be selected by default.'
}

Write-Host 'Installer configuration test passed.'

$legacyBrandPatterns = @(
    '多系统切换',
    'Dual Boot Switcher',
    'DualBootSwitcher',
    'DuoXiTongQieHuan',
    'DXTQH',
    'dual-boot-switcher'
)
$trackedTextFiles = git -C $root ls-files | Where-Object {
    $_ -notin @('run-tests.ps1', 'CHANGELOG.md') -and ($_ -match '\.(cs|ps1|iss|md|yml|xml|manifest)$' -or $_ -eq 'LICENSE')
}

foreach ($relativePath in $trackedTextFiles) {
    $lineNumber = 0
    foreach ($line in Get-Content -Encoding UTF8 (Join-Path $root $relativePath)) {
        $lineNumber++
        foreach ($legacyBrand in $legacyBrandPatterns) {
            if ($line.Contains($legacyBrand) -and $line -notmatch 'Legacy') {
                throw "Legacy brand '$legacyBrand' remains in ${relativePath}:$lineNumber."
            }
        }
    }
}

Write-Host 'Brand naming test passed.'
