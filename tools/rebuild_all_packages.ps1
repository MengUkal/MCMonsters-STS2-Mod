$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'
$ver = '1.5.0'
$gameMods = 'D:\steamwork\steamapps\common\Slay the Spire 2\mods\MCMonsters'

# ===== 1. 重建 workshop_content（适配 mc_textures 自动安装）=====
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

# ===== 2. 重建平台分发包 =====
$pkgName = "MCMonsters_v$ver"
if (Test-Path (Join-Path $root 'platform_package')) { Remove-Item (Join-Path $root 'platform_package') -Recurse -Force }
$stage = Join-Path $root "platform_package\$pkgName"
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
   解压后结构：
   <游戏目录>\
     mods\
       MCMonsters\   (mod本体，含19张PNG)

3. 启动游戏 —— 模组会自动把贴图安装到 <游戏目录>\mc_textures\
   （日志中会显示 [MCMonsters] 贴图安装: 19/19）
4. 在「模组配置」里确认 MCMonsters 已启用
5. 新开一局，正常游玩即可遇到怪物

【重要说明】
- 本模组为独立模组，不需要任何前置依赖
- 贴图自动安装，玩家无需手动操作
- 游戏版本要求：>= 0.107.1

【如果怪物不显示 / 游戏崩溃】
1. 检查 <游戏目录>\mc_textures\ 文件夹是否存在、里面是否有19个PNG
2. 如果没有 → 说明贴图安装失败（通常是游戏装在 C:\Program Files 导致无写权限）
   解决方法：右键游戏图标 → 以管理员身份运行 一次即可
3. 查看日志：[MCMonsters] 贴图安装: X/19 —— 看复制是否成功

【内容一览】
19只怪物 / 25种遭遇，覆盖三幕：
 一层：僵尸 骷髅 蜘蛛 苦力怕 | 精英：鸡骑士 | BOSS：监守者
 二层：凋零骷髅 烈焰人 僵尸猪人 猪灵蛮兵 | 精英：恶魂 | BOSS：凋灵
 三层：末影人 潜影贝 掠夺者 女巫 | 精英：巨人、掠夺者突袭 | BOSS：末影龙

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
Write-Host ("平台包: " + $zip)
Write-Host ("大小: " + $size + " MB")
Write-Host ("SHA256: " + $hash)

# ===== 3. 部署到本机（验证用）=====
Get-Process SlayTheSpire2 -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
Copy-Item "$root\deploy\MCMonsters\MCMonsters.pck" $gameMods -Force
Copy-Item "$root\src\MCMonsters\bin\Release\net9.0\MCMonsters.dll" $gameMods -Force
Copy-Item "$root\src\MCMonsters\bin\Release\net9.0\MCMonsters.pdb" $gameMods -Force
Write-Host '本机已部署最新版'
