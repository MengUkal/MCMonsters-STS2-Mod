using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MCMonsters.Powers;

/// <summary>
/// 潜影贝的"壳甲"状态（可见）：闭壳期间受到的所有攻击伤害减半。
/// 闭壳招式应用，下一次开壳弹幕时移除。
///
/// 伤害修正统一实现在 Patches/DamageModifierPatches.cs（版本无关的 Hook 补丁，
/// 因为 ModifyDamageMultiplicative 在 0.107.x / 0.111.x 签名不同）。
/// 文本走monsters表。
/// </summary>
public sealed class ShulkerShellPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "SHULKER_SHELL.title");

    public override LocString Description => new LocString("monsters", "SHULKER_SHELL.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
