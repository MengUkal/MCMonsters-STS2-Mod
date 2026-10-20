using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 末影龙的"天空之翼"（可见，第三形态）：飞行状态受到的所有攻击伤害减半。
/// 文本走monsters表。
/// </summary>
public sealed class DragonFlightPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "DRAGON_FLIGHT.title");

    public override LocString Description => new LocString("monsters", "DRAGON_FLIGHT.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target == base.Owner && props.IsPoweredAttack())
        {
            return 0.5m;
        }
        return 1m;
    }
}
