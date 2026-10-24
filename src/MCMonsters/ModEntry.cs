using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MCMonsters;

/// <summary>
/// MOD入口：应用 Harmony 补丁（遭遇池注入）+ 注入自定义音效到资源缓存。
///
/// 贴图不再需要安装到游戏根目录：19 张怪物贴图已经以官方同款 import 链
/// （.png.import → .godot/imported/*.ctex + spatlas.source_path）打进
/// MCMonsters.pck，Spine 通过 ResourceLoader 从 res:// 直接加载。
/// 因此本文件不写游戏目录、不需要管理员权限、不产生任何散图。
///
/// 音效：res://debug_audio/*.ogg|mp3 是以"裸文件"打进 pck 的（没有 .ogg.import /
/// .godot/imported/*.oggvorbisstr 这条导入链），ResourceLoader 会报
/// "No loader found for resource" 并返回 null —— 自定义音效全部哑火、日志刷错误。
/// 这里在初始化阶段自己解析这些文件，把 AudioStream 直接塞进游戏的
/// PreloadManager.Cache（键与 TmpSfx.GetPath 一致），引擎原有的
/// NDebugAudioManager.Play / AttackCommand 的 tmp sfx 路径即可正常工作，无需改 19 个怪物文件。
/// </summary>
[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "MCMonsters";

    private const string DebugAudioDir = "res://debug_audio";

    public static void Initialize()
    {
        var harmony = new Harmony(ModId.ToLowerInvariant());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        int patchCount = Harmony.GetAllPatchedMethods().Count();

        int sfxCount = RegisterDebugAudio();

        Log.Info($"[{ModId}] Init complete: {patchCount} patched methods, {sfxCount} custom sfx cached. MC encounters injected.");
    }

    /// <summary>
    /// Parses raw .ogg/.mp3 files packed in the mod pck and seeds the engine asset cache
    /// so that TmpSfx-based playback (WithAttackerFx / NDebugAudioManager) finds them.
    /// Returns the number of streams registered.
    /// </summary>
    private static int RegisterDebugAudio()
    {
        int count = 0;
        try
        {
            using DirAccess? dir = DirAccess.Open(DebugAudioDir);
            if (dir == null)
            {
                Log.Warn($"[{ModId}] debug audio dir not found: {DebugAudioDir}");
                return 0;
            }

            foreach (string fileName in dir.GetFiles())
            {
                // Only our own files: the game ships its own debug_audio folder at the same
                // res:// path, and its files are already importable (we must not shadow them).
                if (!fileName.StartsWith("mc_", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                bool isOgg = fileName.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase);
                bool isMp3 = fileName.EndsWith(".mp3", System.StringComparison.OrdinalIgnoreCase);
                if (!isOgg && !isMp3)
                {
                    continue;
                }

                string key = DebugAudioDir + "/" + fileName;
                if (PreloadManager.Cache.ContainsKey(key))
                {
                    count++;
                    continue;
                }

                byte[]? bytes = Godot.FileAccess.GetFileAsBytes(key);
                if (bytes == null || bytes.Length == 0)
                {
                    continue;
                }

                AudioStream? stream = isOgg
                    ? AudioStreamOggVorbis.LoadFromBuffer(bytes)
                    : AudioStreamMP3.LoadFromBuffer(bytes);

                if (stream == null)
                {
                    Log.Warn($"[{ModId}] failed to parse custom sfx: {key}");
                    continue;
                }

                PreloadManager.Cache.SetAsset(key, stream);
                count++;
            }
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[{ModId}] custom sfx registration failed: {ex.Message}");
        }

        return count;
    }
}
