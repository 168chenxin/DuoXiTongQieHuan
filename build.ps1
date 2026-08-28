$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputDirectory = Join-Path $root 'release'
$buildDirectory = Join-Path $root 'build'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}

if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework C# compiler was not found.'
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null

$dependencyDirectory = Join-Path $buildDirectory 'dependencies'
& (Join-Path $root 'tools\restore-ui-dependencies.ps1') -OutputDirectory $dependencyDirectory
$antdUiAssembly = Join-Path $dependencyDirectory 'AntdUI.dll'

$iconPath = Join-Path $buildDirectory 'SysSwitch.ico'
$embeddedLogoPath = Join-Path $buildDirectory 'SysSwitch-logo.png'
& (Join-Path $root 'tools\generate-icon.ps1') -OutputPath $iconPath -PngOutputPath $embeddedLogoPath

$wizardImagePath = Join-Path $buildDirectory 'SysSwitch-wizard.bmp'
$wizardSmallImagePath = Join-Path $buildDirectory 'SysSwitch-wizard-small.bmp'
& (Join-Path $root 'tools\generate-installer-images.ps1') -WizardImagePath $wizardImagePath -WizardSmallImagePath $wizardSmallImagePath

$executablePath = Join-Path $outputDirectory 'SysSwitch.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:winexe /platform:anycpu `
    "/out:$executablePath" `
    "/win32icon:$iconPath" `
    "/win32manifest:$root\src\app.manifest" `
    "/resource:$embeddedLogoPath,SysSwitch.Logo.png" `
    "/resource:$root\LICENSE,SysSwitch.LICENSE.txt" `
    "/resource:$root\assets\licenses\AntdUI-Apache-2.0.txt,SysSwitch.Licenses.AntdUI-Apache-2.0.txt" `
    "/resource:$root\assets\licenses\OrbiEn-Apache-2.0.txt,SysSwitch.Licenses.OrbiEn-Apache-2.0.txt" `
    "/resource:$root\assets\THIRD_PARTY_NOTICES.md,SysSwitch.THIRD_PARTY_NOTICES.md" `
    "/resource:$antdUiAssembly,SysSwitch.Dependencies.AntdUI.dll" `
    /r:System.dll /r:System.Design.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    /r:System.Runtime.Serialization.dll `
    "/r:$antdUiAssembly" `
    "$root\src\AssemblyInfo.cs" `
    "$root\src\EmbeddedAssemblyLoader.cs" `
    "$root\src\BcdModels.cs" `
    "$root\src\BcdCommandResult.cs" `
    "$root\src\BcdParser.cs" `
    "$root\src\BcdService.cs" `
    "$root\src\BootNameValidator.cs" `
    "$root\src\BootNameStore.cs" `
    "$root\src\UpdateService.cs" `
    "$root\src\BootRemarkStore.cs" `
    "$root\src\UiTheme.cs" `
    "$root\src\UiMotion.cs" `
    "$root\src\UiMotionExtensions.cs" `
    "$root\src\UiControls.cs" `
    "$root\src\AntdUiTheme.cs" `
    "$root\src\StyledDialogForm.cs" `
    "$root\src\ApplicationDialog.cs" `
    "$root\src\AnnouncementDialog.cs" `
    "$root\src\AnnouncementModels.cs" `
    "$root\src\AnnouncementParser.cs" `
    "$root\src\RemarkDialog.cs" `
    "$root\src\RenameDialog.cs" `
    "$root\src\BootTimeoutWorkflow.cs" `
    "$root\src\TimeoutDialog.cs" `
    "$root\src\MainForm.cs" `
    "$root\src\Program.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

$assemblyInfo = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\AssemblyInfo.cs')
$versionMatch = [regex]::Match($assemblyInfo, 'AssemblyVersion\("([0-9]+\.[0-9]+\.[0-9]+)\.0"\)')
if (-not $versionMatch.Success) {
    throw 'The application version could not be read from AssemblyInfo.cs.'
}

$installerCompiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($null -eq $installerCompiler) {
    throw 'Inno Setup 6 is required to build the installer.'
}

& $installerCompiler.Source "/DMyAppVersion=$($versionMatch.Groups[1].Value)" (Join-Path $root 'installer\SysSwitch.iss')

if ($LASTEXITCODE -ne 0) {
    throw "Installer build failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $outputDirectory '系统切换大师-安装包.exe'
if (-not (Test-Path -LiteralPath $installerPath)) {
    throw 'Installer build completed without 系统切换大师-安装包.exe.'
}

$packagePath = Join-Path $outputDirectory '系统切换大师-便携版.zip'
Compress-Archive -LiteralPath $executablePath, (Join-Path $root 'README.md'), (Join-Path $root 'CHANGELOG.md'), (Join-Path $root 'ANNOUNCEMENT.md'), (Join-Path $root 'LICENSE'), (Join-Path $root 'assets\THIRD_PARTY_NOTICES.md'), (Join-Path $root 'assets\licenses\AntdUI-Apache-2.0.txt'), (Join-Path $root 'assets\licenses\OrbiEn-Apache-2.0.txt') `
    -DestinationPath $packagePath -Force

Write-Host "Built: $executablePath"
Write-Host "Installed package: $installerPath"
Write-Host "Packaged: $packagePath"
