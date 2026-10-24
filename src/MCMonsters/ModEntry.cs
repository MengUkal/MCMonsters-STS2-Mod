using System.Collections.Generic;
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
/// 音效：res://debug_audio/mc_*.ogg|mp3 是以"裸文件"打进 pck 的（没有
/// .ogg.import / .godot/imported/*.oggvorbisstr 这条官方导入链），ResourceLoader 会报
/// "No loader found for resource" 并返回 null —— 自定义音效全部哑火、日志刷错误。
/// 这里自己解析这些文件，把 AudioStream 塞进游戏的 PreloadManager.Cache
/// （键与 TmpSfx.GetPath 一致），引擎原有的 NDebugAudioManager.Play /
/// AttackCommand 的 tmp sfx 路径即可正常工作，无需改 19 个怪物文件。
///
/// 注意：PreloadManager.LoadAssetSets 每次进房间都会 Cache.UnloadAssets(
/// "已缓存但不在当前资产清单里的条目")，我们注入的 key 不属于任何官方资产集，
/// 会被一并清掉 —— 所以挂 UnloadAssets 后置补丁，卸载后立刻重新注入（从内存字节重新解析）。
/// </summary>
[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "MCMonsters";

    private const string DebugAudioDir = "res://debug_audio";

    /// <summary>Raw file bytes of our custom sfx, kept so we can re-register after cache unloads.</summary>
    private static readonly Dictionary<string, byte[]> _audioBytes = new();

    private static int _reinjectCount;

    public static void Initialize()
    {
        var harmony = new Harmony(ModId.ToLowerInvariant());
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        // Patch cache-unload explicitly (attribute-based PatchAll did not pick this one up):
        // PreloadManager.LoadAssetSets unloads every cached asset not in the current asset sets,
        // which evicts the sfx streams we inject below.
        try
        {
            MethodInfo? unload = AccessTools.Method(typeof(AssetCache), nameof(AssetCache.UnloadAssets));
            MethodInfo? postfix = AccessTools.Method(typeof(ModEntry), nameof(AfterAssetCacheUnload));
            if (unload != null && postfix != null)
            {
                harmony.Patch(unload, postfix: new HarmonyMethod(postfix));
                Log.Info($"[{ModId}] patched {nameof(AssetCache.UnloadAssets)} for sfx re-injection");
            }
            else
            {
                Log.Warn($"[{ModId}] could not resolve AssetCache.UnloadAssets (unload={unload != null}, postfix={postfix != null})");
            }
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[{ModId}] failed to patch AssetCache.UnloadAssets: {ex.Message}");
        }

        LoadCustomAudioBytes();
        int sfxCount = PushAudioToCache();

        Log.Info($"[{ModId}] Init complete: {Harmony.GetAllPatchedMethods().Count()} patched methods, {sfxCount} custom sfx cached. MC encounters injected.");
    }

    /// <summary>
    /// Collects .ogg/.mp3 bytes for our own mc_* files. The game ships its own debug_audio
    /// folder at the same res:// path, so filter to our prefix and never shadow its assets.
    /// </summary>
    private static void LoadCustomAudioBytes()
    {
        try
        {
            using DirAccess? dir = DirAccess.Open(DebugAudioDir);
            if (dir == null)
            {
                Log.Warn($"[{ModId}] debug audio dir not found: {DebugAudioDir}");
                return;
            }

            foreach (string fileName in dir.GetFiles())
            {
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
                if (_audioBytes.ContainsKey(key))
                {
                    continue;
                }

                byte[]? bytes = Godot.FileAccess.GetFileAsBytes(key);
                if (bytes != null && bytes.Length > 0)
                {
                    _audioBytes[key] = bytes;
                }
            }
        }
        catch (System.Exception ex)
        {
            Log.Warn($"[{ModId}] custom sfx scan failed: {ex.Message}");
        }
    }

    /// <summary>Parses every collected file and puts the resulting AudioStream into the game asset cache.</summary>
    private static int PushAudioToCache()
    {
        int count = 0;
        foreach (KeyValuePair<string, byte[]> kv in _audioBytes)
        {
            try
            {
                // NOTE: do NOT gate this on Cache.ContainsKey — once a load fails the cache keeps
                // the key with a null value, so ContainsKey stays true while playback keeps failing.
                // Overwriting unconditionally is cheap (files are tiny) and always leaves a valid stream.
                AudioStream? stream = kv.Key.EndsWith(".ogg", System.StringComparison.OrdinalIgnoreCase)
                    ? AudioStreamOggVorbis.LoadFromBuffer(kv.Value)
                    : AudioStreamMP3.LoadFromBuffer(kv.Value);

                if (stream == null)
                {
                    Log.Warn($"[{ModId}] failed to parse custom sfx: {kv.Key}");
                    continue;
                }

                PreloadManager.Cache.SetAsset(kv.Key, stream);
                count++;
            }
            catch (System.Exception ex)
            {
                Log.Warn($"[{ModId}] custom sfx register failed for {kv.Key}: {ex.Message}");
            }
        }

        return count;
    }

    /// <summary>
    /// PreloadManager.LoadAssetSets unloads every cached asset that is not part of the current
    /// asset sets; our injected sfx are not in any set, so they get evicted on room transitions.
    /// Re-inject right after each unload so tmp-sfx playback keeps working all run long.
    /// </summary>
    private static void AfterAssetCacheUnload()
    {
        if (_audioBytes.Count == 0)
        {
            return;
        }
        int n = PushAudioToCache();
        if (++_reinjectCount <= 3)
        {
            Log.Info($"[{ModId}] sfx re-injected after cache unload (#{_reinjectCount}, {n} streams)");
        }
    }
}
