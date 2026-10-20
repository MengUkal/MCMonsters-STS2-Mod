using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 潜影贝的"壳甲"状态（可见）：闭壳期间受到的所有攻击伤害减半。
/// 闭壳招式应用，下一次开壳弹幕时移除。
/// 文本走monsters表。
/// </summary>
public sealed class ShulkerShellPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "SHULKER_SHELL.title");

    public override LocString Description => new LocString("monsters", "SHULKER_SHELL.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 壳甲期间受到的攻击伤害减半
        if (target == base.Owner && props.IsPoweredAttack())
        {
            return 0.5m;
        }
        return 1m;
    }
}
