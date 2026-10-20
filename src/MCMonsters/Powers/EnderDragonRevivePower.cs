using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MCMonsters.Monsters;

namespace MCMonsters.Powers;

/// <summary>
/// 末影龙BOSS的"龙之不死"力量（复刻官方 AdaptablePower/实验体模式）：
/// 前两次死亡：阻止移除，强制进入复活招式（形态切换）
/// 第二次复活后由BOSS本体移除此力量，第三次死亡即为真死。
/// 文本走monsters表（powers表的mod合并查询不稳定）。
/// </summary>
public sealed class EnderDragonRevivePower : PowerModel
{
    // 文本放monsters表（mod键实测可用），不走合并行为不明的powers表
    public override LocString Title => new LocString("monsters", "ENDER_DRAGON_REVIVE.title");

    public override LocString Description => new LocString("monsters", "ENDER_DRAGON_REVIVE.description");

    private class Data
    {
        public bool isReviving;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    private bool IsReviving => GetInternalData<Data>().isReviving;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public void DoRevive()
    {
        GetInternalData<Data>().isReviving = false;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (!wasRemovalPrevented && creature == base.Owner && creature.Monster is EnderDragon dragon)
        {
            GetInternalData<Data>().isReviving = true;
            await dragon.TriggerDeadState();
        }
    }

    public override bool ShouldAllowHitting(Creature creature)
    {
        if (creature != base.Owner)
        {
            return true;
        }
        return !IsReviving;
    }

    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        if (creature != base.Owner)
        {
            return true;
        }
        return false;
    }

    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return false;
    }
}
