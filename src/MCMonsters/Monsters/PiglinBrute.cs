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
using MegaCrit.Sts2.Core.ValueProps;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：猪灵蛮兵（第2幕普通怪，小型精英定位）
/// 原版行为映射：永不撤退、永不饶恕——金斧重劈，站在原地和你硬碰硬。
/// 招式循环：金斧劈砍 → 蛮兵横扫 → 蛮兵不退（格挡）。
/// 数值：第2幕最肉普通怪（74-80血），单发伤害也最高，正面硬换血不亏，建议绕着打或上虚弱。
/// </summary>
public sealed class PiglinBrute : MonsterModel
{
    // 猪灵蛮兵无甲但血厚 → STS2第2幕 74-80（最肉普通怪）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 78, 74);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 84, 80);

    private int CleaveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);
    private int SweepDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int SweepHits => 2;
    private int UnyieldingBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 12);

    // AI循环：金斧劈砍 → 蛮兵横扫 → 蛮兵不退 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState cleave = new MoveState("AXE_CLEAVE", AxeCleaveMove, new SingleAttackIntent(CleaveDamage));
        MoveState sweep = new MoveState("BRUTE_SWEEP", BruteSweepMove, new MultiAttackIntent(SweepDamage, SweepHits));
        MoveState unyielding = new MoveState("UNYIELDING", UnyieldingMove, new DefendIntent());

        cleave.FollowUpState = sweep;
        sweep.FollowUpState = unyielding;
        unyielding.FollowUpState = cleave;

        states.Add(cleave);
        states.Add(sweep);
        states.Add(unyielding);

        return new MonsterMoveStateMachine(states, cleave);
    }

    // 金斧劈砍：双手举金斧重劈（单发大伤害）
    private async Task AxeCleaveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CleaveDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_brute_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // 蛮兵横扫：金斧横扫两段
    private async Task BruteSweepMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SweepDamage).WithHitCount(SweepHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_brute_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 蛮兵不退：原地架斧格挡（永不撤退）
    private async Task UnyieldingMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await CreatureCmd.GainBlock(base.Creature, UnyieldingBlock, ValueProp.Unpowered, null);
    }
}
