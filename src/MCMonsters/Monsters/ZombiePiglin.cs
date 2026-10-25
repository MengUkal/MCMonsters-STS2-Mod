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
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：僵尸猪灵（第2幕普通怪）——中立生物还原
/// 原版行为映射：没被打时是"中立"的（每回合蓄怒+2力量）；
/// 一旦受到攻击立刻被激怒，切换为凶猛的攻击循环。
/// 招式：
///   平静（未受伤）：蓄怒（力量+2，无限循环直到被打）
///   被激怒循环：挥砍18 → 撕咬6+易伤2 → 猛击13 → 循环
/// 数值：96血（进阶100），攻击对标第2幕偏上（猪灵蛮兵16/8×8）。
/// 意图即时切换：GrudgePower的AfterDamageGiven钩子触发SetMoveImmediate。
/// </summary>
public sealed class ZombiePiglin : MonsterModel
{
    private MoveState? _attackState;

    private bool _damaged;

    // MC僵尸猪灵20HP+中立 → STS2第2幕 96（进阶100）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 96);
    public override int MaxInitialHp => MinInitialHp;

    private int CalmStrength => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 3, 2);
    private int CleaveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 18);
    private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int BiteVulnerable => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
    private int SlamDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 13);

    private bool Damaged
    {
        get => _damaged;
        set
        {
            AssertMutable();
            _damaged = value;
        }
    }

    private MoveState AttackState
    {
        get => _attackState!;
        set
        {
            AssertMutable();
            _attackState = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<ZombiePiglinGrudgePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 由 ZombiePiglinGrudgePower 在受伤时调用：立刻切换意图为攻击循环
    public async Task SwitchToAttack()
    {
        if (!Damaged)
        {
            Damaged = true;
            SetMoveImmediate(AttackState, forceTransition: true);
        }
        await Task.CompletedTask;
    }

    public void MarkDamaged()
    {
        Damaged = true;
    }

    // AI状态机：平静蓄怒循环（未受伤）→ 攻击循环（被激怒后永久）
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState calm = new MoveState("CALM", McGuard.Move("CALM", CalmMove), new BuffIntent());
        MoveState cleave = new MoveState("CLEAVE", McGuard.Move("CLEAVE", CleaveMove), new SingleAttackIntent(CleaveDamage));
        MoveState bite = new MoveState("BITE", McGuard.Move("BITE", BiteMove), new SingleAttackIntent(BiteDamage), new DebuffIntent());
        MoveState slam = new MoveState("SLAM", McGuard.Move("SLAM", SlamMove), new SingleAttackIntent(SlamDamage));
        AttackState = cleave;

        // 平静分支：未受伤回到蓄怒，受伤进入攻击循环
        ConditionalBranchState calmBranch = new ConditionalBranchState("CALM_BRANCH");
        calm.FollowUpState = calmBranch;
        calmBranch.AddState(calm, () => !Damaged);
        calmBranch.AddState(cleave, () => Damaged);

        // 攻击循环：挥砍 → 撕咬 → 猛击 → 循环
        cleave.FollowUpState = bite;
        bite.FollowUpState = slam;
        slam.FollowUpState = cleave;

        states.Add(calm);
        states.Add(calmBranch);
        states.Add(cleave);
        states.Add(bite);
        states.Add(slam);

        return new MonsterMoveStateMachine(states, calm);
    }

    // 蓄怒：中立状态下每回合力量+2
    private async Task CalmMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_zombiepiglin_angry.ogg");
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, CalmStrength, Creature, null);
    }

    // 挥砍：金剑重劈（18）
    private async Task CleaveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CleaveDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, null, "mc_zombiepiglin_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 撕咬：撕咬6+脆弱2（脆弱：获得的格挡减少25%）
    private async Task BiteMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BiteDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_zombiepiglin_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(), targets, BiteVulnerable, Creature, null);
    }

    // 猛击：猛击13
    private async Task SlamMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlamDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, null, "mc_zombiepiglin_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
