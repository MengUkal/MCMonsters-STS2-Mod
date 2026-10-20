using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using HarmonyLib;

namespace MCMonsters.Patches;

/// <summary>
/// 把MC遭遇注入各幕遭遇池。
/// Act1弱怪池（IsWeak=true）：僵尸 → 骷髅 → 双蜘蛛
/// Act1强怪池（IsWeak=false）：僵尸+骷髅+蜘蛛 → 苦力怕+骷髅
/// Act2池：双烈焰人 / 双僵尸猪人 / 双凋零骷髅 / 猪灵蛮兵
/// Act3池：双末影人 / 双潜影贝 / 双女巫 / 双掠夺者
/// 注：单怪版 *Solo 均不进池（仅控制台测试用）。
/// </summary>
[HarmonyPatch]
public static class ActEncounterPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ActModel), "get_AllEncounters")]
    private static void InjectMcEncounters(ActModel __instance, ref IEnumerable<EncounterModel> __result)
    {
        if (__instance.Index == 0)
        {
            __result = __result.Concat(new EncounterModel[]
            {
                // 弱怪池（IsWeak=true，顺序即用户指定的从左到右）
                ModelDb.Encounter<Encounters.ZombieSolo>(),
                ModelDb.Encounter<Encounters.SkeletonSolo>(),
                ModelDb.Encounter<Encounters.SpiderPair>(),
                // 强怪池（IsWeak=false）
                ModelDb.Encounter<Encounters.ZombieSkeletonSpider>(),
                ModelDb.Encounter<Encounters.CreeperSkeleton>(),
                // 精英与BOSS（之前漏加！）
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
                // 组合遭遇
                ModelDb.Encounter<Encounters.BlazeWitherSkeleton>(),
                ModelDb.Encounter<Encounters.WitherSkeletonTriple>(),
                ModelDb.Encounter<Encounters.BlazeZombiePiglin>(),
                ModelDb.Encounter<Encounters.ZombiePiglinWitherSkeleton>(),
                // 精英与BOSS（之前漏加！）
                ModelDb.Encounter<Encounters.GhastElite>(),
                ModelDb.Encounter<Encounters.WitherBossEncounter>()
            });
        }
        else if (__instance.Index == 2)
        {
            __result = __result.Concat(new EncounterModel[]
            {
                ModelDb.Encounter<Encounters.EndermanPair>(),
                ModelDb.Encounter<Encounters.ShulkerPair>(),
                ModelDb.Encounter<Encounters.WitchPair>(),
                ModelDb.Encounter<Encounters.PillagerPair>(),
                // 精英与BOSS
                ModelDb.Encounter<Encounters.GiantElite>(),
                ModelDb.Encounter<Encounters.PillagerRaidElite>(),
                ModelDb.Encounter<Encounters.EnderDragonBoss>()
            });
        }
    }
}



