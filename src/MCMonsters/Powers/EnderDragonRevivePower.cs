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

    /// <summary>
    /// 由 ReviveDeathPatches（Hook.AfterDeath 后置补丁）在主人死亡时调用。
    /// 不直接 override AbstractModel.AfterDeath：该虚方法在 0.107/0.111 是 4 参、
    /// 0.113 新增 CardModel? source 变 5 参，override 会让一份 DLL 无法同时兼容两个分支。
    /// </summary>
    public void NotifyOwnerDeath()
    {
        GetInternalData<Data>().isReviving = true;
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
