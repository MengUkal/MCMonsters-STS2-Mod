using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MCMonsters;

/// <summary>
/// MOD入口。游戏 ModManager 加载 MCMonsters.dll 后，
/// 通过 [ModInitializer] 找到本类并调用 Initialize()。
/// 怪物/遭遇本身由 ModelDb.Init() 自动发现（反射扫描MOD程序集），
/// Harmony补丁（遭遇池注入）在这里手动应用——
/// 注意：一旦声明了 [ModInitializer]，游戏就不再自动执行 Harmony.PatchAll。
/// </summary>
[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "MCMonsters";

    public static void Initialize()
    {
        // 1. 自安装裸名贴图：Spine 纹理按相对路径（游戏根目录）加载，
        //    创意工坊订阅用户的游戏根目录没有这些文件 → 从MOD目录自动复制过去
        InstallBareTextures();

        // 2. Harmony补丁
        var harmony = new Harmony(ModId.ToLowerInvariant());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        int patchCount = Harmony.GetAllPatchedMethods().Count();
        Log.Info($"[{ModId}] Init complete: {patchCount} patched methods. Zombie injected into Act 1 encounter pools.");
    }

    /// <summary>
    /// 把MOD目录里的怪物裸图复制到游戏根目录（Spine 纹理加载依赖）。
    /// 创意工坊版本必须自安装——订阅内容的目录不是游戏根目录。
    /// </summary>
    private static void InstallBareTextures()
    {
        try
        {
            string? modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(modDir) || !Directory.Exists(modDir))
            {
                Log.Warn($"[{ModId}] Texture install skipped: mod dir not found");
                return;
            }

            // 游戏根目录 = 可执行文件所在目录
            string gameRoot;
            try
            {
                gameRoot = Path.GetDirectoryName(global::Godot.OS.GetExecutablePath()) ?? "";
            }
            catch
            {
                gameRoot = Directory.GetCurrentDirectory();
            }
            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
            {
                gameRoot = Directory.GetCurrentDirectory();
            }

            var pngs = Directory.GetFiles(modDir, "*.png", SearchOption.TopDirectoryOnly);
            int copied = 0;
            foreach (var png in pngs)
            {
                string name = Path.GetFileName(png);
                string dest = Path.Combine(gameRoot, name);
                try
                {
                    if (File.Exists(dest))
                    {
                        // 已存在但大小不同则覆盖（MOD更新场景）
                        if (new FileInfo(dest).Length == new FileInfo(png).Length) continue;
                    }
                    File.Copy(png, dest, overwrite: true);
                    copied++;
                }
                catch (Exception ex)
                {
                    Log.Warn($"[{ModId}] Could not install texture {name}: {ex.Message}");
                }
            }
            Log.Info($"[{ModId}] Installed {copied} bare textures to game root ({pngs.Length} in mod dir)");
        }
        catch (Exception ex)
        {
            Log.Warn($"[{ModId}] Texture install failed: {ex.Message}");
        }
    }
}
