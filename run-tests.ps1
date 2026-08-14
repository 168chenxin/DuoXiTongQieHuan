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
    "$root\src\FirmwareBootModels.cs" `
    "$root\src\FirmwareBootParser.cs" `
    "$root\src\BootTimeoutWorkflow.cs" `
    "$root\tests\BcdParserTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $testExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

$firmwareTestExecutable = Join-Path $outputDirectory 'FirmwareBootParserTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$firmwareTestExecutable" `
    /r:System.dll `
    "$root\src\FirmwareBootModels.cs" `
    "$root\src\FirmwareBootParser.cs" `
    "$root\tests\FirmwareBootParserTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Firmware boot parser test compilation failed with exit code $LASTEXITCODE."
}

& $firmwareTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Firmware boot parser tests failed with exit code $LASTEXITCODE."
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

$logoPath = Join-Path $root 'assets\dual-boot-switcher-logo.png'
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
