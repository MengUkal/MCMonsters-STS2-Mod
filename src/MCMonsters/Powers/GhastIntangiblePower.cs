using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MCMonsters.Powers;

/// <summary>
/// 恶魂的"悬浮庇护"（可见）：恶魂漂浮在高空，无法受到任何伤害。
///
/// 伤害归零统一实现在 Patches/DamageModifierPatches.cs（版本无关的 Hook 补丁，
/// 因为 ModifyDamageMultiplicative 在 0.107.x / 0.111.x 签名不同）。
/// 完美格挡的自伤由 SetCurrentHp 直接结算（不经过伤害管线，不受此免疫影响）。
/// 文本走monsters表。
/// </summary>
public sealed class GhastIntangiblePower : PowerModel
{
    public override LocString Title => new LocString("monsters", "GHAST_INTANGIBLE.title");

    public override LocString Description => new LocString("monsters", "GHAST_INTANGIBLE.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;
}
