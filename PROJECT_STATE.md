# MCMonsters 项目状态交接（供新对话使用）
> 新对话开场语：「先 `git fetch`，读一下 D:\塔2MOD\MCMonsters\PROJECT_STATE.md 然后继续」
> 更新时间：2026-11-13
> ⚠️ 跨机器协作：**远端 master 才是事实**。开工先 `git fetch origin` + `git log HEAD..origin/master`，公司机/家里机都可能落后多个提交（v1.8.0 时代就因此差点覆盖家里成果，见踩坑记录坑34）。

## 项目是什么
《杀戮尖塔2》(Slay the Spire 2, Godot 4.5.1) 的我的世界怪物 Mod。
19 只怪物 / 34 个遭遇类（25 进池 + 9 控制台测试），覆盖三幕，4精英3BOSS。

## 当前版本：v1.9.0（已发布）
- v1.9.0（2026-11-13 公司机）：**适配游戏测试分支 v0.113.0 大更新**，同一份 DLL/pck 继续兼容 v0.107.1 / v0.111.0
  1. `AbstractModel.AfterDeath` 在 0.113 新增 `CardModel? source` → 鸡骑士/末影龙复活力量改为 `Patches/ReviveDeathPatches.cs`（`Hook.AfterDeath` 后置补丁，参数名注入三版通用）
  2. 新版「百科大全→怪物图鉴」按遭遇标题排序 → 补 `pck_content/MCMonsters/localization/{eng,zhs}/encounters.json`（34 遭遇 × title/loss），否则开图鉴 `LocException` 崩溃
  3. `ModEntry` 从 `PatchAll` 改为逐类补丁隔离（日志「属性补丁类 N 应用 / M 跳过」）
  4. 删除发布前临时调试命令 `src/MCMonsters/DevTools/`（mcfight/mcwither，编译也已不兼容 0.113）
- 本机 v0.113.0 实测：3 补丁类全应用；25 个遭遇控制台 `fight` 全部正常进入；战斗渲染/动画/意图正常；图鉴正常显示 MC 怪；日志 0 异常。**复活机制（鸡骑士/末影龙）自动化只验到触发前状态，建议制作人再实战验一次三形态/两条命。**
- v1.8.0（2026-10-27 家里机）：凋零骷髅哨兵错峰起手、末影龙复活后永久免疫修复、意图图标遮挡修复
- v1.7.2/1.7.1：所有招式异常护栏（修组合遭遇敌方回合卡住）、凋零之触修复、音效缓存注入（PreloadManager）
- v1.7.0：第三层平衡重做（末影人单怪激怒、潜影贝错峰、女巫重做）

## 各处发布状态
| 位置 | 状态 |
|---|---|
| Steam 创意工坊 | **已更新 v1.9.0**（2026-11-13，item 3823425588）|
| GitHub | **已推送 v1.9.0**（MengUkal 身份），https://github.com/MengUkal/MCMonsters-STS2-Mod |
| 本机游戏 | D:\steamwork\steamapps\common\Slay the Spire 2\mods\MCMonsters（v1.9.0 已部署，MD5 校验）|
| 拖拽发布包 | D:\塔2MOD\MCMonsters\MCMonsters_v1.9.0.zip |
| 工坊上传暂存 | C:\webshop_build\（content / workshop_preview.png / workshop_build.vdf；ASCII 路径是硬要求）|

## 关键技术事实（防重复踩坑）
- 贴图全部打进 pck（官方 import 链：`.png.import` → `.godot/imported/*.ctex` + spatlas `source_path`），游戏根目录零散图、DLL 不自安装
- 严禁在 mod pck 覆写官方 `power_atlas.tpsheet`；16 个自定义 BUFF 图标走官方兜底 `res://images/powers/<id>.png`
- 跨分支兼容三件套：伤害修正挂 `Hook.ModifyDamage`、复活触发挂 `Hook.AfterDeath`（都按参数名注入）、补丁逐类隔离
- pck 目录条目必须路径排序；条目偏移要加 fileBase（v3 = 112）；解析用 `D:\塔2MOD\tools\pck_tool.ps1`
- 遭遇池注入：HarmonyPostfix 挂 `ActModel.get_AllEncounters`，按 `__instance.Index`（0/1/2）Concat
- 本机 v0.113.0 池规模：Act0 total=29 mc=7；Act1 total=31 mc=11；Act2 total=25 mc=7
- 音效：`res://debug_audio/mc_*.ogg|mp3` 是裸文件，`ModEntry` 手动解析塞进 `PreloadManager.Cache` 并挂 `AssetCache.UnloadAssets` 后置补丁重新注入
- 新版图鉴需要 `encounters` 表；遭遇/怪物标题规律 = `<MODEL_ID>.title`（反编译确认，别猜）

## 已知限制（README「已知限制」同步）
1. 潜影贝开壳/闭壳立绘切换未实现（引擎限制，用 BUFF 表达）
2. 怪物音效仍在补全
3. 若有新遭遇：必须同时生成 run_history 图标（`tools/make_runhistory_icons.ps1`）
4. 末影龙在小窗口（1550×926）下右半略超出画面（`PositionEnemies` 按 Bounds 排布、大怪未用专属镜头）——正常分辨率下待复查

## 常用文件/命令
- `build.ps1`：构建+打包+部署（本机需 `$env:STS2_DIR="D:\steamwork\steamapps\common\Slay the Spire 2"`）
- `tools/make_workshop_update.ps1`：staging 到 C:\webshop_build + 生成 VDF + steamcmd 上传（`-NoUpload` 只暂存）
- `工坊描述.txt`：工坊描述源文件（VDF 从这里生成）
- `踩坑记录.md`：坑1~34；`遭遇代码总览.md`：怪物/遭遇实现索引
- 测试用（仓库外）：`D:\塔2MOD\tools\gamectl.ps1`、`smoke_fights.ps1`、`pck_tool.ps1`
