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

namespace MCMonsters.Powers;

/// <summary>
/// 末影人的"虚空侵蚀"被动（可见）：
/// 每当敌方回合开始（即玩家回合结束时），玩家失去1点敏捷（空间被扭曲，身法被侵蚀）。
/// 文本走monsters表。
/// </summary>
public sealed class EndermanErosionPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "ENDERMAN_EROSION.title");

    public override LocString Description => new LocString("monsters", "ENDERMAN_EROSION.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnStart(CombatSide side, System.Collections.Generic.IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != CombatSide.Enemy || !participants.Contains(base.Owner))
        {
            return;
        }
        foreach (Creature player in combatState.PlayerCreatures)
        {
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(), player, -1, base.Owner, null);
        }
    }
}
