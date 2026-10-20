using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MCMonsters.Monsters;

namespace MCMonsters.Powers;

/// <summary>
/// 监守者的"警觉"被动（可见）：
/// 玩家本回合未打出任何牌便结束回合时，监守者的意图立刻变为【蓄力】（力量+2）。
/// 文本走monsters表。
/// </summary>
public sealed class WardenPunishPower : PowerModel
{
    public override LocString Title => new LocString("monsters", "WARDEN_PUNISH.title");

    public override LocString Description => new LocString("monsters", "WARDEN_PUNISH.description");

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player)
        {
            return;
        }
        if (base.Owner.Monster is not Warden warden)
        {
            return;
        }
        var playerCreatures = base.Owner.CombatState.GetOpponentsOf(base.Owner).Where(c => c.IsPlayer).ToList();
        if (playerCreatures.Count == 0)
        {
            return;
        }
        // 本回合玩家是否有打出过牌（自动打出的牌不算）
        bool anyPlayed = global::MegaCrit.Sts2.Core.Combat.CombatManager.Instance.History.CardPlaysFinished.Any(e =>
            playerCreatures.Contains(e.Actor) && e.HappenedThisTurn(base.Owner.CombatState) && !e.CardPlay.IsAutoPlay);
        if (!anyPlayed)
        {
            await warden.PunishIdleTurn();
        }
    }
}
