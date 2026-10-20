using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Powers;

/// <summary>
/// MC凋零效果（可叠加Debuff）：每回合开始时失去等同于层数的生命，层数不衰减。
/// 参考官方PoisonPower的AfterSideTurnStart钩子，但不递减层数（忠实还原MC凋零）。
/// 文本走monsters表（powers表的mod合并查询不稳定）。
/// </summary>
public sealed class McWitherPower : PowerModel
{
    // 文本放monsters表（mod键实测可用），不走合并行为不明的powers表
    public override LocString Title => new LocString("monsters", "MCWITHER.title");

    public override LocString Description => new LocString("monsters", "MCWITHER.description");

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(base.Owner))
        {
            return;
        }
        if (base.Amount > 0)
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), base.Owner, base.Amount, ValueProp.Unblockable | ValueProp.Unpowered, null, null);
        }
    }
}
