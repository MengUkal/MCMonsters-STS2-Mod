using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 末影人的"空间闪避"被动（可见）：
/// 每次受到攻击后立刻瞬移，获得5点格挡；每回合最多触发2次（惩罚多段攻击）。
/// 文本走monsters表。
/// </summary>
public sealed class EndermanDodgePower : PowerModel
{
    private class Data
    {
        public int triggeredThisTurn;
    }

    public override LocString Title => new LocString("monsters", "ENDERMAN_DODGE.title");

    public override LocString Description => new LocString("monsters", "ENDERMAN_DODGE.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private Data D => GetInternalData<Data>();

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target != base.Owner || !props.IsPoweredAttack() || result.TotalDamage <= 0)
        {
            return;
        }
        if (D.triggeredThisTurn >= 2)
        {
            return;
        }
        D.triggeredThisTurn++;
        await CreatureCmd.GainBlock(base.Owner, 5, ValueProp.Unpowered, null);
    }

    // 每回合重置计数（恶魂一方回合开始=玩家回合结束）
    public override async Task AfterSideTurnStart(global::MegaCrit.Sts2.Core.Combat.CombatSide side, System.Collections.Generic.IReadOnlyList<Creature> participants, global::MegaCrit.Sts2.Core.Combat.ICombatState combatState)
    {
        if (participants.Contains(base.Owner))
        {
            D.triggeredThisTurn = 0;
        }
    }
}
