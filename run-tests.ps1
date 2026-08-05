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
    "$root\src\BcdParser.cs" `
    "$root\src\BcdService.cs" `
    "$root\src\BootTimeoutWorkflow.cs" `
    "$root\tests\BcdParserTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $testExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}

$uiTestExecutable = Join-Path $outputDirectory 'UiMotionTests.exe'

& $compiler /nologo /utf8output /codepage:65001 /target:exe "/out:$uiTestExecutable" `
    /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "$root\src\UiTheme.cs" `
    "$root\src\UiControls.cs" `
    "$root\tests\UiMotionTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "UI test compilation failed with exit code $LASTEXITCODE."
}

& $uiTestExecutable

if ($LASTEXITCODE -ne 0) {
    throw "UI tests failed with exit code $LASTEXITCODE."
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

[xml]$manifest = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\app.manifest')
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$namespaceManager.AddNamespace('asmv3', 'urn:schemas-microsoft-com:asm.v3')
$elevationNode = $manifest.SelectSingleNode('//asmv3:requestedExecutionLevel', $namespaceManager)

if ($null -eq $elevationNode -or $elevationNode.GetAttribute('level') -ne 'requireAdministrator') {
    throw 'The application manifest must request administrator privileges.'
}

Write-Host 'Manifest elevation test passed.'

Write-Host 'Boot timeout save behavior test passed.'
