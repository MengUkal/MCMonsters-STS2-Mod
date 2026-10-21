$ErrorActionPreference = 'Stop'
$root = 'D:\塔2MOD\MCMonsters'
$pkg = Join-Path $root 'workshop_upload'
if (Test-Path $pkg) { Remove-Item $pkg -Recurse -Force }
New-Item -ItemType Directory -Path $pkg -Force | Out-Null

# 1. 上传内容
$content = Join-Path $pkg 'content'
New-Item -ItemType Directory -Path $content -Force | Out-Null
Copy-Item "$root\workshop_content\*" $content -Force

# 2. 预览图
Copy-Item "$root\workshop_preview.png" $pkg -Force

# 3. VDF模板（相对占位，说明里教改路径）
$vdf = @'
"workshopitem"
{
    "appid"             "2868840"
    "publishedfileid"   "0"
    "contentfolder"     "C:\webshop_build\content"
    "previewfile"       "C:\webshop_build\workshop_preview.png"
    "visibility"        "2"
    "title"             "MC怪物 / Minecraft Monsters"
    "description"       "把《我的世界》的经典怪物加入《杀戮尖塔2》！19只怪物、25种遭遇，覆盖全部三幕。\n\n【第一幕】僵尸 / 骷髅 / 蜘蛛 / 苦力怕 + 精英鸡骑士 + BOSS监守者（空过回合惩罚机制）\n【第二幕】凋零骷髅 / 烈焰人 / 僵尸猪人 / 猪灵蛮兵 + 精英恶魂（完美格挡解谜）+ BOSS凋灵（半血99人工制品）\n【第三幕】末影人 / 潜影贝 / 掠夺者 / 女巫 + 精英巨人 / 掠夺者突袭 + BOSS末影龙（三形态600血）\n\n【特色】\n· 每只怪都有MC原版音效与专属攻击特效\n· 大量互动机制：空过惩罚、完美格挡解谜、装填打断、治疗打断、壳甲减伤、多段攻击克星\n· 遭遇按强度分入弱怪池/强怪池，与官方怪物同台竞技\n· 全中文+英文本地化，20个自定义BUFF均有图标\n· 贴图自动安装，无需任何前置模组\n\n作者：Wanjiadongli\n源码：https://github.com/MengUkal/MCMonsters-STS2-Mod"
    "changenote"        "首次发布"
}
'@
Set-Content (Join-Path $pkg 'upload.vdf') $vdf -Encoding UTF8

# 4. 上传说明
$readme = @'
========================================
 创意工坊上传包（在家用电脑上操作）
========================================

【一、准备】
1. 把整个 workshop_upload 文件夹复制到家里电脑（例如放到 C:\webshop_build\）
2. 确保家里电脑装了 Steam + 杀戮尖塔2（用来登录上传）

【二、下载 SteamCMD】
地址：https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
解压到例如 D:\steamcmd\
首次运行（初始化，约29MB更新）：
  D:\steamcmd\steamcmd.exe +quit

【三、修改VDF里的路径】
用记事本打开 upload.vdf，把两处路径改成你实际存放位置：
  "contentfolder"     "C:\webshop_build\content"
  "previewfile"       "C:\webshop_build\workshop_preview.png"
（如果解压到其他盘，改成对应路径）

【四、上传】
打开 CMD（Win+R 输入 cmd），执行：
  D:\steamcmd\steamcmd.exe +login 你的Steam账号 +workshop_build_item "C:\webshop_build\upload.vdf" +quit

会依次要求：
  1. password: 输入Steam密码（不显示字符正常）
  2. 手机Steam App确认登录
  3. 自动上传

【五、成功标志】
  Logged in OK
  ...
  Uploaded workshop item: <物品ID>
记下物品ID！以后更新版本要改 VDF 里的 publishedfileid
（首次上传=0，更新=填物品ID）

【六、上传后】
1. Steam → 库 → Slay the Spire 2 → 创意工坊 → 我的物品
2. 点「编辑」→ 可见性改「公开」
3. 补充游戏内截图

【内容清单】
content\  23个文件（清单+pck+dll+pdb+19张PNG）
workshop_preview.png  工坊预览图
upload.vdf  上传配置
'@
Set-Content (Join-Path $pkg '上传说明.txt') $readme -Encoding UTF8

# 5. 压缩
$zip = Join-Path $root 'workshop_upload_package.zip'
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path $pkg -DestinationPath $zip -CompressionLevel Optimal
Write-Host ("上传包: " + $zip)
Write-Host ("大小: " + [Math]::Round((Get-Item $zip).Length/1MB,2) + " MB")
Write-Host ("SHA256: " + (Get-FileHash $zip -Algorithm SHA256).Hash)
Get-ChildItem $pkg -Recurse -File | ForEach-Object { '  ' + $_.FullName.Replace($pkg,'') } | Select-Object -First 30