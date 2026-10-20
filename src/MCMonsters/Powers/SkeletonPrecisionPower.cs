using System;
using System.Threading.Tasks;
using Godot;
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
/// 骷髅专属力量"精准箭矢"：骷髅的攻击对玩家造成未格挡伤害时，
/// 追加一次等量的直接HP损耗——等效于"未被格挡部分造成双倍伤害"。
/// 参考官方 PaperCutsPower 的 AfterDamageGiven 钩子模式。
/// 图标：images/atlases/power_atlas.sprites/skeleton_precision_power.tres
/// 文本：实测 powers 表的mod合并查询不稳定，改用已验证可用的 monsters 表承载文本。
/// </summary>
public sealed class SkeletonPrecisionPower : PowerModel
{
    // 文本放monsters表（mod键实测可用），不走合并行为不明的powers表
    public override LocString Title => new LocString("monsters", "SKELETON_PRECISION.title");

    public override LocString Description => new LocString("monsters", "SKELETON_PRECISION.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer == base.Owner && target.IsPlayer && props.IsPoweredAttack() && result.UnblockedDamage > 0)
        {
            // 未被格挡的部分再扣一次（封顶当前HP，避免负数；正常击杀由原伤害负责）
            int extra = (int)Math.Min(result.UnblockedDamage, target.CurrentHp);
            if (extra > 0)
            {
                await CreatureCmd.SetCurrentHp(target, target.CurrentHp - extra);
            }
        }
    }
}
