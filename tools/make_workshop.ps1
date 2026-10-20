$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = 'D:\塔2MOD\MCMonsters'
$staging = Join-Path $root 'workshop_content'
if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
New-Item -ItemType Directory -Path $staging -Force | Out-Null

# ===== 1. 复制模组文件 =====
Copy-Item "$root\deploy\MCMonsters\MCMonsters.json" $staging -Force
Copy-Item "$root\deploy\MCMonsters\MCMonsters.pck" $staging -Force
Copy-Item "$root\src\MCMonsters\bin\Release\net9.0\MCMonsters.dll" $staging -Force
Copy-Item "$root\src\MCMonsters\bin\Release\net9.0\MCMonsters.pdb" $staging -Force

# ===== 2. 复制19张裸图（自安装用）=====
$monsterDir = Join-Path $root 'pck_content\animations\monsters'
Get-ChildItem $monsterDir -Directory | ForEach-Object {
    $png = Join-Path $_.FullName ($_.Name + '.png')
    if (Test-Path $png) { Copy-Item $png $staging -Force }
}

# ===== 3. 生成工坊预览图（512x512 怪物拼贴）=====
$preview = Join-Path $root 'workshop_preview.png'
$canvas = New-Object System.Drawing.Bitmap(512, 512, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($canvas)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
# 深色渐变背景
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Point(0,0)), (New-Object System.Drawing.Point(0,512)),
    [System.Drawing.Color]::FromArgb(255, 20, 24, 34), [System.Drawing.Color]::FromArgb(255, 45, 35, 60))
$g.FillRectangle($brush, 0, 0, 512, 512)

# 4只代表怪：僵尸猪人/烈焰人/凋灵/末影龙（2x2 网格）
$picks = @('zombie_piglin', 'blaze', 'wither_skeleton', 'ender_dragon')
$cells = @(
    @(8, 8), @(260, 8), @(8, 260), @(260, 260)
)
for ($i = 0; $i -lt $picks.Count; $i++) {
    $src = Join-Path $monsterDir ($picks[$i] + '\' + $picks[$i] + '.png')
    if (-not (Test-Path $src)) { continue }
    $img = [System.Drawing.Bitmap]::FromFile($src)
    # 裁剪艺术区左300宽，底部对齐
    $rect = New-Object System.Drawing.Rectangle(0, 0, [Math]::Min(300, $img.Width), [Math]::Min(500, $img.Height))
    $crop = $img.Clone($rect, $img.PixelFormat)
    $g.DrawImage($crop, $cells[$i][0], $cells[$i][1], 244, 244)
    $crop.Dispose(); $img.Dispose()
}
$g.Dispose()
$canvas.Save($preview, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
Write-Host ("预览图: " + $preview)

# ===== 4. 生成上传VDF =====
$vdf = @'
"workshopitem"
{
    "appid"             "2868840"
    "publishedfileid"   "0"
    "contentfolder"     "D:\塔2MOD\MCMonsters\workshop_content"
    "previewfile"       "D:\塔2MOD\MCMonsters\workshop_preview.png"
    "visibility"        "2"
    "title"             "MC怪物 / Minecraft Monsters"
    "description"       "把《我的世界》的经典怪物加入《杀戮尖塔2》！19只怪物、21种遭遇，覆盖全部三幕。\n\n【第一幕】僵尸 / 骷髅 / 蜘蛛 / 苦力怕 + 精英鸡骑士 + BOSS监守者（空过回合惩罚机制）\n【第二幕】凋零骷髅 / 烈焰人 / 僵尸猪人 / 猪灵蛮兵 + 精英恶魂（完美格挡解谜）+ BOSS凋灵（半血99人工制品）\n【第三幕】末影人 / 潜影贝 / 掠夺者 / 女巫 + 精英巨人 / 掠夺者突袭 + BOSS末影龙（三形态600血）\n\n【特色】\n· 每只怪都有MC原版音效与专属攻击特效\n· 大量互动机制：空过惩罚、完美格挡解谜、装填打断、治疗打断、壳甲减伤、多段攻击克星\n· 遭遇按强度分入弱怪池/强怪池，与官方怪物同台竞技\n· 全中文+英文本地化，18个自定义BUFF均有图标\n\n【安装】\n订阅后在游戏内启用即可，需要前置模组 BaseLib（创意工坊搜索安装）。\n\n作者：MengUkal"
    "changenote"        "首次发布"
}
'@
Set-Content (Join-Path $root 'workshop_build.vdf') $vdf -Encoding UTF8
Write-Host 'VDF已生成'
Write-Host '=== 工坊内容清单 ==='
Get-ChildItem $staging | ForEach-Object { $_.Name + " " + [Math]::Round($_.Length/1KB) + "KB" }
Write-Host ("总计: " + (Get-ChildItem $staging | Measure-Object Length -Sum).Sum / 1MB + " MB")