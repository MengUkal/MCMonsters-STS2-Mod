using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MCMonsters.Powers;

/// <summary>
/// 末影龙的"天空之翼"（可见，第三形态）：飞行状态受到的所有攻击伤害减半。
///
/// 注意：伤害修正不再用 ModifyDamageMultiplicative 重写——该虚方法在
/// 0.107.x（5 参数）和 0.111.x（6 参数，多了 CardPlay）签名不同，一份 DLL
/// 无法同时 override 两个版本。因此统一挪到 Patches/DamageModifierPatches.cs，
/// 用两个版本签名完全一致的 Hook.ModifyDamage 后置补丁实现。
/// 文本走monsters表。
/// </summary>
public sealed class DragonFlightPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "DRAGON_FLIGHT.title");

    public override LocString Description => new LocString("monsters", "DRAGON_FLIGHT.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
