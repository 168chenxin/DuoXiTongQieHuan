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

$iconPath = Join-Path $buildDirectory 'DualBootSwitcher.ico'
& (Join-Path $root 'tools\generate-icon.ps1') -OutputPath $iconPath

$executablePath = Join-Path $outputDirectory 'DualBootSwitcher.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:winexe /platform:anycpu `
    "/out:$executablePath" `
    "/win32icon:$iconPath" `
    "/win32manifest:$root\src\app.manifest" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$root\src\AssemblyInfo.cs" `
    "$root\src\BcdModels.cs" `
    "$root\src\BcdParser.cs" `
    "$root\src\BcdService.cs" `
    "$root\src\BootRemarkStore.cs" `
    "$root\src\UiTheme.cs" `
    "$root\src\UiControls.cs" `
    "$root\src\RemarkDialog.cs" `
    "$root\src\MainForm.cs" `
    "$root\src\Program.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE."
}

$packagePath = Join-Path $outputDirectory 'DualBootSwitcher-portable.zip'
Compress-Archive -LiteralPath $executablePath, (Join-Path $root 'README.md'), (Join-Path $root 'assets\THIRD_PARTY_NOTICES.md') `
    -DestinationPath $packagePath -Force

Write-Host "Built: $executablePath"
Write-Host "Packaged: $packagePath"
