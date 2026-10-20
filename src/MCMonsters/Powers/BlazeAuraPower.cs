using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MCMonsters.Powers;

/// <summary>
/// 烈焰人的"烈焰之躯"被动（可见展示）：烈焰人攻击时玩家失去2点生命。
/// 纯展示力量——2点生命流失由各攻击招式的代码直接实现（每次攻击动作结算一次）。
/// 文本走monsters表。
/// </summary>
public sealed class BlazeAuraPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "BLAZE_AURA.title");

    public override LocString Description => new LocString("monsters", "BLAZE_AURA.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
