$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'
$ver = '1.5.0'
$pkgName = "MCMonsters_v$ver"
$stage = Join-Path $root "platform_package\$pkgName"
if (Test-Path (Join-Path $root 'platform_package')) { Remove-Item (Join-Path $root 'platform_package') -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

# ===== 1. mods\MCMonsters\（模组本体+裸图）=====
$modDir = Join-Path $stage 'mods\MCMonsters'
New-Item -ItemType Directory -Path $modDir -Force | Out-Null
Copy-Item (Join-Path $root 'workshop_content\*') $modDir -Recurse -Force

# ===== 2. mods\BaseLib（前置依赖）=====
$gameMods = 'D:\steamwork\steamapps\common\Slay the Spire 2\mods'
foreach ($f in @('BaseLib.json','BaseLib.dll','BaseLib.pck')) {
    $src = Join-Path $gameMods $f
    if (Test-Path $src) { Copy-Item $src (Join-Path $stage 'mods') -Force }
}

# ===== 3. 安装说明 =====
$readme = @'
========================================
 MC怪物 / Minecraft Monsters v1.5.0
 安装说明（解压即用版）
========================================

【安装步骤】
1. 完全退出游戏
2. 把本压缩包里的 mods 文件夹，解压到游戏根目录（SlayTheSpire2.exe 所在目录）
   解压后结构应为：
   <游戏目录>\
     mods\
       MCMonsters\   (mod本体)
       BaseLib.json / BaseLib.dll / BaseLib.pck   (前置依赖)

3. 启动游戏，在「模组配置」里确认 MCMonsters 已启用
4. 新开一局，正常游玩即可遇到我们的怪物

【重要说明】
- 怪物贴图会由模组在启动时自动安装到游戏根目录，无需手动操作
  （日志中会看到 [MCMonsters] Installed 19 bare textures to game root）
- 前置依赖 BaseLib 已打包（作者 Alchyr，创意工坊开源模组，仅作安装便利，版权归原作者）
- 游戏版本要求：>= 0.107.1

【内容一览】
19只怪物 / 21种遭遇，覆盖三幕：
 一层：僵尸 骷髅 蜘蛛 苦力怕 | 精英：鸡骑士 | BOSS：监守者
 二层：凋零骷髅 烈焰人 僵尸猪人 猪灵蛮兵 | 精英：恶魂 | BOSS：凋灵
 三层：末影人 潜影贝 掠夺者 女巫 | 精英：巨人、掠夺者突袭 | BOSS：末影龙

【常见问题】
Q: 怪物不显示/游戏崩溃？
A: 检查是否完整解压了 mods\ 文件夹（两个模组都要在），并确认游戏版本达标。

Q: 想调整难度？
A: 数值均在源码 Monsters\ 目录下的各怪物类里，可自行编译修改。

【致谢】
- 美术：基于 MC 官方材质重绘的 Spine 动画
- 音效：Minecraft 原版音效
- 前置框架：BaseLib (Alchyr)

作者：Wanjiadongli
'@
Set-Content (Join-Path $stage '安装说明.txt') $readme -Encoding UTF8

# ===== 4. 压缩 =====
$zip = Join-Path $root "platform_package\$pkgName.zip"
Compress-Archive -Path $stage -DestinationPath $zip -CompressionLevel Optimal
$size = [Math]::Round((Get-Item $zip).Length / 1MB, 2)
Write-Host ("压缩包: " + $zip + "  (" + $size + " MB)")
Write-Host '=== 包内结构 ==='
$stageItems = Get-ChildItem $stage -Recurse | ForEach-Object { $_.FullName.Replace($stage, '') }
$stageItems | Where-Object { $_ -notmatch '\\$' } | Select-Object -First 40 | ForEach-Object { $_ }