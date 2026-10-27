# 一次性诊断：读取官方 SlayTheSpire2.pck，列出所有 creature_visuals 场景的
# Visuals scale / Bounds / IntentPos，用于对比官方意图图标定位惯例。
param(
    [string]$Pck = "E:\SteamLibrary\steamapps\common\Slay the Spire 2\SlayTheSpire2.pck",
    [string]$Filter = ""
)
$ErrorActionPreference = 'Stop'
$fs = [System.IO.File]::OpenRead($Pck)
$br = [System.IO.BinaryReader]::new($fs)
$null = $br.ReadUInt32(); $null = $br.ReadUInt32(); $null = $br.ReadUInt32(); $null = $br.ReadUInt32(); $null = $br.ReadUInt32()
$null = $br.ReadUInt32(); $fileBase = $br.ReadUInt64(); $dirOffset = $br.ReadUInt64()
$fs.Position = [long]$dirOffset
$count = $br.ReadUInt32()
$entries = @{}
for ($i = 0; $i -lt $count; $i++) {
    $plen = $br.ReadUInt32()
    $path = [System.Text.Encoding]::UTF8.GetString($br.ReadBytes([int]$plen)).TrimEnd([char]0)
    $off = $br.ReadUInt64(); $size = $br.ReadUInt64(); $null = $br.ReadBytes(16); $null = $br.ReadUInt32()
    $entries[$path] = [pscustomobject]@{ Offset = $off; Size = $size }
}
Write-Host ("总文件数: {0}" -f $count)

function Read-Entry([string]$path) {
    $e = $entries[$path]
    if ($null -eq $e) { return $null }
    $fs.Position = [long]($fileBase + $e.Offset)
    return $br.ReadBytes([int]$e.Size)
}

$scenePaths = $entries.Keys | Where-Object { $_ -like '*scenes/creature_visuals/*.tscn' } | Sort-Object
Write-Host ("creature_visuals 场景数: {0}" -f $scenePaths.Count)
foreach ($p in $scenePaths) {
    $name = ($p -split "/")[-1] -replace "\.tscn$",""
    if ($Filter -and $name -notmatch $Filter) { continue }
    $bytes = Read-Entry $p
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $scale = if ($text -match 'scale = Vector2\(([-\d.]+), ') { $Matches[1] } else { '?' }
    $top    = if ($text -match 'offset_top = ([-\d.]+)') { $Matches[1] } else { '?' }
    $bottom = if ($text -match 'offset_bottom = ([-\d.]+)') { $Matches[1] } else { '?' }
    $intent = if ($text -match 'IntentPos[\s\S]{0,200}?position = Vector2\(([-\d.]+), ([-\d.]+)\)') { $Matches[2] } else { '?' }
    $visPos = if ($text -match 'Visuals[\s\S]{0,400}?position = Vector2\(([-\d.]+), ([-\d.]+)\)') { $Matches[2] } else { '?' }
    # intent 相对 Bounds 顶部的偏移
    $rel = '?'
    if ($intent -ne '?' -and $top -ne '?') { $rel = [math]::Round(([double]$intent - [double]$top), 1) }
    Write-Host ("{0,-28} scale={1,-6} boundsTop={2,-8} intentY={3,-8} intent-boundsTop={4}" -f $name, $scale, $top, $intent, $rel)
}
$fs.Close()
