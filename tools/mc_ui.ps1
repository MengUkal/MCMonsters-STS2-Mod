# STS2 UI 自动化助手（坑28：PostMessage 绕 IME、截图前 SetForegroundWindow、点击用绝对屏幕坐标）
# 用法：先 . .\mc_ui.ps1 导入函数
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class McWin {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
"@

function Get-GameWindow {
    $proc = Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
    if (-not $proc) { throw "游戏窗口未找到（未启动或无主窗口）" }
    return $proc.MainWindowHandle
}

function Focus-Game {
    $h = Get-GameWindow
    [McWin]::SetForegroundWindow($h) | Out-Null
    Start-Sleep -Milliseconds 800
    return $h
}

# 截图保存到指定路径（返回窗口 rect 供坐标换算）
function Get-GameShot([string]$path) {
    $h = Focus-Game
    $rect = New-Object McWin+RECT
    [McWin]::GetWindowRect($h, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $ht = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap($w, $ht)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $ht)))
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Host ("shot {0}x{1} at ({2},{3}) -> {4}" -f $w, $ht, $rect.Left, $rect.Top, $path)
    return $rect
}

# 点击：参数是“窗口内坐标”（与截图坐标一致），内部换算成屏幕坐标
function Click-InWindow([int]$x, [int]$y) {
    $h = Focus-Game
    $rect = New-Object McWin+RECT
    [McWin]::GetWindowRect($h, [ref]$rect) | Out-Null
    $sx = $rect.Left + $x; $sy = $rect.Top + $y
    [McWin]::SetCursorPos($sx, $sy) | Out-Null
    Start-Sleep -Milliseconds 120
    [McWin]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)  # LEFTDOWN
    Start-Sleep -Milliseconds 60
    [McWin]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)  # LEFTUP
    Write-Host ("clicked window ({0},{1}) = screen ({2},{3})" -f $x, $y, $sx, $sy)
}

# 控制台输入一行文本（PostMessage WM_CHAR 绕输入法）+ 回车（keybd_event 带扫描码）
function Send-ConsoleLine([string]$text) {
    $h = Focus-Game
    foreach ($ch in $text.ToCharArray()) {
        [McWin]::PostMessage($h, 0x0102, [IntPtr][int]$ch, [IntPtr]::Zero) | Out-Null  # WM_CHAR
        Start-Sleep -Milliseconds 25
    }
    Start-Sleep -Milliseconds 200
    [McWin]::keybd_event(0x0D, 0x1C, 0, [UIntPtr]::Zero)   # VK_RETURN down (scan 0x1C)
    Start-Sleep -Milliseconds 60
    [McWin]::keybd_event(0x0D, 0x1C, 2, [UIntPtr]::Zero)   # VK_RETURN up (KEYEVENTF_KEYUP)
    Write-Host ("sent line: {0}" -f $text)
}

# 打开/关闭控制台：Shift+8（README）
function Send-ConsoleToggle {
    $h = Focus-Game
    [McWin]::keybd_event(0x10, 0x2A, 0, [UIntPtr]::Zero)   # SHIFT down
    Start-Sleep -Milliseconds 60
    [McWin]::PostMessage($h, 0x0102, [IntPtr][int][char]'8', [IntPtr]::Zero) | Out-Null
    Start-Sleep -Milliseconds 60
    [McWin]::keybd_event(0x10, 0x2A, 2, [UIntPtr]::Zero)   # SHIFT up
    Write-Host "toggled console (Shift+8)"
}
