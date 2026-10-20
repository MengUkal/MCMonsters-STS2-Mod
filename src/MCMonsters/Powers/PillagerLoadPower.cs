using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Monsters;

namespace MCMonsters.Powers;

/// <summary>
/// 掠夺者的"装填完毕"标记（可见）：
/// 装填后，若在下一回合累计受到12点以上伤害，弩箭被击落——攻击招式取消，变回装填。
/// 文本走monsters表。
/// </summary>
public sealed class PillagerLoadPower : PowerModel
{
    private class Data
    {
        public int accumulated;
    }

    public override LocString Title => new LocString("monsters", "PILLAGER_LOAD.title");

    public override LocString Description => new LocString("monsters", "PILLAGER_LOAD.description");

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
        D.accumulated += (int)result.TotalDamage;
        // 12点以上伤害：弩箭被击落，攻击取消变回装填
        if (D.accumulated >= 12 && base.Owner.Monster is Pillager pillager)
        {
            await pillager.CancelLoadedShot();
        }
    }
}
