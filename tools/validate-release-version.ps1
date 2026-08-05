param(
    [Parameter(Mandatory = $true)]
    [string]$Tag
)

$ErrorActionPreference = 'Stop'

if (-not $Tag.StartsWith('v')) {
    throw "Release tag must start with v: $Tag"
}

$root = Split-Path -Parent $PSScriptRoot
$releaseVersion = $Tag.Substring(1)
$assemblyText = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\AssemblyInfo.cs')
$assemblyVersion = [regex]::Match(
    $assemblyText,
    'AssemblyVersion\("([0-9.]+)"\)'
).Groups[1].Value

if (-not $assemblyVersion.StartsWith($releaseVersion + '.')) {
    throw "Tag $Tag does not match AssemblyVersion $assemblyVersion."
}

$changelogPath = Join-Path $root 'CHANGELOG.md'
if (-not (Select-String -Path $changelogPath -Pattern "^## v$releaseVersion$" -Quiet)) {
    throw "CHANGELOG.md has no section for $Tag."
}

Write-Host "Release version validation passed: $Tag ($assemblyVersion)."
