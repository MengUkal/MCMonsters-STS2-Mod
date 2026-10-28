# 创意工坊发布指南

## 一、发布前检查

- ✅ **移除临时调试命令**：`src\MCMonsters\DevTools\McWitherReproCmds.cs`（mcfight/mcwither）是复现工具，发布构建前删除或注释整个文件，防止控制台命令泄漏给玩家
- ✅ 模组清单 `MCMonsters.json`：**无任何前置依赖**，`min_game_version` = 0.107.1
- ✅ 贴图全部内置：19 张怪物贴图以官方 import 链（`.png.import` → `.ctex` + `spatlas.source_path`）打进 `MCMonsters.pck`。**不写游戏目录、不需要管理员权限、不产生任何散图**
- ✅ 工坊内容包 `workshop_content\`：manifest + pck + dll（约 5.0MB，无 PNG）
- ✅ 工坊预览图 `workshop_preview.png`（512×512）
- ✅ 上传配置 `workshop_build.vdf`
- ✅ 本机实测（游戏正式版 v0.107.1）：MOD 正常加载、2 个 Harmony 补丁、三幕遭遇池注入成功、19/19 贴图资源链加载成功、16/16 BUFF 图标加载成功、僵尸/恶魂/潜影贝战斗与减伤机制正常；测试分支 v0.111.0 亦实测运行正常

## 二、上传步骤

### 1. 下载 SteamCMD
- 官方地址：https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
- 解压到任意目录，例如 `D:\steamcmd\`

### 2. 登录 SteamCMD（首次会要 Steam Guard 验证码）
```
D:\steamcmd\steamcmd.exe
login <你的Steam账号>
```
输入密码 + Steam Guard 验证码（手机令牌或邮箱）。

### 3. 上传工坊物品
```
workshop_build_item "<本目录>\workshop_build.vdf"
```
- 首次上传后 SteamCMD 会返回一个 **publishedfileid**（物品ID）
- 打开 Steam → 库 → Slay the Spire 2 → 创意工坊 → 「我的物品」就能看到新物品（默认私密）

### 4. 完善工坊页面
在 Steam 创意工坊页面点「编辑」：
- 上传截图（游戏内截图效果更好）
- 选中分类：`Monsters` / `Gameplay`
- 语言：简体中文 + English
- 可见性改为「公开」

### 5. 更新版本时
- 修改 `workshop_build.vdf` 里的 `publishedfileid`（换成你的物品ID）
- 提升 `version`（清单里）和 `changenote`
- 重新执行第 3 步命令即可更新（不用重新传预览图）

## 三、注意事项

### 本地验证（无需游戏根目录散图）
1. 把 `workshop_content\` 整个文件夹复制到 `mods\MCMonsters\`（模拟订阅后的本地目录）
2. 确保游戏根目录**没有**任何怪物 PNG，也**没有** `mc_textures\` 目录
3. 启动游戏，日志应出现：
   - `Found mod manifest file ...\mods\MCMonsters\MCMonsters.json`
   - `[MCMonsters] Init complete: 2 patched methods.`
   - `[MCMonsters] Act0/1/2 pool injected: ...`
4. 进游戏新开一局（正式版没有 `act`/`fight` 调试命令），或在本地测试版里加临时控制台命令直接跳 `zombie_solo`/`ghast_elite`/`shulker_pair` 验证怪物显示与减伤
5. 可选离线校验：`powershell -ExecutionPolicy Bypass -File tools\verify_pck.ps1`

### 依赖关系
- 无前置模组。清单 `dependencies` 为空数组。

### 游戏版本
- `min_game_version: 0.107.1`（本版按正式版 0.107.1 程序集编译；同一份 DLL 对测试分支 0.111.0 兼容——伤害钩子已改为跨版本补丁实现）

### Steam 分步
- 如果你的 Steam 账号没有创意工坊上传权限（极少数情况），需要在 Steam 支持页面申请

## 四、文件清单

```
workshop_content\           ← 上传内容（整个文件夹）
  MCMonsters.json           ← 清单
  MCMonsters.pck            ← 美术/音效/图标/贴图 import 链 4.65MB
  MCMonsters.dll            ← 代码
workshop_preview.png        ← 工坊预览图
workshop_build.vdf          ← SteamCMD 上传配置
```

## 五、上传命令速查

```batch
:: 登录
D:\steamcmd\steamcmd.exe +login 你的账号 +quit
:: 上传
D:\steamcmd\steamcmd.exe +login 你的账号 +workshop_build_item "<本目录>\workshop_build.vdf" +quit
```
