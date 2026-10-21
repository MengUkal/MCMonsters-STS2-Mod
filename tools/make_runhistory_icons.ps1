$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = 'D:\塔2MOD\MCMonsters'
$monsterDir = Join-Path $root 'pck_content\animations\monsters'
$outDir = Join-Path $root 'pck_content\images\ui\run_history'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# 遭遇名 -> 主怪物slug 映射（从遭遇源码提取第一个 ModelDb.Monster<X>）
function SnakeLower([string]$name) { return ([regex]::Replace($name, '([a-z0-9])([A-Z])', '$1_$2')).ToLower() }

$encounters = @{}
Get-ChildItem (Join-Path $root 'src\MCMonsters\Encounters') -Filter '*.cs' | ForEach-Object {
    $id = ([regex]::Replace($_.BaseName, '([a-z0-9])([A-Z])', '$1_$2')).ToLower()
    $c = Get-Content $_.FullName -Raw -Encoding UTF8
    # 取 GenerateMonsters 里第一个怪物
    $genBlock = [regex]::Match($c, 'GenerateMonsters\(\)[\s\S]*?\n    \}').Value
    $m = [regex]::Match($genBlock, 'ModelDb\.Monster<(\w+)>')
    if ($m.Success) { $encounters[$id] = (SnakeLower $m.Groups[1].Value) }
}
Write-Host ("遭遇数: " + $encounters.Count)

# 生成 128x128 图标 + 白色轮廓
$made = 0
$failed = @()
foreach ($encId in $encounters.Keys) {
    $slug = $encounters[$encId]
    $src = Join-Path $monsterDir ($slug + '\' + $slug + '.png')
    if (-not (Test-Path $src)) { $failed += ($encId + '->' + $slug); continue }
    $bmp = [System.Drawing.Bitmap]::FromFile($src)
    # 包围盒（取艺术区左上300x500）
    $minX = 300; $minY = 500; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt [Math]::Min(500, $bmp.Height); $y++) {
        for ($x = 0; $x -lt [Math]::Min(300, $bmp.Width); $x++) {
            if ($bmp.GetPixel($x, $y).A -gt 40) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    if ($maxX -lt 0) { $bmp.Dispose(); $failed += ($encId + '->空图'); continue }
    $bw = $maxX - $minX + 1; $bh = $maxY - $minY + 1
    # 头部区域
    $cw = [int]($bw * 0.5); $ch = [int]($bh * 0.5)
    $cx = $minX + [int](($bw - $cw) / 2); $cy = $minY
    if ($cx + $cw -gt 300) { $cw = 300 - $cx }
    if ($cw -lt 1) { $cw = 1 }; if ($ch -lt 1) { $ch = 1 }
    $rect = New-Object System.Drawing.Rectangle($cx, $cy, $cw, $ch)
    $crop = $bmp.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)

    # 图标（128x128，透明背景）
    $icon = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($icon)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $sc = [Math]::Min(120.0 / $cw, 120.0 / $ch)
    $dw = [int]($cw * $sc); $dh = [int]($ch * $sc)
    if ($dw -lt 1) { $dw = 1 }; if ($dh -lt 1) { $dh = 1 }
    $g.DrawImage($crop, [int]((128 - $dw) / 2), [int]((128 - $dh) / 2), $dw, $dh)
    $g.Dispose()
    $icon.Save((Join-Path $outDir ($encId + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)

    # 轮廓（白色剪影）
    $outline = New-Object System.Drawing.Bitmap(128, 128, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    for ($y = 0; $y -lt 128; $y++) {
        for ($x = 0; $x -lt 128; $x++) {
            $px = $icon.GetPixel($x, $y)
            if ($px.A -gt 40) { $outline.SetPixel($x, $y, [System.Drawing.Color]::FromArgb($px.A, 255, 255, 255)) }
        }
    }
    $outline.Save((Join-Path $outDir ($encId + '_outline.png')), [System.Drawing.Imaging.ImageFormat]::Png)

    $icon.Dispose(); $outline.Dispose(); $crop.Dispose(); $bmp.Dispose()
    $made++
}
Write-Host ("生成图标: " + $made + " 个遭遇 x2 (图标+轮廓)")
if ($failed.Count -gt 0) { Write-Host ("失败: " + ($failed -join ', ')) }
Write-Host ("文件总数: " + (Get-ChildItem $outDir -Filter '*.png').Count)
