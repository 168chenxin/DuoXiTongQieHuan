param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath,
    [string]$PngOutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $scriptRoot '..\assets\SysSwitch-logo.png'

if (-not (Test-Path -LiteralPath $sourcePath)) {
    throw "The logo source was not found: $sourcePath"
}

$sourceImage = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $sourcePath))

function Get-AlphaBounds {
    param([System.Drawing.Bitmap]$Image)

    $left = $Image.Width
    $top = $Image.Height
    $right = -1
    $bottom = -1

    for ($y = 0; $y -lt $Image.Height; $y++) {
        for ($x = 0; $x -lt $Image.Width; $x++) {
            if ($Image.GetPixel($x, $y).A -gt 0) {
                if ($x -lt $left) { $left = $x }
                if ($y -lt $top) { $top = $y }
                if ($x -gt $right) { $right = $x }
                if ($y -gt $bottom) { $bottom = $y }
            }
        }
    }

    if ($right -lt $left -or $bottom -lt $top) {
        return New-Object System.Drawing.Rectangle(0, 0, $Image.Width, $Image.Height)
    }

    return New-Object System.Drawing.Rectangle(
        $left,
        $top,
        ($right - $left + 1),
        ($bottom - $top + 1))
}

$sourceBounds = Get-AlphaBounds $sourceImage

function New-ApplicationBitmap {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy

    try {
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $inset = $Size * 0.03
        $usableSize = $Size - (2 * $inset)
        $sourceAspect = $sourceBounds.Width / [double]$sourceBounds.Height
        if ($sourceAspect -ge 1) {
            $destinationWidth = $usableSize
            $destinationHeight = $usableSize / $sourceAspect
            $destinationX = ($Size - $destinationWidth) / 2
            $destinationY = ($Size - $destinationHeight) / 2
        }
        else {
            $destinationHeight = $usableSize
            $destinationWidth = $usableSize * $sourceAspect
            $destinationX = ($Size - $destinationWidth) / 2
            $destinationY = ($Size - $destinationHeight) / 2
        }

        $destination = New-Object System.Drawing.Rectangle(
            [int][Math]::Round($destinationX),
            [int][Math]::Round($destinationY),
            [int][Math]::Round($destinationWidth),
            [int][Math]::Round($destinationHeight))
        $graphics.DrawImage(
            $sourceImage,
            $destination,
            $sourceBounds.X,
            $sourceBounds.Y,
            $sourceBounds.Width,
            $sourceBounds.Height,
            [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally {
        $graphics.Dispose()
    }

    return $bitmap
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$payloads = @()
$resolvedPngOutputPath = $null

try {
    foreach ($size in $sizes) {
        $bitmap = New-ApplicationBitmap $size
        $memory = New-Object System.IO.MemoryStream
        try {
            $bitmap.Save($memory, [System.Drawing.Imaging.ImageFormat]::Png)
            $payloads += [PSCustomObject]@{
                Size = $size
                Bytes = $memory.ToArray()
            }
        }
        finally {
            $memory.Dispose()
            $bitmap.Dispose()
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($PngOutputPath)) {
        $resolvedPngOutputPath = [System.IO.Path]::GetFullPath($PngOutputPath)
        $pngOutputDirectory = Split-Path -Parent $resolvedPngOutputPath
        New-Item -ItemType Directory -Force -Path $pngOutputDirectory | Out-Null

        $embeddedBitmap = New-ApplicationBitmap 256
        try {
            $embeddedBitmap.Save($resolvedPngOutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
        }
        finally {
            $embeddedBitmap.Dispose()
        }
    }
}
finally {
    $sourceImage.Dispose()
}

$stream = [System.IO.File]::Open($resolvedOutputPath, [System.IO.FileMode]::Create)
$writer = New-Object System.IO.BinaryWriter($stream)

try {
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$payloads.Count)

    $offset = 6 + (16 * $payloads.Count)
    foreach ($payload in $payloads) {
        $dimension = if ($payload.Size -eq 256) { [byte]0 } else { [byte]$payload.Size }
        $writer.Write($dimension)
        $writer.Write($dimension)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$payload.Bytes.Length)
        $writer.Write([UInt32]$offset)
        $offset += $payload.Bytes.Length
    }

    foreach ($payload in $payloads) {
        $writer.Write([byte[]]$payload.Bytes)
    }
}
finally {
    $writer.Dispose()
    $stream.Dispose()
}

Write-Host "Generated: $resolvedOutputPath"
if ($resolvedPngOutputPath) {
    Write-Host "Generated: $resolvedPngOutputPath"
}
