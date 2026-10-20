$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'

$zhs = Get-Content "$root\pck_content\MCMonsters\localization\zhs\monsters.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$eng = Get-Content "$root\pck_content\MCMonsters\localization\eng\monsters.json" -Raw -Encoding UTF8 | ConvertFrom-Json
$zhsKeys = $zhs.PSObject.Properties.Name
$engKeys = $eng.PSObject.Properties.Name

function SnakeUpper([string]$name) {
  # ChickenJockey -> CHICKEN_JOCKEY (用 [regex]::Replace 避免 PS 的 -creplace 问题)
  return ([regex]::Replace($name, '([a-z0-9])([A-Z])', '$1_$2')).ToUpper()
}

Write-Host '========== A. 招式标题键 =========='
$missingTotal = 0
Get-ChildItem "$root\src\MCMonsters\Monsters" -Filter '*.cs' | ForEach-Object {
  $id = SnakeUpper $_.BaseName
  $c = Get-Content $_.FullName -Raw -Encoding UTF8
  $moves = [regex]::Matches($c, 'new MoveState\("([A-Z_0-9]+)"') | ForEach-Object { $_.Groups[1].Value }
  foreach ($mv in $moves) {
    $key = $id + '.moves.' + $mv + '.title'
    if ($zhsKeys -notcontains $key) { Write-Host ("[缺中文] " + $key); $missingTotal++ }
    if ($engKeys -notcontains $key) { Write-Host ("[缺英文] " + $key); $missingTotal++ }
  }
}
Write-Host ("招式键缺失总数: " + $missingTotal)

Write-Host ''
Write-Host '========== B. 力量文本键 =========='
$pm = 0
Get-ChildItem "$root\src\MCMonsters\Powers" -Filter '*.cs' | ForEach-Object {
  $c = Get-Content $_.FullName -Raw -Encoding UTF8
  $keys = [regex]::Matches($c, 'new LocString\("monsters", "([A-Z_0-9]+)"\)') | ForEach-Object { $_.Groups[1].Value }
  $keys = $keys | Select-Object -Unique
  foreach ($k in $keys) {
    foreach ($suffix in @('.title', '.description')) {
      if ($zhsKeys -notcontains ($k + $suffix)) { Write-Host ("[缺中文] " + $_.BaseName + " -> " + $k + $suffix); $pm++ }
      if ($engKeys -notcontains ($k + $suffix)) { Write-Host ("[缺英文] " + $_.BaseName + " -> " + $k + $suffix); $pm++ }
    }
  }
}
Write-Host ("力量键缺失总数: " + $pm)

Write-Host ''
Write-Host '========== C. 遭遇引用 =========='
$encFiles = Get-ChildItem "$root\src\MCMonsters\Encounters" -Filter '*.cs' | ForEach-Object { $_.BaseName }
$patch = Get-Content "$root\src\MCMonsters\Patches\ActEncounterPatches.cs" -Raw -Encoding UTF8
$refs = [regex]::Matches($patch, 'ModelDb\.Encounter<Encounters\.(\w+)>') | ForEach-Object { $_.Groups[1].Value }
$missing = $refs | Where-Object { $encFiles -notcontains $_ }
if ($missing) { Write-Host ("[补丁引用不存在] " + ($missing -join ', ')) } else { Write-Host '补丁引用全部存在' }
$unused = $encFiles | Where-Object { $refs -notcontains $_ }
Write-Host ("未进池的遭遇类: " + $(if ($unused) { $unused -join ', ' } else { '无' }))
Write-Host ("遭遇引用总数: " + $refs.Count + " 处")

Write-Host ''
Write-Host '========== D. 怪物名键 =========='
$mm = 0
Get-ChildItem "$root\src\MCMonsters\Monsters" -Filter '*.cs' | ForEach-Object {
  $key = (SnakeUpper $_.BaseName) + '.name'
  if ($zhsKeys -notcontains $key) { Write-Host ("[缺中文] " + $key); $mm++ }
  if ($engKeys -notcontains $key) { Write-Host ("[缺英文] " + $key); $mm++ }
}
Write-Host ("怪物名键缺失: " + $mm)
