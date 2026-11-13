# make_workshop_update.ps1 - stage + build VDF + upload MCMonsters to Steam Workshop
# ASCII comments only; every path steamcmd consumes must stay ASCII (pitfall 29).
# Usage: powershell -File tools\make_workshop_update.ps1 [-NoUpload]
param(
    [switch]$NoUpload
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$itemId = '3823425588'
$stage = 'C:\webshop_build'
$content = Join-Path $stage 'content'
$vdfPath = Join-Path $stage 'workshop_build.vdf'

# ---- 1. read version from manifest ----
$manifest = Get-Content (Join-Path $root 'MCMonsters.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$version = $manifest.version
Write-Host ("Version: " + $version)

# ---- 2. stage deploy files into ASCII path ----
$deploy = Join-Path $root 'deploy\MCMonsters'
if (-not (Test-Path $deploy)) { throw "deploy dir not found: $deploy (run build.ps1 first)" }
New-Item -ItemType Directory -Path $stage -Force | Out-Null
if (Test-Path $content) { Remove-Item $content -Recurse -Force }
New-Item -ItemType Directory -Path $content -Force | Out-Null
Copy-Item "$deploy\*" $content -Force
Copy-Item (Join-Path $root 'workshop_preview.png') $stage -Force
foreach ($f in @('MCMonsters.json','MCMonsters.dll','MCMonsters.pck')) {
    $src = (Get-FileHash (Join-Path $deploy $f) -Algorithm MD5).Hash
    $dst = (Get-FileHash (Join-Path $content $f) -Algorithm MD5).Hash
    if ($src -ne $dst) { throw "stage checksum mismatch: $f" }
    Write-Host ("  staged OK {0} {1}" -f $f, $src)
}

# ---- 3. build VDF (UTF-8 no BOM) ----
$descName = [string]([char]0x5DE5) + [char]0x574A + [char]0x63CF + [char]0x8FF0 + '.txt'
$desc = [System.IO.File]::ReadAllText((Join-Path $root $descName), [System.Text.Encoding]::UTF8).TrimEnd()
$escaped = $desc -replace "`r`n", '\n' -replace "`n", '\n' -replace '"', '\"'
$changeNote = "v$version - adapted to game v0.113.0 beta (bestiary crash fix, revive powers API, per-class patch isolation); still compatible with v0.107.1 / v0.111.0"
$vdf = @"
"workshopitem"
{
    "appid"             "2868840"
    "publishedfileid"   "$itemId"
    "contentfolder"     "$content"
    "previewfile"       "$stage\workshop_preview.png"
    "visibility"        "0"
    "title"             "MC怪物 / Minecraft Monsters"
    "description"       "$escaped"
    "changenote"        "$changeNote"
}
"@
[System.IO.File]::WriteAllText($vdfPath, $vdf, [System.Text.UTF8Encoding]::new($false))
Write-Host ("VDF written: " + $vdfPath)

if ($NoUpload) { Write-Host 'NoUpload set - stop here.'; return }

# ---- 4. upload via steamcmd (cached login) ----
$steamcmd = 'D:\steamcmd\steamcmd.exe'
if (-not (Test-Path $steamcmd)) { throw "steamcmd not found: $steamcmd" }
Write-Host 'Uploading via steamcmd...'
$log = Join-Path $env:TEMP 'mc_workshop_upload.log'
& $steamcmd +login alan_jua +workshop_build_item $vdfPath +quit *>&1 | Tee-Object -FilePath $log | Select-Object -Last 40
$text = Get-Content $log -Raw -Encoding UTF8
if ($text -match 'Success') {
    Write-Host ''
    Write-Host '=== UPLOAD SUCCESS ==='
    ($text -split "`n" | Where-Object { $_ -match 'Success|published|Manifest|ERROR' }) | ForEach-Object { Write-Host $_.Trim() }
} else {
    throw "steamcmd upload failed - see $log"
}
