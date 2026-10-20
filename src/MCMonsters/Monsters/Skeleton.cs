using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：骷髅
/// 设计：每回合固定一箭6点（意图直观），配合专属力量"精准箭矢"——
/// 未被格挡的部分造成双倍伤害（挡不住就掉双倍血，逼玩家认真叠格挡）。
/// 注意：TriggerAnim/WithAttackerAnim 传的是 Animator 触发器名（"Cast"/"Attack"，帕斯卡命名）。
/// </summary>
public sealed class Skeleton : MonsterModel
{
    // 数值（第1幕基准36血/每回合攻击6；高进阶血+6、伤害+1）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 36);
    public override int MaxInitialHp => MinInitialHp;

    private int VolleyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);

    // 精准箭矢：未被格挡部分造成双倍伤害
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<SkeletonPrecisionPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // AI循环：每回合固定一箭
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        MoveState volley = new MoveState("VOLLEY", VolleyMove, new SingleAttackIntent(VolleyDamage));
        volley.FollowUpState = volley;
        return new MonsterMoveStateMachine(new List<MonsterState> { volley }, volley);
    }

    // 射箭：单发6点，意图清晰
    private async Task VolleyMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(VolleyDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_skeleton_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
