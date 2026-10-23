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
/// 每次受到攻击后立刻瞬移，获得4点格挡；无限触发（惩罚多段攻击）。
/// 文本走monsters表。
/// </summary>
public sealed class EndermanDodgePower : PowerModel
{
    public override LocString Title => new LocString("monsters", "ENDERMAN_DODGE.title");

    public override LocString Description => new LocString("monsters", "ENDERMAN_DODGE.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target != base.Owner || !props.IsPoweredAttack() || result.TotalDamage <= 0)
        {
            return;
        }
        await CreatureCmd.GainBlock(base.Owner, 4, ValueProp.Unpowered, null);
    }
}
