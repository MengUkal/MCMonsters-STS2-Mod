using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MCMonsters.Monsters;
using MCMonsters.Powers;

namespace MCMonsters.Patches;

/// <summary>
/// 复活型 BOSS 的死亡触发补丁（鸡骑士两条命 / 末影龙三形态）。
///
/// 为什么不直接 override PowerModel.AfterDeath：
///   0.107.x / 0.111.x 的 AbstractModel.AfterDeath 是
///     (PlayerChoiceContext, Creature, bool wasRemovalPrevented, float deathAnimLength)
///   0.113.0 起变成
///     (PlayerChoiceContext, Creature, CardModel? source, bool wasRemovalPrevented, float deathAnimLength)
///   虚方法 override 必须精确签名 → 一份 DLL 只能匹配一个分支。
///   本项目坚持「一份 DLL 同时兼容正式版 0.107.1 / 测试版 0.111.0 / 0.113.0」，
///   所以把死亡触发挂到两个分支签名都存在的 Hook.AfterDeath 后置补丁上：
///   Harmony 按参数名注入，declaration 里只写 creature / wasRemovalPrevented 两个
///   两版本都有的参数，0.113 多出来的 source 不声明即可（踩坑记录坑25 同款手法）。
///
/// 触发条件与原 override 一致：wasRemovalPrevented == false（真正死亡分支）。
/// TriggerDeadState() 内部只有同步的 SetMoveImmediate，因此可以安全地同步等待。
/// </summary>
[HarmonyPatch]
public static class ReviveDeathPatches
{
    [HarmonyPatch(typeof(Hook), "AfterDeath")]
    [HarmonyPostfix]
    private static void Postfix(Creature? creature, bool wasRemovalPrevented)
    {
        if (creature == null || wasRemovalPrevented) return;
        try
        {
            // 小僵尸鸡骑士：第一条命死亡 → 进入浴血重生招式
            JockeyRevivePower? jockeyPower = creature.GetPower<JockeyRevivePower>();
            if (jockeyPower != null && creature.Monster is ChickenJockey jockey)
            {
                jockeyPower.NotifyOwnerDeath();
                jockey.TriggerDeadState().GetAwaiter().GetResult();
                Log.Info("[MCMonsters] Jockey revive triggered.");
                return;
            }

            // 末影龙：前两形态死亡 → 进入重生招式（第三形态后复活力量被移除，不再触发）
            EnderDragonRevivePower? dragonPower = creature.GetPower<EnderDragonRevivePower>();
            if (dragonPower != null && creature.Monster is EnderDragon dragon)
            {
                dragonPower.NotifyOwnerDeath();
                dragon.TriggerDeadState().GetAwaiter().GetResult();
                Log.Info("[MCMonsters] EnderDragon revive triggered.");
            }
        }
        catch (Exception ex)
        {
            // 复活触发失败绝不能崩死战斗管线（最坏情况：BOSS 正常死亡、战斗提前结束）
            Log.Error($"[MCMonsters] ReviveDeathPatches failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}
