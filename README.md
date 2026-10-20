# MC怪物 / Minecraft Monsters — 杀戮尖塔2 Mod

把《我的世界》的经典怪物加入《杀戮尖塔2》。共 **19 只怪物、25 种遭遇**，覆盖全部三幕。

## 安装方法

1. 确认游戏版本 ≥ 0.111.0（本模组无前置依赖，开箱即用）
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

游戏内按 **Shift+8** 打开控制台：

```
act 3                    跳到第三幕
fight shulker_pair       直接进入指定遭遇
kill all                 全灭敌人
heal                     回满血
godmode                  无敌
```

主要遭遇 ID：`zombie_solo` `skeleton_solo` `spider_pair` `creeper_skeleton` `warden_boss` `blaze_pair` `zombie_piglin_pair` `wither_skeleton_pair` `piglin_brute_solo` `ghast_elite` `wither_boss_encounter` `enderman_pair` `shulker_pair` `pillager_pair` `witch_pair` `giant_elite` `pillager_raid_elite` `ender_dragon_boss`

## 已知限制

- 潜影贝「闭壳/开壳」的立绘切换未实现（Spine 引擎不支持持久换皮），仅保留壳甲机制与 BUFF 图标
- 部分怪物音效为 MC 原版短音效，BOSS 音效（凋灵/末影龙/监守者）取自 Java 版音效包
- 三幕 BOSS 的跑图记录图标（run history）显示为空白

## 技术说明

- 加载架构：`MCMonsters.dll`（Harmony 补丁 + 怪物模型/力量/遭遇）+ `MCMonsters.pck`（Spine 美术资源 + 音效 + 图标 + 本地化）
- 贴图加载：完全复刻官方 pck 的 import 链——`<怪>.png.import` remap 到 `.godot/imported/<怪>.png-<hash>.ctex`（56 字节官方头 + 无损 WebP），`.spatlas` 内写入 `source_path`，Spine 运行时据此把页面名解析成 `res://animations/monsters/<怪>/<怪>.png` 再由 `ResourceLoader` 命中 ctex。全程不需要游戏根目录散图，也不需要写权限。
- 美术：全部怪物为自绘 Spine 4.2.43 双帧骨架（idle/攻击/受击/死亡），由 `tools/ArtGen` 生成
- 构建：`build.ps1`（需要 .NET 9 SDK；`tools/verify_pck.ps1` 可离线校验 pck 内 19 只怪的贴图 import 链）

## 版本

**1.5.2** — 修复「19 张怪物 PNG 必须放到游戏根目录」的垃圾设计：贴图改为 pck 内置 import 链，订阅即用、无需散图；移除 DLL 自安装/写游戏目录逻辑；适配游戏 0.111.0 API（`ModifyDamageMultiplicative` 新增 `CardPlay` 参数）；修复 `mc_textures` 双前缀导致的僵尸等贴图丢失；pck 体积 6.25MB → 4.92MB。

**1.5.0** — 第三层全面重做（末影人/潜影贝/掠夺者/女巫/巨人/末影龙/袭击队长）、第二层遭遇扩充、全怪物音效、BUFF 图标补齐
