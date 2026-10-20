using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 恶魂的"悬浮庇护"（可见）：恶魂漂浮在高空，无法受到任何伤害。
/// ModifyDamageMultiplicative将所有指向恶魂的伤害归零（含攻击/灼烧/中毒等HP流失）。
/// 完美格挡的自伤由SetCurrentHp直接结算（不经过伤害管线，不受此免疫影响）。
/// 文本走monsters表。
/// </summary>
public sealed class GhastIntangiblePower : PowerModel
{
    public override LocString Title => new LocString("monsters", "GHAST_INTANGIBLE.title");

    public override LocString Description => new LocString("monsters", "GHAST_INTANGIBLE.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 恶魂本体受到的一切伤害归零
        if (target == base.Owner)
        {
            return 0m;
        }
        return 1m;
    }
}
