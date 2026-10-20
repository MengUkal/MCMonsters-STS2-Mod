$ErrorActionPreference = 'Stop'
$src = 'D:\塔2MOD\tools\power_atlas.tpsheet'
$dstDir = 'D:\塔2MOD\MCMonsters\pck_content\images\atlases'
$j = Get-Content $src -Raw -Encoding UTF8 | ConvertFrom-Json
$sprites = @($j.textures[0].sprites)

$map = @{
  'skeleton_precision_power' = 'accuracy_power.png'
  'mc_wither_power' = 'poison_power.png'
  'wither_skeleton_curse_power' = 'poison_power.png'
  'zombie_piglin_grudge_power' = 'strength_power.png'
  'jockey_revive_power' = 'regen_power.png'
  'wither_boss_revive_power' = 'regen_power.png'
  'ender_dragon_revive_power' = 'regen_power.png'
  'blaze_aura_power' = 'flame_barrier_power.png'
  'ghast_intangible_power' = 'intangible_power.png'
  'ghast_perfect_block_power' = 'buffer_power.png'
  'warden_punish_power' = 'ritual_power.png'
  'wither_warding_power' = 'intangible_power.png'
  'enderman_dodge_power' = 'blur_power.png'
  'shulker_shell_power' = 'hardened_shell_power.png'
  'pillager_load_power' = 'accuracy_power.png'
  'witch_brew_power' = 'poison_power.png'
  'dragon_flight_power' = 'blur_power.png'
}
$added = 0
foreach ($k in $map.Keys) {
  $srcName = $map[$k]
  $srcSprite = $sprites | Where-Object { $_.filename -eq $srcName } | Select-Object -First 1
  if (-not $srcSprite) { Write-Host ("MISS " + $srcName); continue }
  # 移除同名旧条目
  $sprites = @($sprites | Where-Object { $_.filename -ne ($k + '.png') })
  $sprites += [pscustomobject]@{
    filename = ($k + '.png')
    region = [pscustomobject]@{ x = $srcSprite.region.x; y = $srcSprite.region.y; w = $srcSprite.region.w; h = $srcSprite.region.h }
    margin = [pscustomobject]@{ x = 0; y = 0; w = 0; h = 0 }
  }
  $added++
}
$j.textures[0].sprites = $sprites
$json = $j | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText((Join-Path $dstDir 'power_atlas.tpsheet'), $json)
Write-Host ('tpsheet rebuilt: ' + $sprites.Count + ' sprites (' + $added + ' custom)')
# 验证
$v = Get-Content (Join-Path $dstDir 'power_atlas.tpsheet') -Raw -Encoding UTF8 | ConvertFrom-Json
$ok = 0
foreach ($n in $map.Keys) { if ($v.textures[0].sprites | Where-Object { $_.filename -eq ($n + '.png') }) { $ok++ } }
Write-Host ('验证自定义图标数: ' + $ok + '/' + $map.Count)
