using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 女巫的"炼金术"被动（可见）+ 治疗受阻追踪：
/// 1) 玩家单次对她造成15点以上伤害时，向其弃牌堆塞1张灼伤（药水溅射）；
/// 2) 记录本回合她受到的伤害总量，供"饮用治疗"判断是否被打断（20+伤害打断）。
/// 文本走monsters表。
/// </summary>
public sealed class WitchBrewPower : PowerModel
{
    private class Data
    {
        public int damageThisTurn;
        public int lastTurnDamage;
    }

    public override LocString Title => new LocString("monsters", "WITCH_BREW.title");

    public override LocString Description => new LocString("monsters", "WITCH_BREW.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private Data D => GetInternalData<Data>();

    protected override object InitInternalData()
    {
        return new Data();
    }

    // 供饮用治疗招式查询：上一回合（玩家回合）受到的总伤害
    public int LastTurnDamage => D.lastTurnDamage;

    public void ResetDamageTracking()
    {
        D.damageThisTurn = 0;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target != base.Owner || !props.IsPoweredAttack() || result.TotalDamage <= 0)
        {
            return;
        }
        D.damageThisTurn += (int)result.TotalDamage;
        // 单次15点以上：药水溅射 → 玩家弃牌堆塞1张灼伤
        if (result.TotalDamage >= 15 && dealer != null && dealer.IsPlayer)
        {
            await CardPileCmd.AddToCombatAndPreview<Burn>(new[] { dealer }, PileType.Discard, 1, null);
        }
    }

    // 每回合结束重置伤害追踪
    public override async Task AfterSideTurnStart(CombatSide side, System.Collections.Generic.IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(base.Owner))
        {
            D.lastTurnDamage = D.damageThisTurn;
            D.damageThisTurn = 0;
        }
    }
}
