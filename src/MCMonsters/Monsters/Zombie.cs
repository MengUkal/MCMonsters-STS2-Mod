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

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：僵尸
/// 经典循环：爪击 → 死灵低吼（力量+2）→ 撕咬（重击），然后重复。
/// 数值对标第1幕普通怪（参考 Cultist 系：50血左右、攻击7-12）。
/// </summary>
public sealed class Zombie : MonsterModel
{
    // ===== 生命值（第1幕基准48；高进阶+6）=====
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 54, 48);
    public override int MaxInitialHp => MinInitialHp;

    // ===== 招式数值（攻击8；高进阶+1）=====
    private int ClawSwipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int ChompDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int GrowlStrength => 2;

    // ===== AI 状态机：爪击 → 低吼 → 撕咬 循环 =====
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState clawSwipe = new MoveState("CLAW_SWIPE", ClawSwipeMove, new SingleAttackIntent(ClawSwipeDamage));
        MoveState deadGrowl = new MoveState("DEAD_GROWL", DeadGrowlMove, new BuffIntent());
        MoveState chomp = new MoveState("CHOMP", ChompMove, new SingleAttackIntent(ChompDamage));

        clawSwipe.FollowUpState = deadGrowl;
        deadGrowl.FollowUpState = chomp;
        chomp.FollowUpState = clawSwipe;

        states.Add(clawSwipe);
        states.Add(deadGrowl);
        states.Add(chomp);

        return new MonsterMoveStateMachine(states, clawSwipe);
    }

    // ===== 招式实现 =====

    private async Task ClawSwipeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ClawSwipeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_zombie_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DeadGrowlMove(IReadOnlyList<Creature> targets)
    {
        // 死灵低吼：给自己上力量
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, GrowlStrength, Creature, null);
    }

    private async Task ChompMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ChompDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_zombie_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}
