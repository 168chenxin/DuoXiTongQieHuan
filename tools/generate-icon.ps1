param(
    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [single]$X,
        [single]$Y,
        [single]$Width,
        [single]$Height,
        [single]$Radius
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $path.AddArc($X, $Y, $diameter, $diameter, 180, 90)
    $path.AddArc($X + $Width - $diameter, $Y, $diameter, $diameter, 270, 90)
    $path.AddArc($X + $Width - $diameter, $Y + $Height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc($X, $Y + $Height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-ApplicationBitmap {
    param([int]$Size)

    $scale = $Size / 256.0
    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    try {
        $backgroundPath = New-RoundedRectanglePath (18 * $scale) (18 * $scale) (220 * $scale) (220 * $scale) (50 * $scale)
        $backgroundBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(99, 102, 241))
        $graphics.FillPath($backgroundBrush, $backgroundPath)

        $screenOuter = New-RoundedRectanglePath (40 * $scale) (79 * $scale) (133 * $scale) (76 * $scale) (12 * $scale)
        $screenBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255))
        $graphics.FillPath($screenBrush, $screenOuter)

        $screenInner = New-RoundedRectanglePath (51 * $scale) (90 * $scale) (111 * $scale) (54 * $scale) (5 * $scale)
        $screenInnerBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(30, 41, 59))
        $graphics.FillPath($screenInnerBrush, $screenInner)

        $standPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 255, 255), (10 * $scale))
        $standPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $standPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLine($standPen, (106 * $scale), (156 * $scale), (106 * $scale), (180 * $scale))
        $graphics.DrawLine($standPen, (79 * $scale), (181 * $scale), (133 * $scale), (181 * $scale))

        $gearCenterX = 184 * $scale
        $gearCenterY = 68 * $scale
        $gearPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(129, 140, 248), (13 * $scale))
        $gearPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $gearPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

        for ($index = 0; $index -lt 8; $index++) {
            $angle = (($index * 45) - 90) * [Math]::PI / 180
            $innerRadius = 29 * $scale
            $outerRadius = 43 * $scale
            $graphics.DrawLine(
                $gearPen,
                $gearCenterX + ([Math]::Cos($angle) * $innerRadius),
                $gearCenterY + ([Math]::Sin($angle) * $innerRadius),
                $gearCenterX + ([Math]::Cos($angle) * $outerRadius),
                $gearCenterY + ([Math]::Sin($angle) * $outerRadius))
        }

        $gearBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(129, 140, 248))
        $graphics.FillEllipse($gearBrush, $gearCenterX - (32 * $scale), $gearCenterY - (32 * $scale), (64 * $scale), (64 * $scale))
        $gearCenterBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(99, 102, 241))
        $graphics.FillEllipse($gearCenterBrush, $gearCenterX - (13 * $scale), $gearCenterY - (13 * $scale), (26 * $scale), (26 * $scale))
    }
    finally {
        if ($backgroundPath) { $backgroundPath.Dispose() }
        if ($backgroundBrush) { $backgroundBrush.Dispose() }
        if ($screenOuter) { $screenOuter.Dispose() }
        if ($screenBrush) { $screenBrush.Dispose() }
        if ($screenInner) { $screenInner.Dispose() }
        if ($screenInnerBrush) { $screenInnerBrush.Dispose() }
        if ($standPen) { $standPen.Dispose() }
        if ($gearPen) { $gearPen.Dispose() }
        if ($gearBrush) { $gearBrush.Dispose() }
        if ($gearCenterBrush) { $gearCenterBrush.Dispose() }
        $graphics.Dispose()
    }

    return $bitmap
}

$resolvedOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $resolvedOutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$payloads = @()

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
