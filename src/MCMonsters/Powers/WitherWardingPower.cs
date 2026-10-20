using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// 凋灵的"幽冥护壁"被动（可见）：
/// 凋灵生命降至一半以下时，立刻获得99层人工制品（免疫一切状态效果）。
/// 文本走monsters表。
/// </summary>
public sealed class WitherWardingPower : PowerModel
{
    private class Data
    {
        public bool granted;
    }

    public override LocString Title => new LocString("monsters", "WITHER_WARDING.title");

    public override LocString Description => new LocString("monsters", "WITHER_WARDING.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private Data D => GetInternalData<Data>();

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 凋灵自身受伤后：生命降至一半以下且尚未触发 → 获得99层人工制品
        if (target != base.Owner || D.granted)
        {
            return;
        }
        if (base.Owner.CurrentHp * 2 <= base.Owner.MaxHp)
        {
            D.granted = true;
            await PowerCmd.Apply<ArtifactPower>(new ThrowingPlayerChoiceContext(), base.Owner, 99, base.Owner, null);
        }
    }
}
