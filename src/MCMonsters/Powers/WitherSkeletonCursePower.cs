using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 凋零骷髅的"凋零之触"被动（可见）：攻击未被格挡时给予玩家1层凋零。
///
/// 关键点：必须复用钩子传入的 choiceContext，不能用 new ThrowingPlayerChoiceContext()。
/// 那个 context 的 SignalPlayerChoiceBegun 是直接 throw NotImplementedException 的，
/// 一旦 Apply/Power 钩子链里有任何环节要请求玩家选择，异常会打断这次攻击的伤害管线
/// —— 表现就是"牌浮在屏幕上、回合永远不结束"。官方同类力量（EnvenomPower 等）同样复用传入 context。
/// 文本走monsters表。
/// </summary>
public sealed class WitherSkeletonCursePower : PowerModel
{
    // 文本放monsters表（mod键实测可用），不走合并行为不明的powers表
    public override LocString Title => new LocString("monsters", "WITHER_TOUCH.title");

    public override LocString Description => new LocString("monsters", "WITHER_TOUCH.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 主人（凋零骷髅）的攻击未被格挡 → 给玩家叠1层凋零
        if (dealer != base.Owner || !target.IsPlayer || !props.IsPoweredAttack() || result.UnblockedDamage <= 0)
        {
            return;
        }

        try
        {
            await PowerCmd.Apply<McWitherPower>(choiceContext, target, 1, base.Owner, null);
        }
        catch (Exception ex)
        {
            // 绝不能让异常穿透到伤害管线把怪物动作卡死
            Log.Warn($"[MCMonsters] WitherSkeletonCursePower apply failed: {ex}");
        }
    }
}
