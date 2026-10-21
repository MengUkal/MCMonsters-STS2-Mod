$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'
$ver = '1.6.1'

# ===== 1. 更新清单版本 =====
foreach ($p in @((Join-Path $root 'MCMonsters.json'), (Join-Path $root 'deploy\MCMonsters\MCMonsters.json'))) {
    if (Test-Path $p) {
        $j = Get-Content $p -Raw -Encoding UTF8 | ConvertFrom-Json
        $j.version = $ver
        $j | ConvertTo-Json -Depth 5 | Set-Content $p -Encoding UTF8
        Write-Host ('更新清单: ' + $p)
    }
}

# ===== 2. 重新打包 =====
Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
& "C:\Program Files\dotnet\dotnet.exe" run --project "$root\tools\ArtGen\ArtGen.csproj" -c Release --no-build -- packonly 2>&1 | Select-String -Pattern '打包完成' | ForEach-Object { $_.Line }

# ===== 3. 重建平台分发包 =====
$staging = Join-Path $root 'workshop_content'
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null
Copy-Item "$root\deploy\MCMonsters\MCMonsters.json" $staging -Force
Copy-Item "$root\deploy\MCMonsters\MCMonsters.pck" $staging -Force
Copy-Item "$root\src\MCMonsters\bin\Release\net9.0\MCMonsters.dll" $staging -Force
Copy-Item "$root\src\MCMonsters\bin\Release\net9.0\MCMonsters.pdb" $staging -Force
Get-ChildItem "$root\pck_content\animations\monsters" -Directory | ForEach-Object {
    $png = Join-Path $_.FullName ($_.Name + '.png')
    if (Test-Path $png) { Copy-Item $png $staging -Force }
}
Write-Host ('workshop_content: ' + (Get-ChildItem $staging).Count + ' 文件')

$pkgName = "MCMonsters_v$ver"
if (Test-Path (Join-Path $root 'platform_package')) { Remove-Item (Join-Path $root 'platform_package') -Recurse -Force }
$stage = Join-Path $root "platform_package\$pkgName"
$modDir = Join-Path $stage 'mods\MCMonsters'
New-Item -ItemType Directory -Path $modDir -Force | Out-Null
Copy-Item "$staging\*" $modDir -Force
$readme = @'
========================================
 MC怪物 / Minecraft Monsters v1.6.1
 安装说明（解压即用版 · 无前置依赖）
========================================

【更新内容 v1.6.1】
修复严重BUG：进入第二幕时游戏卡死
原因：缺少跑图记录图标，游戏加载失败后同步回退循环导致卡死
已补齐全部 34 个遭遇的跑图图标（图标+轮廓）

【安装步骤】
1. 完全退出游戏
2. 把 mods 文件夹解压到游戏根目录（SlayTheSpire2.exe 所在目录）
3. 启动游戏，在「模组配置」确认已启用
4. 新开一局

【重要说明】
- 独立模组，无前置依赖
- 贴图全部内置在模组包内，不需要往游戏目录放任何文件
- 游戏版本要求：>= 0.107.1

【内容一览】
19只怪物 / 25种遭遇，覆盖三幕

作者：MengUkal
'@
Set-Content (Join-Path $stage '安装说明.txt') $readme -Encoding UTF8
$zip = Join-Path $root "platform_package\$pkgName.zip"
Compress-Archive -Path $stage -DestinationPath $zip -CompressionLevel Optimal
Write-Host ("平台包: " + $zip)
Write-Host ("大小: " + [Math]::Round((Get-Item $zip).Length/1MB,2) + " MB")
Write-Host ("SHA256: " + (Get-FileHash $zip -Algorithm SHA256).Hash)
