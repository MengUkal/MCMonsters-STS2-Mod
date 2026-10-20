$ErrorActionPreference = 'Stop'
$files = @(
  'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\raid_captain\raid_captain.atlas',
  'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\raid_captain\raid_captain_skel_data.tres',
  'D:\塔2MOD\MCMonsters\pck_content\MCMonsters\localization\eng\monsters.json',
  'D:\塔2MOD\MCMonsters\pck_content\MCMonsters\localization\zhs\monsters.json'
)
foreach ($f in $files) {
  $b = [System.IO.File]::ReadAllBytes($f)
  if ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF) {
    [System.IO.File]::WriteAllBytes($f, $b[3..($b.Length - 1)])
    Write-Host ("BOM removed: " + (Split-Path $f -Leaf))
  }
}
# 再次全扫描确认
$bad = 0
Get-ChildItem 'D:\塔2MOD\MCMonsters\pck_content' -Recurse -Include '*.tres','*.tscn','*.json','*.atlas','*.import','*.tpsheet' | ForEach-Object {
  $b = [System.IO.File]::ReadAllBytes($_.FullName)
  if ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF) { $bad++ }
}
Write-Host ("剩余BOM文件: " + $bad)
