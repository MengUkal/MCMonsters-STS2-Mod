using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Powers;

namespace MCMonsters.Patches;

/// <summary>
/// 版本无关的伤害修正补丁。
///
/// 背景：0.107.x 的 AbstractModel.ModifyDamageMultiplicative 是 5 个参数，
/// 0.111.x 变成 6 个参数（末尾多了 CardPlay）。用 override 实现的话，一份 DLL
/// 只能匹配其中一个版本（另一个版本加载这些力量类型时会 TypeLoadException）。
///
/// 而 Hook.ModifyDamage 在两个版本里都存在、且参数名一致（0.111 只是多了
/// CardPlay 一个参数，补丁里不声明它即可，Harmony 按名字注入不影响）。所以改为
/// 在伤害管线总入口做后置补丁，按"受击者身上有没有这些力量"来修正最终伤害：
///   · 恶魂【悬浮庇护】：恶魂受到的一切伤害归零
///   · 潜影贝【壳甲】：受到的攻击伤害减半
///   · 末影龙【天空之翼】：受到的攻击伤害减半
///
/// 注意：补丁方法的参数必须"按名字"声明（target/props/modifyDamageHookType），
/// 不能用 object[] __args —— 该方法带 out 参数，用 __args 会让 Harmony 在部分
/// 版本下丢失 out 值，导致 AfterModifyingDamageAmount 拿到 null 崩溃。
/// </summary>
[HarmonyPatch]
public static class DamageModifierPatches
{
    [HarmonyPatch(typeof(Hook), "ModifyDamage")]
    [HarmonyPostfix]
    private static void Postfix(
        Creature? target,
        ValueProp props,
        ModifyDamageHookType modifyDamageHookType,
        ref decimal __result)
    {
        try
        {
            if (target == null) return;

            // 只在"乘法修正"回合生效，和原 override 的调用时机保持一致
            if (!modifyDamageHookType.HasFlag(ModifyDamageHookType.Multiplicative)) return;

            // 恶魂：悬浮庇护，任何伤害都归零（不管来源/类型）
            if (target.GetPower<GhastIntangiblePower>() != null)
            {
                __result = 0m;
                return;
            }

            // 下面两个只对"被强化的攻击伤害"生效
            if (!props.IsPoweredAttack()) return;

            if (target.GetPower<ShulkerShellPower>() != null)
            {
                __result *= 0.5m;
                return;
            }

            if (target.GetPower<DragonFlightPower>() != null)
            {
                __result *= 0.5m;
            }
        }
        catch
        {
            // 伤害补丁绝不能让游戏崩溃；出错就当没生效
        }
    }
}
