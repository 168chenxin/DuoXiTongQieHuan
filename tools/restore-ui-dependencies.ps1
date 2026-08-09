param(
    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'

$packageVersion = '2.4.4'
$packageHash = '1FD4B1E8E42429B54F3382644B80BD73944AEA1C2FFA78212333B331954AC35F'
$assemblyHash = 'DDC586F04812F17CB77FAC4D980C380726C215B9805D5893F3C369D693C9E239'
$packageUrl = "https://api.nuget.org/v3-flatcontainer/antdui/$packageVersion/antdui.$packageVersion.nupkg"
$assemblyPath = Join-Path $OutputDirectory 'AntdUI.dll'

if (Test-Path -LiteralPath $assemblyPath) {
    $cachedAssemblyHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $assemblyPath).Hash
    if ($cachedAssemblyHash -ne $assemblyHash) {
        throw "Cached AntdUI assembly hash mismatch. Expected $assemblyHash; actual $cachedAssemblyHash."
    }

    Write-Host "UI dependency ready: $assemblyPath"
    return
}

$restoreDirectory = Join-Path $OutputDirectory '.restore'
$packagePath = Join-Path $restoreDirectory "AntdUI.$packageVersion.zip"
$extractDirectory = Join-Path $restoreDirectory "AntdUI.$packageVersion"

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $restoreDirectory | Out-Null

Invoke-WebRequest -UseBasicParsing -Uri $packageUrl -OutFile $packagePath
$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packagePath).Hash
if ($actualHash -ne $packageHash) {
    throw "AntdUI package hash mismatch. Expected $packageHash; actual $actualHash."
}

Expand-Archive -LiteralPath $packagePath -DestinationPath $extractDirectory -Force
Copy-Item -LiteralPath (Join-Path $extractDirectory 'lib\net40\AntdUI.dll') -Destination $assemblyPath
$restoredAssemblyHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $assemblyPath).Hash
if ($restoredAssemblyHash -ne $assemblyHash) {
    throw "Restored AntdUI assembly hash mismatch. Expected $assemblyHash; actual $restoredAssemblyHash."
}

Write-Host "Restored AntdUI ${packageVersion}: $assemblyPath"
