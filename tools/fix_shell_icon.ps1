$ErrorActionPreference = 'Stop'
$src = 'D:\塔2MOD\tools\power_atlas.tpsheet'
$dstDir = 'D:\塔2MOD\MCMonsters\pck_content\images\atlases'
$j = Get-Content $src -Raw -Encoding UTF8 | ConvertFrom-Json
$sprites = $j.textures[0].sprites

# 用官方 hardened_shell 图标重做壳甲条目（64x64标准区域）
$srcSprite = $sprites | Where-Object { $_.filename -eq 'hardened_shell_power.png' } | Select-Object -First 1
if (-not $srcSprite) { throw 'hardened_shell_power not found' }
Write-Host ('hardened_shell region: ' + $srcSprite.region.x + ',' + $srcSprite.region.y + ' ' + $srcSprite.region.w + 'x' + $srcSprite.region.h)

# 移除旧的 shulker_shell_power 条目并重建
$filtered = @()
foreach ($s in $sprites) { if ($s.filename -ne 'shulker_shell_power.png') { $filtered += $s } }
$newEntry = [pscustomobject]@{
  filename = 'shulker_shell_power.png'
  region = [pscustomobject]@{ x = $srcSprite.region.x; y = $srcSprite.region.y; w = $srcSprite.region.w; h = $srcSprite.region.h }
  margin = [pscustomobject]@{ x = 0; y = 0; w = 0; h = 0 }
}
$filtered += $newEntry
$j.textures[0].sprites = $filtered
$json = $j | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText((Join-Path $dstDir 'power_atlas.tpsheet'), $json)
Write-Host ('tpsheet updated, sprites: ' + $j.textures[0].sprites.Count)

# 顺带检查其他新图标的区域是否是标准64x64
foreach ($n in @('enderman_dodge_power','pillager_load_power','witch_brew_power','dragon_flight_power','warden_punish_power','mc_wither_power','ghast_intangible_power')) {
  $e = $j.textures[0].sprites | Where-Object { $_.filename -eq ($n + '.png') } | Select-Object -First 1
  if ($e) { Write-Host ($n + ': ' + $e.region.x + ',' + $e.region.y + ' ' + $e.region.w + 'x' + $e.region.h) }
}
