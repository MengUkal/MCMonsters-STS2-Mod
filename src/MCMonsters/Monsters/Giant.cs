using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：巨人（第3幕精英）
/// 被动【巨力预警】：抬脚蓄力的回合完全不攻击，但下一回合的践踏是致命的。
/// 招式循环：抬脚蓄力（不攻击+格挡12）→ 践踏（35）→ 横扫（2×14）。
/// 数值：300血（进阶315）。
/// </summary>
public sealed class Giant : MonsterModel
{
    // MC巨人100HP纯代码怪 → STS2第3幕精英 300
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 315, 300);
    public override int MaxInitialHp => MinInitialHp;

    private int LiftBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 12);
    private int StompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 38, 35);
    private int SweepDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 14);
    private int SweepHits => 2;

    // AI循环：抬脚蓄力 → 践踏 → 横扫 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState lift = new MoveState("LIFT_FOOT", LiftMove, new DefendIntent());
        MoveState stomp = new MoveState("STOMP", StompMove, new SingleAttackIntent(StompDamage));
        MoveState sweep = new MoveState("SWEEP", SweepMove, new MultiAttackIntent(SweepDamage, SweepHits));

        lift.FollowUpState = stomp;
        stomp.FollowUpState = sweep;
        sweep.FollowUpState = lift;

        states.Add(lift);
        states.Add(stomp);
        states.Add(sweep);

        return new MonsterMoveStateMachine(states, lift);
    }

    // 抬脚蓄力：不攻击，自身格挡12（给玩家一回合准备）
    private async Task LiftMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await CreatureCmd.GainBlock(base.Creature, LiftBlock, ValueProp.Unpowered, null);
    }

    // 践踏：35致命重击
    private async Task StompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StompDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.4f)
            .WithAttackerFx(null, null, "mc_giant_attack.ogg")
            .WithHitFx("vfx/vfx_rock_shatter")
            .Execute(null);
        global::MegaCrit.Sts2.Core.Nodes.NGame.Instance?.ScreenShake(
            global::MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.ShakeStrength.Strong,
            global::MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.ShakeDuration.Normal);
    }

    // 横扫：2×14
    private async Task SweepMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SweepDamage).WithHitCount(SweepHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_giant_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
