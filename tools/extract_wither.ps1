$ErrorActionPreference = 'Stop'
$pck = 'D:\steamwork\steamapps\common\Slay the Spire 2\mods\MCMonsters\MCMonsters.pck'
$outDir = 'D:\塔2MOD\_wither_check'
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir -Force | Out-Null

$fs = [System.IO.File]::OpenRead($pck)
$br = [System.IO.BinaryReader]::new($fs)
$magic = $br.ReadUInt32(); $ver = $br.ReadUInt32(); $maj = $br.ReadUInt32(); $min = $br.ReadUInt32(); $pat = $br.ReadUInt32()
$flags = $br.ReadUInt32(); $fileBase = $br.ReadUInt64(); $dirOffset = $br.ReadUInt64()
$fs.Position = [long]$dirOffset
$count = $br.ReadUInt32()
$entries = @{}
for ($i = 0; $i -lt $count; $i++) {
    $plen = $br.ReadUInt32()
    $path = [System.Text.Encoding]::UTF8.GetString($br.ReadBytes([int]$plen)).TrimEnd([char]0)
    $off = $br.ReadUInt64(); $size = $br.ReadUInt64(); $md5 = $br.ReadBytes(16); $eflags = $br.ReadUInt32()
    $entries[$path] = [pscustomobject]@{ Offset = $off; Size = $size }
}
Write-Host ("pck条目: " + $entries.Count)

# 提取凋灵相关的全部文件
foreach ($path in $entries.Keys | Where-Object { $_ -match 'wither_boss' }) {
    $e = $entries[$path]
    $data = New-Object byte[] $e.Size
    $fs.Position = [long]($e.Offset + $fileBase)
    $read = $fs.Read($data, 0, $data.Length)
    $safe = ($path -replace '[/\\:]', '_') -replace '[^a-zA-Z0-9_.-]', '_'
    [System.IO.File]::WriteAllBytes((Join-Path $outDir $safe), $data)
    Write-Host ("  " + $path + "  " + $e.Size + "B 读取=" + $read)
}
$br.Close(); $fs.Close()


