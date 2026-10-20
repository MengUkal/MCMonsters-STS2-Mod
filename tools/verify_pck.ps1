# 校验 MCMonsters.pck 的贴图 import 链是否完整（无游戏根目录裸图也能加载）
# 用法：powershell -ExecutionPolicy Bypass -File tools\verify_pck.ps1 [-Pck <路径>]
param(
    [string]$Pck = ""
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $Pck) { $Pck = Join-Path $root "deploy\MCMonsters\MCMonsters.pck" }
if (-not (Test-Path $Pck)) { throw "找不到 pck: $Pck" }

$monsters = @(Get-ChildItem (Join-Path $root "pck_content\animations\monsters") -Directory | ForEach-Object { $_.Name })
$errors = New-Object System.Collections.Generic.List[string]
$fs = [System.IO.File]::OpenRead($Pck)
$br = [System.IO.BinaryReader]::new($fs)
$magic = $br.ReadUInt32(); $ver = $br.ReadUInt32(); $maj = $br.ReadUInt32(); $min = $br.ReadUInt32(); $pat = $br.ReadUInt32()
$flags = $br.ReadUInt32(); $fileBase = $br.ReadUInt64(); $dirOffset = $br.ReadUInt64()
if ($magic -ne 0x43504447) { throw ("pck magic 错误: 0x{0:X8}" -f $magic) }
$fs.Position = [long]$dirOffset
$count = $br.ReadUInt32()
$entries = @{}
for ($i = 0; $i -lt $count; $i++) {
    $plen = $br.ReadUInt32()
    $path = [System.Text.Encoding]::UTF8.GetString($br.ReadBytes([int]$plen)).TrimEnd([char]0)
    $off = $br.ReadUInt64(); $size = $br.ReadUInt64(); $md5 = $br.ReadBytes(16); $eflags = $br.ReadUInt32()
    $entries[$path] = [pscustomobject]@{ Offset = $off; Size = $size }
}

function Read-Entry([string]$path) {
    $e = $entries[$path]
    if ($null -eq $e) { return $null }
    $fs.Position = [long]($fileBase + $e.Offset)
    $buf = New-Object byte[] ([int]$e.Size)
    $read = 0
    while ($read -lt $buf.Length) { $n = $fs.Read($buf, $read, $buf.Length - $read); if ($n -le 0) { break }; $read += $n }
    return ,$buf
}

function Get-RemapPath([string]$importText) {
    $m = [regex]::Match($importText, 'path="([^"]+)"')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

Write-Host ("PCK: {0}" -f $Pck)
Write-Host ("头部: magic=GDPC ver={0} godot={1}.{2}.{3} 文件数={4}" -f $ver, $maj, $min, $pat, $count)

# ---- 必需基础资源 ----
foreach ($p in @('res://MCMonsters/localization/zhs/monsters.json', 'res://MCMonsters/localization/eng/monsters.json', 'res://scenes/creature_visuals/zombie.tscn')) {
    if (-not $entries.ContainsKey($p)) { $errors.Add("缺少基础资源: $p") }
}

foreach ($m in $monsters) {
    $dir = Join-Path $root ("pck_content\animations\monsters\" + $m)
    $png = Join-Path $dir ($m + ".png")

    # 1) 源资源 import 存根存在
    foreach ($p in @(("res://animations/monsters/{0}/{0}.png.import" -f $m), ("res://animations/monsters/{0}/{0}.atlas.import" -f $m), ("res://animations/monsters/{0}/{0}.json.import" -f $m))) {
        if (-not $entries.ContainsKey($p)) { $errors.Add("缺少 import 存根: $p") }
    }

    # 2) .png.import → ctex，并校验官方 56 字节头
    $imp = Read-Entry ("res://animations/monsters/{0}/{0}.png.import" -f $m)
    if ($null -eq $imp) {
        $errors.Add("缺少 .png.import: $m")
    } else {
        $impText = [System.Text.Encoding]::UTF8.GetString($imp)
        if ($impText -notmatch 'type="CompressedTexture2D"') { $errors.Add(("{0}.png.import 类型不是 CompressedTexture2D" -f $m)) }
        $ctexPath = Get-RemapPath $impText
        if (-not $ctexPath) {
            $errors.Add(("{0}.png.import 缺少 remap path" -f $m))
        } else {
            $ctex = Read-Entry $ctexPath
            if ($null -eq $ctex) {
                $errors.Add(("{0}.png.import 指向的 ctex 不存在: {1}" -f $m, $ctexPath))
            } else {
                if ($ctex.Length -lt 60) { $errors.Add(("{0}.ctex 太短" -f $m)) }
                else {
                    $ascii = [System.Text.Encoding]::ASCII.GetString($ctex, 0, 4)
                    if ($ascii -ne 'GST2') { $errors.Add(("{0}.ctex magic 不是 GST2" -f $m)) }
                    if ([BitConverter]::ToUInt32($ctex, 4) -ne 1) { $errors.Add(("{0}.ctex 版本不是 1" -f $m)) }
                    $w = [BitConverter]::ToUInt32($ctex, 8); $h = [BitConverter]::ToUInt32($ctex, 12)
                    $payloadLen = [BitConverter]::ToUInt32($ctex, 52)
                    if ($payloadLen -ne ($ctex.Length - 56)) { $errors.Add(("{0}.ctex payload 长度不符: {1} vs {2}" -f $m, $payloadLen, ($ctex.Length - 56))) }
                    $riff = [System.Text.Encoding]::ASCII.GetString($ctex, 56, 12)
                    if (-not $riff.StartsWith('RIFF') -or $riff.Substring(8, 4) -ne 'WEBP') { $errors.Add(("{0}.ctex payload 不是 RIFF/WEBP: {1}" -f $m, $riff)) }
                    if (Test-Path $png) {
                        $imgBytes = [System.IO.File]::ReadAllBytes($png)
                        $pngW = [int]$imgBytes[16] * 16777216 + [int]$imgBytes[17] * 65536 + [int]$imgBytes[18] * 256 + [int]$imgBytes[19]
                        $pngH = [int]$imgBytes[20] * 16777216 + [int]$imgBytes[21] * 65536 + [int]$imgBytes[22] * 256 + [int]$imgBytes[23]
                        if ($w -ne $pngW -or $h -ne $pngH) { $errors.Add(("{0}.ctex 尺寸 {1}x{2} 与 png {3}x{4} 不符" -f $m, $w, $h, $pngW, $pngH)) }
                    }
                }
            }
        }
    }

    # 3) spatlas：必须带 source_path，页面名为相对文件名
    $aimp = Read-Entry ("res://animations/monsters/{0}/{0}.atlas.import" -f $m)
    if ($null -eq $aimp) {
        $errors.Add(("{0}.atlas.import 缺失" -f $m))
    } else {
        $spatlasPath = Get-RemapPath ([System.Text.Encoding]::UTF8.GetString($aimp))
        if (-not $spatlasPath) {
            $errors.Add(("{0}.atlas.import 缺少 remap path" -f $m))
        } else {
            $spatlasBytes = Read-Entry $spatlasPath
            if ($null -eq $spatlasBytes) {
                $errors.Add(("{0} spatlas 不存在: {1}" -f $m, $spatlasPath))
            } else {
                $obj = [System.Text.Encoding]::UTF8.GetString($spatlasBytes) | ConvertFrom-Json
                $expectSource = "res://animations/monsters/{0}/{0}.atlas" -f $m
                if (-not $obj.source_path) {
                    $errors.Add(("{0} spatlas 缺少 source_path（会退回游戏根目录裸图）" -f $m))
                } elseif ($obj.source_path -ne $expectSource) {
                    $errors.Add(("{0} spatlas source_path 错误: {1}" -f $m, $obj.source_path))
                }
                $firstLine = ($obj.atlas_data -split "`n")[0]
                if ($firstLine -ne ("{0}.png" -f $m)) { $errors.Add(("{0} atlas 页面名不是相对文件名: {1}" -f $m, $firstLine)) }
                if ($obj.atlas_data -match 'mc_textures') { $errors.Add(("{0} spatlas 仍残留 mc_textures 页名" -f $m)) }
            }
        }
    }
}

# ---- 不允许出现旧版根级 remap / 裸图缓存 ----
foreach ($m in $monsters) {
    if ($entries.ContainsKey("res://$m.png.import")) { $errors.Add("残留根级 remap: res://$m.png.import") }
    if ($entries.ContainsKey("res://.godot/imported/$m.png")) { $errors.Add("残留裸图缓存: res://.godot/imported/$m.png") }
}

$fs.Close()

Write-Host ("pck 大小: {0:N0} 字节" -f (Get-Item $Pck).Length)
if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "=== 校验失败 ===" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host ("  x " + $_) -ForegroundColor Red }
    exit 1
}
Write-Host ""
Write-Host "=== 校验通过 ===" -ForegroundColor Green
Write-Host ("  {0} 只怪物：.png.import -> .ctex（56 字节官方头 + RIFF/WEBP）、spatlas 带 source_path、页名相对。" -f $monsters.Count)
Write-Host "  游戏根目录不需要任何贴图文件。"
