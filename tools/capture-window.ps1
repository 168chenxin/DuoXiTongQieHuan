param(
    [Parameter(Mandatory = $true)]
    [int]$ProcessId,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [switch]$Screen
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class WindowCaptureNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr window, out RECT rectangle);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr window, IntPtr deviceContext, uint flags);
}
'@

$process = Get-Process -Id $ProcessId
if ($process.MainWindowHandle -eq [IntPtr]::Zero) {
    throw "Process $ProcessId has no main window."
}

$rectangle = New-Object WindowCaptureNative+RECT
if (-not [WindowCaptureNative]::GetWindowRect($process.MainWindowHandle, [ref]$rectangle)) {
    throw "Unable to read the window bounds for process $ProcessId."
}

$width = $rectangle.Right - $rectangle.Left
$height = $rectangle.Bottom - $rectangle.Top
$bitmap = New-Object System.Drawing.Bitmap $width, $height
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
if ($Screen) {
    $graphics.CopyFromScreen(
        $rectangle.Left,
        $rectangle.Top,
        0,
        0,
        (New-Object System.Drawing.Size $width, $height))
}
else {
    $deviceContext = $graphics.GetHdc()
    try {
        if (-not [WindowCaptureNative]::PrintWindow($process.MainWindowHandle, $deviceContext, 2)) {
            throw "Unable to capture the window for process $ProcessId."
        }
    }
    finally {
        $graphics.ReleaseHdc($deviceContext)
    }
}

$graphics.Dispose()

$absoluteOutputPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = Split-Path -Parent $absoluteOutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$bitmap.Save($absoluteOutputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host "Captured: $absoluteOutputPath"
