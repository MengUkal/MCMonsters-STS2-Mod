# MCMonsters 构建+部署脚本（收尾版）
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$proj = Join-Path $root "src\MCMonsters\MCMonsters.csproj"
$deploySrc = Join-Path $root "deploy\MCMonsters"
$gameRoot = "D:\steamwork\steamapps\common\Slay the Spire 2"
$gameMods = Join-Path $gameRoot "mods\MCMonsters"

Write-Host "=== 1. 关闭游戏（避免文件占用）==="
Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

Write-Host "=== 2. dotnet build ==="
& "C:\Program Files\dotnet\dotnet.exe" build $proj -c Release --nologo -v minimal
if ($LASTEXITCODE -ne 0) { throw "构建失败" }

Write-Host "=== 3. 重新打包 pck（美术资源+音效+图标）==="
& "C:\Program Files\dotnet\dotnet.exe" run --project "$root\tools\ArtGen\ArtGen.csproj" -c Release --no-build -- packonly
if ($LASTEXITCODE -ne 0) { throw "打包失败" }

Write-Host "=== 4. 部署到游戏 mods 目录 ==="
if (-not (Test-Path $gameMods)) { New-Item -ItemType Directory -Path $gameMods -Force | Out-Null }
Copy-Item (Join-Path $deploySrc "MCMonsters.json") $gameMods -Force
Copy-Item (Join-Path $deploySrc "MCMonsters.pck") $gameMods -Force
Copy-Item (Join-Path $root "src\MCMonsters\bin\Release\net9.0\MCMonsters.dll") $gameMods -Force
Copy-Item (Join-Path $root "src\MCMonsters\bin\Release\net9.0\MCMonsters.pdb") $gameMods -Force -ErrorAction SilentlyContinue

Write-Host "=== 5. 同步裸名贴图到mod目录（DLL启动时自安装到游戏根，兼容创意工坊）==="
# 所有怪物：{monster}.png 必须存在于游戏根目录，否则 Spine 纹理加载失败（坑18）
$monsters = Get-ChildItem (Join-Path $root "pck_content\animations\monsters") -Directory | ForEach-Object { $_.Name }
$copied = 0
foreach ($m in $monsters) {
    $src = Join-Path $root ("pck_content\animations\monsters\" + $m + "\" + $m + ".png")
    if (Test-Path $src) {
        Copy-Item $src (Join-Path $gameMods ($m + ".png")) -Force
        $copied++
    }
}
Write-Host ("已同步 " + $copied + " 个怪物裸图到mod目录（游戏根由自安装处理）")

Write-Host "=== 6. 部署完成 ==="
Get-ChildItem $gameMods | Select-Object Name, Length | Format-Table -AutoSize
Write-Host ("启动游戏验证：" + $gameRoot + "\SlayTheSpire2.exe")
