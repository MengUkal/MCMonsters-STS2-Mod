# MC怪物 / Minecraft Monsters — 杀戮尖塔2 Mod

把《我的世界》的经典怪物加入《杀戮尖塔2》。共 **19 只怪物、25 种遭遇**，覆盖全部三幕。

## 安装方法

1. 确认游戏版本 ≥ 0.107.1（**正式版 v0.107.1 可用**；测试分支 v0.111.0 同一份文件兼容。本模组无前置依赖，开箱即用）
2. 把 `MCMonsters` 文件夹（含 `MCMonsters.json`、`MCMonsters.pck`、`MCMonsters.dll`）放入：
   ```
   <游戏目录>\mods\MCMonsters\
   ```
   > 创意工坊版本订阅后由游戏自动放入，无需手动操作
3. 启动游戏，新开一局即可在遭遇池中遇到

**不需要往游戏根目录放任何贴图文件。** 19 张怪物贴图已按官方资源格式（`.png.import` → `.ctex` 缓存 + atlas 的 `source_path`）完整打进 `MCMonsters.pck`，启动后直接从 `res://` 加载。

## 怪物图鉴

### 第一幕
| 怪物 | 血量 | 机制 |
|---|---|---|
| 僵尸 | 48 | 斩击/撕咬/低吼（+2力量）|
| 骷髅 | 36 | 精准射击：未格挡的伤害翻倍 |
| 蜘蛛 | 22 | 扑击/网陷阱/攀爬格挡 |
| 苦力怕 | 36 | 3回合嘶嘶蓄力 → 30点全体爆炸（含自爆）|
| 鸡骑士（精英）| 70+36 | 双命召唤：先杀鸡，骑士复活再战 |
| 监守者（BOSS）| 220 | 【警觉】你空过回合 → 意图变蓄力+2力量；循环：重拳18/缠结/狂怒2×7 |

### 第二幕
| 怪物 | 血量 | 机制 |
|---|---|---|
| 凋零骷髅 | 58 | 【凋零之触】未被格挡的攻击给你1层凋零（每回合掉血）|
| 烈焰人 | 74 | 【烈焰之躯】每次攻击你-2血；火球+翱翔减伤 |
| 僵尸猪人 | 96 | 中立蓄怒 vs 被激怒反击的双状态 |
| 猪灵蛮兵 | 77 | 纯物理重装 |
| 恶魂（精英）| 3 | 【完美格挡解谜】免疫伤害+99人工制品，只有足额格挡火球才能反伤 |
| 凋灵（BOSS）| 356 | 【幽冥护壁】半血获得99层人工制品；凋零之触+循环4招 |

### 第三幕
| 怪物 | 血量 | 机制 |
|---|---|---|
| 末影人 | 150 | 【空间闪避】每次受攻击+5格挡（每回合2次）→克制多段攻击 |
| 潜影贝 | 165 | 【壳甲】闭壳期间受到伤害减半 |
| 掠夺者 | 150 | 【装填中断】装填后受12+伤害 → 攻击取消 |
| 女巫 | 155 | 【炼金术】单次15+伤害塞你1张灼伤；喝药可被打断 |
| 巨人（精英）| 300 | 抬脚蓄力 → 践踏35（屏幕震动）|
| 掠夺者突袭（精英）| 3×150+130 | 3掠夺者+袭击队长（号令全军/狙击/压制）|
| 末影龙（BOSS）| 600 | 三形态，最终形态【天空之翼】受到攻击伤害减半 |

## 遭遇配置

- **一层**：弱怪池3种 / 强怪池2种 / 精英1 / BOSS1
- **二层**：弱怪池2种 / 强怪池7种 / 精英1 / BOSS1
- **三层**：强怪池4种 / 精英2 / BOSS1

官方池子与本 Mod 池子合并，按强度分级抽选。

## 开发调试

游戏内按 **Shift+8** 打开控制台。

注意：**正式版 v0.107.1 没有 `fight`（直接进入指定遭遇）和 `act`（跳幕）命令**——这两个命令是测试分支后续版本才加的。正式版可用的常用命令：

```
room <id>                跳到指定房间
travel                   跳到地图上任意房间
kill all                 全灭敌人
heal                     回满血
godmode                  无敌
unlock all               解锁全部内容
```

调试遭遇池建议直接新开一局多打几层，或临时在 mod 里加一个 `AbstractConsoleCmd` 子类（游戏的 `DevConsole` 会自动发现 mod 程序集里的命令子类，0.107.1 也支持），发布前删掉即可。

以下为**测试分支 v0.111.0** 才可用的命令，仅供对照：

```
act 3                    跳到第三幕
fight shulker_pair       直接进入指定遭遇
```

主要遭遇 ID（配合 beta 的 `fight` 命令使用）：`zombie_solo` `skeleton_solo` `spider_pair` `creeper_skeleton` `warden_boss` `blaze_pair` `zombie_piglin_pair` `wither_skeleton_pair` `piglin_brute_solo` `ghast_elite` `wither_boss_encounter` `enderman_pair` `shulker_pair` `pillager_pair` `witch_pair` `giant_elite` `pillager_raid_elite` `ender_dragon_boss`

## 已知限制

- 潜影贝「闭壳/开壳」的立绘切换未实现（Spine 引擎不支持持久换皮），仅保留壳甲机制与 BUFF 图标
- 音效：pck 内音频文件的 import 链仍在修复中（当前版本音效可能不播放）
- 正式版 v0.107.1 与测试分支 v0.111.0 **均已实机验证通过**（同一份 DLL 两版通用）

## 技术说明

- 加载架构：`MCMonsters.dll`（Harmony 补丁 + 怪物模型/力量/遭遇）+ `MCMonsters.pck`（Spine 美术资源 + 音效 + 图标 + 本地化）
- 贴图加载：完全复刻官方 pck 的 import 链——`<怪>.png.import` remap 到 `.godot/imported/<怪>.png-<hash>.ctex`（56 字节官方头 + 无损 WebP），`.spatlas` 内写入 `source_path`，Spine 运行时据此把页面名解析成 `res://animations/monsters/<怪>/<怪>.png` 再由 `ResourceLoader` 命中 ctex。全程不需要游戏根目录散图，也不需要写权限。
- BUFF 图标：官方 `power_atlas.tpsheet` 原样使用，**严禁在 mod pck 里覆写官方图集文件**；16 个自定义 BUFF 图标走游戏内置兜底路径 `res://images/powers/<id>.png`（配好 `.png.import` → `.ctex` 即可）
- 美术：全部怪物为自绘 Spine 4.2.43 双帧骨架（idle/攻击/受击/死亡），由 `tools/ArtGen` 生成
- 伤害修正（恶魂免疫 / 潜影贝壳甲 / 末影龙天空之翼）：**不用 `AbstractModel.ModifyDamageMultiplicative` 重写**（该虚方法在 0.107.x 是 5 参数、0.111.x 是 6 参数，一份 DLL 无法同时兼容），改为 Harmony 补丁挂在两版参数一致的 `Hook.ModifyDamage` 上，按受击者身上的力量修正最终伤害；补丁参数必须按名字声明，不能用 `object[] __args`（该方法带 `out` 参数，用 `__args` 会导致 out 值丢失、`AfterModifyingDamageAmount` 收到 null 崩溃）
- 构建：`build.ps1`（需要 .NET 9 SDK；`tools/verify_pck.ps1` 可离线校验 pck 内 19 只怪的贴图 import 链）

## 版本


**1.6.1** — 修复**严重 BUG：进入第二幕时游戏卡死**。根因：游戏在幕切换/跑图记录界面会加载 `res://images/ui/run_history/<遭遇ID>.png`（+`_outline`），本模组此前没有提供这 34 个遭遇的图标，缺失导致 `NAssetLoader` 同步回退加载循环、严重时卡死。现已用 `tools/make_runhistory_icons.ps1` 为全部 34 个遭遇生成图标 + 白色轮廓（128×128），组装器补上 `.png.import → .ctex` 导入链。本机 v0.107.1 实测进入二幕/凋灵场景 0 报错。
**1.6.0** — 支持游戏**正式版 v0.107.1**（此前只支持测试分支 0.111.0）。改动：恶魂/潜影贝/末影龙的伤害修正从 `ModifyDamageMultiplicative` 重写改为 `Hook.ModifyDamage` 后置补丁，一份 DLL 同时兼容 0.107.x 与 0.111.x；清单 `min_game_version` 降为 `0.107.1`。已在正式版实机验证：模组加载、19/19 怪物贴图、16/16 BUFF 图标、三幕遭遇池注入、僵尸/恶魂/潜影贝战斗、恶魂伤害归零与潜影贝壳甲减半；**测试分支 v0.111.0 同样实测运行正常**。

**1.5.3** — 修复状态/BUFF 图标全部消失：删除旧版塞进 pck 的 `power_atlas.tpsheet` 覆写（游戏 0.111.0 重排官方图集后旧 region 全部错位/越界），自定义 BUFF 图标改走官方兜底 png 路径。

**1.5.2** — 修复「19 张怪物 PNG 必须放到游戏根目录」的垃圾设计：贴图改为 pck 内置 import 链，订阅即用、无需散图；移除 DLL 自安装/写游戏目录逻辑；适配游戏 0.111.0 API（`ModifyDamageMultiplicative` 新增 `CardPlay` 参数）；修复 `mc_textures` 双前缀导致的僵尸等贴图丢失；pck 体积 6.25MB → 4.92MB。

**1.5.0** — 第三层全面重做（末影人/潜影贝/掠夺者/女巫/巨人/末影龙/袭击队长）、第二层遭遇扩充、全怪物音效、BUFF 图标补齐
