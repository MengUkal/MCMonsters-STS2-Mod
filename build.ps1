# MCMonsters 构建+部署脚本（仓库自包含版）
# 用法：  pwsh -File build.ps1
# 可选环境变量：
#   DOTNET_EXE  指定 dotnet（需要 .NET 9 SDK）
#   STS2_DIR    指定游戏目录（默认 E:\SteamLibrary\steamapps\common\Slay the Spire 2）
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root "src\MCMonsters\MCMonsters.csproj"
$artgen = Join-Path $root "tools\ArtGen\ArtGen.csproj"
$manifest = Join-Path $root "MCMonsters.json"
$deploy = Join-Path $root "deploy\MCMonsters"

# ---- 定位游戏目录：STS2_DIR > Steam 库注册表/库配置 > 常见默认路径 ----
function Find-GameRoot {
    if ($env:STS2_DIR -and (Test-Path (Join-Path $env:STS2_DIR 'SlayTheSpire2.exe'))) { return $env:STS2_DIR }
    $libs = @()
    try {
        $steamPath = (Get-ItemProperty 'HKCU:\Software\Valve\Steam' -ErrorAction Stop).SteamPath
        if ($steamPath) {
            $steamPath = $steamPath -replace '/', '\'
            $libs += $steamPath
            $lf = Join-Path $steamPath 'steamapps\libraryfolders.vdf'
            if (Test-Path $lf) {
                foreach ($m in [regex]::Matches((Get-Content $lf -Raw), '"path"\s+"([^"]+)"')) {
                    $libs += ($m.Groups[1].Value -replace '\\\\', '\')
                }
            }
        }
    } catch { }
    $libs += @('E:\SteamLibrary', 'D:\SteamLibrary', 'F:\SteamLibrary', 'C:\SteamLibrary', 'D:\steamwork\steamapps')
    foreach ($lib in $libs) {
        $candidate = if ($lib -match 'steamapps$') { Join-Path $lib 'common\Slay the Spire 2' } else { Join-Path $lib 'steamapps\common\Slay the Spire 2' }
        if (Test-Path (Join-Path $candidate 'SlayTheSpire2.exe')) { return $candidate }
    }
    throw "找不到 Slay the Spire 2 安装目录。请设置环境变量 STS2_DIR 指向游戏根目录（SlayTheSpire2.exe 所在处）。"
}
$gameRoot = Find-GameRoot
Write-Host "游戏目录: $gameRoot"
$gameMods = Join-Path $gameRoot "mods\MCMonsters"
$dllOut = Join-Path $root "src\MCMonsters\bin\Release\net9.0\MCMonsters.dll"
$pdbOut = Join-Path $root "src\MCMonsters\bin\Release\net9.0\MCMonsters.pdb"

$dotnet = $env:DOTNET_EXE
if (-not $dotnet) { $dotnet = (Get-Command dotnet -ErrorAction SilentlyContinue).Source }
if (-not $dotnet) { throw "找不到 dotnet，请安装 .NET 9 SDK 或设置 DOTNET_EXE" }
Write-Host "dotnet: $dotnet"
& $dotnet --list-sdks | ForEach-Object { Write-Host "  sdk $_" }

Write-Host "=== 1. 关闭游戏（避免文件占用） ==="
Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "=== 2. 构建 MCMonsters.dll ==="
& $dotnet build $proj -c Release --nologo -v minimal "-p:GameDir=$gameRoot"
if ($LASTEXITCODE -ne 0) { throw "构建失败" }
if (-not (Test-Path $dllOut)) { throw "找不到构建产物 $dllOut" }

Write-Host "=== 3. 打包 MCMonsters.pck（只打包，不重新生成素材） ==="
& $dotnet run --project $artgen -c Release -- packonly
if ($LASTEXITCODE -ne 0) { throw "打包失败" }
$pckOut = Join-Path $root "deploy\MCMonsters\MCMonsters.pck"
if (-not (Test-Path $pckOut)) { throw "找不到打包产物 $pckOut" }

Write-Host "=== 4. 组装 deploy\MCMonsters ==="
New-Item -ItemType Directory -Path $deploy -Force | Out-Null
Copy-Item $manifest $deploy -Force
Copy-Item $dllOut $deploy -Force
if (Test-Path $pdbOut) { Copy-Item $pdbOut $deploy -Force }

Write-Host "=== 5. 部署到本机游戏 mods 目录 ==="
if (-not (Test-Path $gameMods)) { New-Item -ItemType Directory -Path $gameMods -Force | Out-Null }
Get-ChildItem $deploy -File | Copy-Item -Destination $gameMods -Force
# 旧版本残留：mod 目录里的 19 张裸图与游戏根目录 mc_textures（新版已彻底不需要）
Get-ChildItem $gameMods -Filter '*.png' -ErrorAction SilentlyContinue | Remove-Item -Force
$legacyTex = Join-Path $gameRoot "mc_textures"
if (Test-Path $legacyTex) { Remove-Item $legacyTex -Recurse -Force; Write-Host "已清理旧版 mc_textures 目录" }

Write-Host "=== 6. 校验部署 ==="
foreach ($f in @('MCMonsters.json', 'MCMonsters.pck', 'MCMonsters.dll')) {
    $a = (Get-FileHash (Join-Path $deploy $f) -Algorithm MD5).Hash
    $b = (Get-FileHash (Join-Path $gameMods $f) -Algorithm MD5).Hash
    if ($a -ne $b) { throw "部署校验失败: $f" }
    Write-Host ("  OK {0} {1}" -f $f, $a)
}
Write-Host ("启动游戏验证: " + (Join-Path $gameRoot "SlayTheSpire2.exe"))
