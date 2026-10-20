$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'
$gameMods = 'D:\steamwork\steamapps\common\Slay the Spire 2\mods'

# ===== 1. 恢复BaseLib（其他模组可能要用，与本模组无关）=====
foreach ($f in @('BaseLib.json','BaseLib.dll','BaseLib.pck')) {
    $bk = Join-Path "$env:TEMP\baselib_backup" $f
    if (Test-Path $bk) { Move-Item $bk (Join-Path $gameMods $f) -Force }
}
Write-Host 'BaseLib已恢复（本模组不依赖它）'

# ===== 2. 重建 workshop_content（无依赖版）=====
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
Write-Host ('workshop_content: ' + (Get-ChildItem $staging).Count + ' 文件（无BaseLib）')

# ===== 3. 重建平台压缩包（无前置）=====
$ver = '1.5.0'
$pkgName = "MCMonsters_v$ver"
if (Test-Path (Join-Path $root 'platform_package')) { Remove-Item (Join-Path $root 'platform_package') -Recurse -Force }
$stage = Join-Path $root "platform_package\$pkgName"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$modDir = Join-Path $stage 'mods\MCMonsters'
New-Item -ItemType Directory -Path $modDir -Force | Out-Null
Copy-Item "$staging\*" $modDir -Force
$readme = @'
========================================
 MC怪物 / Minecraft Monsters v1.5.0
 安装说明（解压即用版 · 无前置依赖）
========================================

【安装步骤】
1. 完全退出游戏
2. 把本压缩包里的 mods 文件夹，解压到游戏根目录（SlayTheSpire2.exe 所在目录）
   解压后结构应为：
   <游戏目录>\
     mods\
       MCMonsters\   (mod本体，含19张贴图)

3. 启动游戏，在「模组配置」里确认 MCMonsters 已启用
4. 新开一局，正常游玩即可遇到我们的怪物

【重要说明】
- 本模组为独立模组，不需要任何前置依赖
- 怪物贴图会由模组在启动时自动安装到游戏根目录，无需手动操作
  （日志中会看到 [MCMonsters] Installed 19 bare textures to game root）
- 游戏版本要求：>= 0.107.1

【内容一览】
19只怪物 / 21种遭遇，覆盖三幕：
 一层：僵尸 骷髅 蜘蛛 苦力怕 | 精英：鸡骑士 | BOSS：监守者
 二层：凋零骷髅 烈焰人 僵尸猪人 猪灵蛮兵 | 精英：恶魂 | BOSS：凋灵
 三层：末影人 潜影贝 掠夺者 女巫 | 精英：巨人、掠夺者突袭 | BOSS：末影龙

【常见问题】
Q: 怪物不显示/游戏崩溃？
A: 检查是否把 mods\MCMonsters 完整解压到游戏目录，并确认游戏版本达标。

Q: 能和其他模组一起用吗？
A: 可以。本模组不依赖任何前置，与主流模组兼容。

【致谢】
- 美术：基于 MC 官方材质重绘的 Spine 动画
- 音效：Minecraft 原版音效

作者：MengUkal
'@
Set-Content (Join-Path $stage '安装说明.txt') $readme -Encoding UTF8
$zip = Join-Path $root "platform_package\$pkgName.zip"
Compress-Archive -Path $stage -DestinationPath $zip -CompressionLevel Optimal
$size = [Math]::Round((Get-Item $zip).Length / 1MB, 2)
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host ("新压缩包: " + $zip)
Write-Host ("大小: " + $size + " MB")
Write-Host ("SHA256: " + $hash)
Get-ChildItem $stage -Recurse -File | ForEach-Object { $_.FullName.Replace($stage, '') } | Select-Object -First 30