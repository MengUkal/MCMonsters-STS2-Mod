using System;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MCMonsters;

/// <summary>
/// MOD入口。
/// 1. 自安装贴图到 <游戏根>\mc_textures\（Spine 纹理按相对路径加载的引擎限制）
/// 2. 应用 Harmony 补丁（遭遇池注入）
/// 贴图安装失败时不注入遭遇池——避免怪物立绘缺失导致游戏崩溃。
/// </summary>
[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "MCMonsters";
    public const string TextureDirName = "mc_textures";

    /// <summary>贴图是否就绪（未就绪则不注入遭遇，走安全降级）</summary>
    public static bool TexturesReady { get; private set; }

    public static void Initialize()
    {
        TexturesReady = InstallBareTextures();

        var harmony = new Harmony(ModId.ToLowerInvariant());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        int patchCount = Harmony.GetAllPatchedMethods().Count();
        if (TexturesReady)
        {
            Log.Info($"[{ModId}] Init complete: {patchCount} patched methods. MC encounters injected.");
        }
        else
        {
            Log.Error($"[{ModId}] 贴图安装失败！MC遭遇池已跳过，怪物不会出现。请把 mods/MCMonsters/*.png 复制到 {TextureDirName} 文件夹（如游戏在C盘Program Files需管理员权限）");
        }
    }

    private static bool InstallBareTextures()
    {
        try
        {
            string? modDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(modDir) || !Directory.Exists(modDir))
            {
                Log.Warn($"[{ModId}] 找不到MOD目录");
                return false;
            }

            string gameRoot;
            try { gameRoot = Path.GetDirectoryName(global::Godot.OS.GetExecutablePath()) ?? ""; }
            catch { gameRoot = Directory.GetCurrentDirectory(); }
            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
            {
                gameRoot = Directory.GetCurrentDirectory();
            }

            string texDir = Path.Combine(gameRoot, TextureDirName);
            Directory.CreateDirectory(texDir);

            var pngs = Directory.GetFiles(modDir, "*.png", SearchOption.TopDirectoryOnly);
            if (pngs.Length == 0)
            {
                Log.Warn($"[{ModId}] MOD目录没有贴图文件");
                return false;
            }

            int ok = 0;
            foreach (var png in pngs)
            {
                try
                {
                    string dest = Path.Combine(texDir, Path.GetFileName(png));
                    if (File.Exists(dest) && new FileInfo(dest).Length == new FileInfo(png).Length)
                    {
                        ok++;
                        continue;
                    }
                    File.Copy(png, dest, overwrite: true);
                    ok++;
                }
                catch (Exception ex)
                {
                    Log.Warn($"[{ModId}] 复制贴图失败 {Path.GetFileName(png)}: {ex.Message}");
                }
            }

            bool ready = ok >= pngs.Length && ok > 0;
            Log.Info($"[{ModId}] 贴图安装: {ok}/{pngs.Length} -> {texDir} (ready={ready})");
            return ready;
        }
        catch (Exception ex)
        {
            Log.Error($"[{ModId}] 贴图安装异常: {ex.Message}");
            return false;
        }
    }
}