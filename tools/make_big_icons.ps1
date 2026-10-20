$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$monsterDir = 'D:\塔2MOD\MCMonsters\pck_content\animations\monsters'
$outDir = 'D:\塔2MOD\MCMonsters\pck_content\images\powers'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

# 新力量 → 怪物（大图标PNG，怪物头颅裁剪）
$map = [ordered]@{
  'shulker_shell_power' = 'shulker'
  'enderman_dodge_power' = 'enderman'
  'pillager_load_power' = 'pillager'
  'witch_brew_power' = 'witch'
  'dragon_flight_power' = 'ender_dragon'
  'warden_punish_power' = 'warden'
  'wither_warding_power' = 'wither_boss'
}
$special = @{
  'ender_dragon' = 'left'
  'wither_boss'  = 'center'
}

foreach ($k in $map.Keys) {
  $m = $map[$k]
  $src = Join-Path $monsterDir ($m + '\' + $m + '.png')
  if (-not (Test-Path $src)) { Write-Host ("SKIP " + $src); continue }
  $bmp = [System.Drawing.Bitmap]::FromFile($src)
  $minX = 300; $minY = 500; $maxX = -1; $maxY = -1
  for ($y = 0; $y -lt 500; $y++) {
    for ($x = 0; $x -lt 300; $x++) {
      if ($bmp.GetPixel($x, $y).A -gt 40) {
        if ($x -lt $minX) { $minX = $x }
        if ($x -gt $maxX) { $maxX = $x }
        if ($y -lt $minY) { $minY = $y }
        if ($y -gt $maxY) { $maxY = $y }
      }
    }
  }
  if ($maxX -lt 0) { $bmp.Dispose(); continue }
  $bw = $maxX - $minX + 1; $bh = $maxY - $minY + 1
  $cw = [int]($bw * 0.45); $ch = [int]($bh * 0.40)
  $cx = $minX + [int](($bw - $cw) / 2)
  $cy = $minY
  if ($special.ContainsKey($m)) {
    if ($special[$m] -eq 'left') { $cx = $minX }
    if ($special[$m] -eq 'center') { $cy = $minY + [int]($bh * 0.08) }
  }
  if ($cx + $cw -gt 300) { $cw = 300 - $cx }
  if ($cx -lt 0) { $cx = 0 }
  if ($cw -lt 1) { $cw = 1 }
  if ($ch -lt 1) { $ch = 1 }
  $rect = New-Object System.Drawing.Rectangle($cx, $cy, $cw, $ch)
  $crop = $bmp.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $icon = New-Object System.Drawing.Bitmap(64, 64, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
  $g = [System.Drawing.Graphics]::FromImage($icon)
  $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
  $sc = [Math]::Min(64.0 / $cw, 64.0 / $ch)
  $dw = [int]($cw * $sc); $dh = [int]($ch * $sc)
  if ($dw -lt 1) { $dw = 1 }
  if ($dh -lt 1) { $dh = 1 }
  $g.DrawImage($crop, [int]((64 - $dw) / 2), [int]((64 - $dh) / 2), $dw, $dh)
  $g.Dispose()
  $dst = Join-Path $outDir ($k + '.png')
  $icon.Save($dst, [System.Drawing.Imaging.ImageFormat]::Png)
  $icon.Dispose(); $crop.Dispose(); $bmp.Dispose()
  Write-Host ("OK " + $k)
}
Write-Host 'new big icons done'
