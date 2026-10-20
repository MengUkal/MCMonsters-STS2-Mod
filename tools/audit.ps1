$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'
$NL = [char]10

# ===== A. 招式本地化键检查 =====
Write-Host '========== A. 招式标题键检查 =========='
$zhs = Get-Content "$root\pck_content\MCMonsters\localization\zhs\monsters.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$eng = Get-Content "$root\pck_content\MCMonsters\localization\eng\monsters.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$zhsKeys = $zhs.PSObject.Properties.Name
$engKeys = $eng.PSObject.Properties.Name

Get-ChildItem "$root\src\MCMonsters\Monsters" -Filter '*.cs' | ForEach-Object {
  $monster = $_.BaseName
  $c = Get-Content $_.FullName -Raw -Encoding UTF8
  # 提取 MoveState("NAME", ...)
  $moves = [regex]::Matches($c, 'new MoveState\("([A-Z_]+)"') | ForEach-Object { $_.Groups[1].Value }
  foreach ($mv in $moves) {
    $key = $monster.ToUpper() + '.moves.' + $mv + '.title'
    if ($zhsKeys -notcontains $key) { Write-Host ("[缺中文] " + $key) }
    if ($engKeys -notcontains $key) { Write-Host ("[缺英文] " + $key) }
  }
  if ($moves.Count -gt 0) { Write-Host ($monster + ": " + $moves.Count + " 招") }
}

# ===== B. 力量文本键检查 =====
Write-Host ''
Write-Host '========== B. 力量标题/描述键检查 =========='
Get-ChildItem "$root\src\MCMonsters\Powers" -Filter '*.cs' | ForEach-Object {
  $c = Get-Content $_.FullName -Raw -Encoding UTF8
  $keys = [regex]::Matches($c, 'new LocString\("monsters", "([A-Z_]+)"\)') | ForEach-Object { $_.Groups[1].Value }
  foreach ($k in $keys) {
    foreach ($suffix in @('.title', '.description')) {
      $full = $k + $suffix
      if ($zhsKeys -notcontains $full) { Write-Host ("[缺中文] " + $_.BaseName + " -> " + $full) }
      if ($engKeys -notcontains $full) { Write-Host ("[缺英文] " + $_.BaseName + " -> " + $full) }
    }
  }
}
Write-Host '力量键检查完成'

# ===== C. 遭遇引用一致性 =====
Write-Host ''
Write-Host '========== C. 遭遇类与补丁引用检查 =========='
$encFiles = Get-ChildItem "$root\src\MCMonsters\Encounters" -Filter '*.cs' | ForEach-Object { $_.BaseName }
$patch = Get-Content "$root\src\MCMonsters\Patches\ActEncounterPatches.cs" -Raw -Encoding UTF8
$refs = [regex]::Matches($patch, 'ModelDb\.Encounter<Encounters\.(\w+)>') | ForEach-Object { $_.Groups[1].Value }
Write-Host ("遭遇类文件: " + $encFiles.Count + " 个")
Write-Host ("补丁引用: " + $refs.Count + " 处")
$missing = $refs | Where-Object { $encFiles -notcontains $_ }
if ($missing) { Write-Host ("[补丁引用了不存在的类] " + ($missing -join ', ')) } else { Write-Host '补丁引用全部存在 ✓' }
$unused = $encFiles | Where-Object { $refs -notcontains $_ }
if ($unused) { Write-Host ("[未进任何池的遭遇] " + ($unused -join ', ')) } else { Write-Host '所有遭遇都已进池 ✓' }

# ===== D. 世界文本键（怪物名） =====
Write-Host ''
Write-Host '========== D. 怪物名键检查 =========='
Get-ChildItem "$root\src\MCMonsters\Monsters" -Filter '*.cs' | ForEach-Object {
  $id = $_.BaseName
  # WitherBoss -> WITHER_BOSS, RaidCaptain -> RAID_CAPTAIN
  $key = ($id -creplace '([a-z])([A-Z])', '$1_$2').ToUpper() + '.name'
  # 特殊：WitherBoss的遭遇模型名用WITHER_BOSS
  if ($zhsKeys -notcontains $key -and $engKeys -notcontains $key) { Write-Host ("[缺怪物名] " + $id + " -> " + $key) }
}
Write-Host '怪物名检查完成'
