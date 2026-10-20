$ErrorActionPreference = 'Stop'
$c = 'D:\塔2MOD\MCMonsters\pck_content\animations\monsters\raid_captain\raid_captain.json'
$text = [System.IO.File]::ReadAllText($c)
if (-not $text.Contains('mcpillager001')) { Write-Host 'hash not found (already fixed?)'; exit 0 }
$text = $text.Replace('"mcpillager001"', '"mcraidcaptain01"')
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($c, $text, $utf8NoBom)
$j = [System.IO.File]::ReadAllText($c) | ConvertFrom-Json
Write-Host ('captain hash now: ' + $j.skeleton.hash)
