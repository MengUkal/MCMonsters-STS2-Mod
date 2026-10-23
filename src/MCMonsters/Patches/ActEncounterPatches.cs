using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using HarmonyLib;

namespace MCMonsters.Patches;

/// <summary>
/// 把MC遭遇注入各幕遭遇池。
/// 每幕首次生成池时会打印一次诊断日志（排查"遇不到怪物"用）。
/// </summary>
[HarmonyPatch]
public static class ActEncounterPatches
{
    private static readonly HashSet<int> _loggedActs = new HashSet<int>();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActModel), "get_AllEncounters")]
    private static void InjectMcEncounters(ActModel __instance, ref IEnumerable<EncounterModel> __result)
    {
        if (__instance.Index == 0)
        {
            __result = __result.Concat(new EncounterModel[]
            {
                ModelDb.Encounter<Encounters.ZombieSolo>(),
                ModelDb.Encounter<Encounters.SkeletonSolo>(),
                ModelDb.Encounter<Encounters.SpiderPair>(),
                ModelDb.Encounter<Encounters.ZombieSkeletonSpider>(),
                ModelDb.Encounter<Encounters.CreeperSkeleton>(),
                ModelDb.Encounter<Encounters.ChickenJockeyElite>(),
                ModelDb.Encounter<Encounters.WardenBoss>()
            });
        }
        else if (__instance.Index == 1)
        {
            __result = __result.Concat(new EncounterModel[]
            {
                ModelDb.Encounter<Encounters.BlazePair>(),
                ModelDb.Encounter<Encounters.ZombiePiglinPair>(),
                ModelDb.Encounter<Encounters.WitherSkeletonPair>(),
                ModelDb.Encounter<Encounters.PiglinBruteSolo>(),
                ModelDb.Encounter<Encounters.PiglinBrutePair>(),
                ModelDb.Encounter<Encounters.BlazeWitherSkeleton>(),
                ModelDb.Encounter<Encounters.WitherSkeletonTriple>(),
                ModelDb.Encounter<Encounters.BlazeZombiePiglin>(),
                ModelDb.Encounter<Encounters.ZombiePiglinWitherSkeleton>(),
                ModelDb.Encounter<Encounters.GhastElite>(),
                ModelDb.Encounter<Encounters.WitherBossEncounter>()
            });
        }
        else if (__instance.Index == 2)
        {
            // v1.7.0：末影人改为单怪遭遇（中立激怒机制只适合单怪），endeman_pair 仍保留供控制台测试
            __result = __result.Concat(new EncounterModel[]
            {
                ModelDb.Encounter<Encounters.EndermanSolo>(),
                ModelDb.Encounter<Encounters.ShulkerPair>(),
                ModelDb.Encounter<Encounters.WitchPair>(),
                ModelDb.Encounter<Encounters.PillagerPair>(),
                ModelDb.Encounter<Encounters.GiantElite>(),
                ModelDb.Encounter<Encounters.PillagerRaidElite>(),
                ModelDb.Encounter<Encounters.EnderDragonBoss>()
            });
        }

        // 诊断：每幕首次生成池时打印注入结果（排查"遇不到怪物"问题用）
        try
        {
            if (_loggedActs.Add(__instance.Index))
            {
                var list = __result.ToList();
                var mc = list.Where(e =>
                    e.Id.Entry.StartsWith("ZOMBIE") || e.Id.Entry.StartsWith("SKELETON") ||
                    e.Id.Entry.StartsWith("SPIDER") || e.Id.Entry.StartsWith("CREEPER") ||
                    e.Id.Entry.StartsWith("CHICKEN") || e.Id.Entry.StartsWith("WARDEN") ||
                    e.Id.Entry.StartsWith("BLAZE") || e.Id.Entry.StartsWith("WITHER") ||
                    e.Id.Entry.StartsWith("PIGLIN") || e.Id.Entry.StartsWith("GHAST") ||
                    e.Id.Entry.StartsWith("ENDER") || e.Id.Entry.StartsWith("SHULKER") ||
                    e.Id.Entry.StartsWith("WITCH") || e.Id.Entry.StartsWith("PILLAGER") ||
                    e.Id.Entry.StartsWith("GIANT") || e.Id.Entry.StartsWith("RAID"));
                Log.Info("[MCMonsters] Act" + __instance.Index + " pool injected: total=" + list.Count
                    + " mc=" + mc.Count() + " -> " + string.Join(" | ", mc.Select(e => e.Id.Entry)));
            }
        }
        catch { /* 诊断日志失败不影响注入 */ }
    }
}
