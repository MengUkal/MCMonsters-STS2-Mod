# 创意工坊发布指南

## 一、发布前检查（已全部完成）

- ✅ 模组清单 `MCMonsters.json` 含 BaseLib 依赖声明（订阅者缺前置会自动提示）
- ✅ DLL 内置**裸图自安装**：工坊版本没有游戏根目录写权限问题——MOD 首次加载时自动把 19 张怪物贴图从 MOD 目录复制到游戏根目录
- ✅ 工坊内容包 `workshop_content\`：manifest + pck + dll + pdb + 19 张 PNG（7.4MB）
- ✅ 工坊预览图 `workshop_preview.png`（512×512）
- ✅ 上传配置 `workshop_build.vdf`

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
workshop_build_item "D:\塔2MOD\MCMonsters\workshop_build.vdf"
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

### 首次发布前的本地验证
1. 用**工坊视角**测试：把 `workshop_content\` 整个文件夹复制到 `mods\MCMonsters\`（模拟订阅后本地目录），再删掉游戏根目录的 19 张 PNG，启动游戏
2. 看日志应有：`[MCMonsters] Installed 19 bare textures to game root`
3. 进游戏 `act 1` + `fight zombie_solo` 验证怪物正常显示

### 依赖关系
- 订阅者必须先装 **BaseLib**（工坊搜 "BaseLib"，作者 Alchyr）
- 我们的 manifest 已声明依赖，游戏加载时会检查

### 游戏版本
- `min_game_version: 0.107.1` —— 游戏大版本更新后 BaseLib/本模组都可能需要更新

### Steam 分步
- 如果你的 Steam 账号没有创意工坊上传权限（极少数情况），需要在 Steam 支持页面申请

## 四、文件清单

```
workshop_content\           ← 上传内容（整个文件夹）
  MCMonsters.json           ← 清单（含依赖）
  MCMonsters.pck            ← 美术/音效/图标资源 6.1MB
  MCMonsters.dll            ← 代码
  MCMonsters.pdb            ← 调试符号（可选，建议保留方便报错定位）
  *.png × 19                ← 怪物裸图（自安装用）
workshop_preview.png        ← 工坊预览图
workshop_build.vdf          ← SteamCMD 上传配置
```

## 五、上传命令速查

```batch
:: 登录
D:\steamcmd\steamcmd.exe +login 你的账号 +quit
:: 上传
D:\steamcmd\steamcmd.exe +login 你的账号 +workshop_build_item "D:\塔2MOD\MCMonsters\workshop_build.vdf" +quit
```
