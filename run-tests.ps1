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
    "$root\tests\BcdParserTests.cs"

if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $testExecutable

if ($LASTEXITCODE -ne 0) {
    throw "Tests failed with exit code $LASTEXITCODE."
}
