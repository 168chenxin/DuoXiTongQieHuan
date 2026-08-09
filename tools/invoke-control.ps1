param(
    [Parameter(Mandatory = $true)]
    [int]$ProcessId,

    [Parameter(Mandatory = $true)]
    [string]$AccessibleName,

    [int]$ClientX = -1,

    [int]$ClientY = -1
)

$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class ControlInvokeNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(IntPtr window, ref POINT point);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);
}
'@

$process = Get-Process -Id $ProcessId
$root = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
$condition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty,
    $AccessibleName)
$control = $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    $condition)

if ($null -eq $control) {
    if ($ClientX -lt 0 -or $ClientY -lt 0) {
        throw "Unable to find control '$AccessibleName' in process $ProcessId."
    }

    $point = New-Object ControlInvokeNative+POINT
    $point.X = $ClientX
    $point.Y = $ClientY
    [ControlInvokeNative]::SetForegroundWindow($process.MainWindowHandle) | Out-Null
    [ControlInvokeNative]::ClientToScreen($process.MainWindowHandle, [ref]$point) | Out-Null
    [ControlInvokeNative]::SetCursorPos($point.X, $point.Y) | Out-Null
    [ControlInvokeNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [ControlInvokeNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Write-Host "Clicked client point: $ClientX, $ClientY"
    return
}

$invokePattern = $control.GetCurrentPattern(
    [System.Windows.Automation.InvokePattern]::Pattern)
$invokePattern.Invoke()
Write-Host "Invoked: $AccessibleName"
