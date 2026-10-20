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
/// 凋零骷髅的"凋零之触"被动（可见）：攻击未被格挡时给予玩家1层凋零。
/// 参考SkeletonPrecisionPower的AfterDamageGiven钩子模式。
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
        if (dealer == base.Owner && target.IsPlayer && props.IsPoweredAttack() && result.UnblockedDamage > 0)
        {
            await PowerCmd.Apply<McWitherPower>(new ThrowingPlayerChoiceContext(), target, 1, base.Owner, null);
        }
    }
}
