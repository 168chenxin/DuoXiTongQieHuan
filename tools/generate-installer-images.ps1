param(
    [Parameter(Mandatory = $true)]
    [string]$WizardImagePath,
    [Parameter(Mandatory = $true)]
    [string]$WizardSmallImagePath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourcePath = Join-Path $root '..\assets\dual-boot-switcher-logo.png'
$sourceImage = [System.Drawing.Image]::FromFile((Resolve-Path -LiteralPath $sourcePath))

function Save-WizardImage {
    param([string]$Path, [int]$Width, [int]$Height)

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.Clear([System.Drawing.Color]::White)
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        if ($Width -le 55) {
            $smallestDimension = [Math]::Min([int]$Width, [int]$Height)
            $size = $smallestDimension - 10
            $x = [int](($Width - $size) / 2)
            $y = [int](($Height - $size) / 2)
            $graphics.DrawImage($sourceImage, $x, $y, $size, $size)
        }
        else {
            $availableWidth = [int]$Width - 28
            $availableHeight = [int]$Height - 84
            $size = [Math]::Min($availableWidth, $availableHeight)
            $x = [int](($Width - $size) / 2)
            $graphics.DrawImage($sourceImage, $x, 24, $size, $size)
            $titleFont = New-Object System.Drawing.Font('Microsoft YaHei UI', 13, [System.Drawing.FontStyle]::Bold)
            $textFont = New-Object System.Drawing.Font('Microsoft YaHei UI', 8)
            try {
                $format = New-Object System.Drawing.StringFormat
                $format.Alignment = [System.Drawing.StringAlignment]::Center
                $titleBounds = [System.Drawing.RectangleF]::new(0, [single]($Height - 54), $Width, 24)
                $authorBounds = [System.Drawing.RectangleF]::new(0, [single]($Height - 29), $Width, 18)
                $graphics.DrawString('多系统切换', $titleFont, [System.Drawing.Brushes]::Black, $titleBounds, $format)
                $graphics.DrawString('作者：称心', $textFont, [System.Drawing.Brushes]::DimGray, $authorBounds, $format)
                $format.Dispose()
            }
            finally {
                $titleFont.Dispose()
                $textFont.Dispose()
            }
        }
        $directory = Split-Path -Parent ([System.IO.Path]::GetFullPath($Path))
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

try {
    Save-WizardImage -Path $WizardImagePath -Width 164 -Height 314
    Save-WizardImage -Path $WizardSmallImagePath -Width 55 -Height 55
}
finally {
    $sourceImage.Dispose()
}
