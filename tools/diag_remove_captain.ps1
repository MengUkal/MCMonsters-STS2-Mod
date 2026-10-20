$ErrorActionPreference = 'Stop'
$p = 'D:\塔2MOD\MCMonsters\tools\ArtGen\ModPckAssembler.cs'
$c = [System.IO.File]::ReadAllText($p)
if ($c.Contains('"warden", "raid_captain"')) {
  $c = $c.Replace('"warden", "raid_captain" }', '"warden" }')
  [System.IO.File]::WriteAllText($p, $c)
  Write-Host 'raid_captain removed from assembler (diagnostic test)'
} else {
  Write-Host 'pattern not found'
}
