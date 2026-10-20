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
/// 恶魂的"完美格挡反击"（可见，教玩家机制）：恶魂的攻击被玩家足额格挡
/// （未格挡伤害为0且格挡吸收大于0，即伤害与格挡值相同或格挡足额）时，
/// 恶魂自己受到1点伤害（直接SetCurrentHp结算，绕过悬浮庇护的免伤）。
/// 恶魂3血：三次完美格挡即可击败。
/// </summary>
public sealed class GhastPerfectBlockPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "GHAST_PERFECT.title");

    public override LocString Description => new LocString("monsters", "GHAST_PERFECT.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        // 主人（恶魂）的攻击被玩家足额格挡 → 反噬1点
        if (dealer == base.Owner && target.IsPlayer && props.IsPoweredAttack() && result.UnblockedDamage == 0 && result.BlockedDamage > 0)
        {
            var ghast = base.Owner;
            if (ghast.CurrentHp > 0)
            {
                await CreatureCmd.SetCurrentHp(ghast, ghast.CurrentHp - 1);
                if (ghast.CurrentHp <= 0)
                {
                    // 直接扣至0不触发死亡结算，手动击杀
                    await CreatureCmd.Kill(ghast);
                }
            }
        }
    }
}
