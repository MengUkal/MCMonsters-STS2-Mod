using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Monsters;

namespace MCMonsters.Powers;

/// <summary>
/// 僵尸猪灵的"激怒"力量：受到攻击后立刻标记受伤状态并切换意图为攻击循环。
/// 文本走monsters表（powers表的mod合并查询不稳定）。
/// </summary>
public sealed class ZombiePiglinGrudgePower : PowerModel
{
    // 文本放monsters表（mod键实测可用），不走合并行为不明的powers表
    public override LocString Title => new LocString("monsters", "ZOMBIE_PIGLIN_GRUDGE.title");

    public override LocString Description => new LocString("monsters", "ZOMBIE_PIGLIN_GRUDGE.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 主人（僵尸猪灵）受到任意伤害 → 标记激怒并立刻切换意图为攻击循环
        if (target == base.Owner && result.TotalDamage > 0 && base.Owner.Monster is ZombiePiglin piglin)
        {
            piglin.MarkDamaged();
            await piglin.SwitchToAttack();
        }
    }
}
