using System.Linq;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace MCMonsters;

/// <summary>
/// MOD入口：只做一件事——应用 Harmony 补丁（遭遇池注入）。
///
/// 贴图不再需要安装到游戏根目录：19 张怪物贴图已经以官方同款 import 链
/// （.png.import → .godot/imported/*.ctex + spatlas.source_path）打进
/// MCMonsters.pck，Spine 通过 ResourceLoader 从 res:// 直接加载。
/// 因此本文件不写游戏目录、不需要管理员权限、不产生任何散图。
/// </summary>
[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "MCMonsters";

    public static void Initialize()
    {
        var harmony = new Harmony(ModId.ToLowerInvariant());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        int patchCount = Harmony.GetAllPatchedMethods().Count();
        Log.Info($"[{ModId}] Init complete: {patchCount} patched methods. MC encounters injected.");

    }
}
