using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：末影龙（第3幕BOSS，三形态600总血，复刻官方实验体的多形态结构）
/// 原版行为映射：盘旋吐龙息 → 俯冲冲撞 → 栖息狂怒，末影龙的三段战斗。
/// 形态1（200）盘旋形态：龙息吐息（16+灼伤2）→ 翼击（2×10）
/// 形态2（200）俯冲形态：俯冲冲撞（24）→ 龙息云（10+灼伤3）
/// 形态3（200）狂怒形态：龙怒咆哮（力量3+格挡20）→ 三重俯冲（3×12）→ 死亡凝视（22）
/// 每次死亡后强制进复活招式切换形态；第三次死亡移除复活力量，即为真死。
/// 战斗场景槽位抬高（末影龙飞行在空中）。
/// </summary>
public sealed class EnderDragon : MonsterModel
{
    private MoveState? _deadState;

    private int _respawns;

    // 三形态血量（总600，对标第3幕BOSS TestSubjectBoss 600）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 212, 200);
    public override int MaxInitialHp => MinInitialHp;

    public int SecondFormHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 212, 200);

    public int ThirdFormHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 212, 200);

    private int BreathDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 16);
    private int BreathBurn => 2;
    private int GustDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int GustHits => 2;
    private int DiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 26, 24);
    private int CloudDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int CloudBurn => 3;
    private int RoarStrength => 3;
    private int RoarBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 22, 20);
    private int TripleDiveDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);
    private int TripleDiveHits => 3;
    private int StareDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 22);

    private MoveState DeadState
    {
        get => _deadState!;
        set
        {
            AssertMutable();
            _deadState = value;
        }
    }

    private int Respawns
    {
        get => _respawns;
        set
        {
            AssertMutable();
            _respawns = value;
        }
    }

    public override bool ShouldFadeAfterDeath => false;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Armor;

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<EnderDragonRevivePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 由 EnderDragonRevivePower.AfterDeath 在形态死亡时调用
    public async Task TriggerDeadState()
    {
        SetMoveImmediate(DeadState, forceTransition: true);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        DeadState = new MoveState("RESPAWN_MOVE", RespawnMove, new HealIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };

        // 形态1：盘旋形态
        MoveState breath = new MoveState("DRAGON_BREATH", DragonBreathMove, new SingleAttackIntent(BreathDamage), new DebuffIntent());
        MoveState gust = new MoveState("WING_GUST", WingGustMove, new MultiAttackIntent(GustDamage, GustHits));

        // 形态2：俯冲形态
        MoveState dive = new MoveState("DIVE_CHARGE", DiveChargeMove, new SingleAttackIntent(DiveDamage));
        MoveState cloud = new MoveState("DRAGON_CLOUD", DragonCloudMove, new SingleAttackIntent(CloudDamage), new DebuffIntent());

        // 形态3：狂怒形态
        MoveState roar = new MoveState("RAGE_ROAR", RageRoarMove, new BuffIntent(), new DefendIntent());
        MoveState tripleDive = new MoveState("TRIPLE_DIVE", TripleDiveMove, new MultiAttackIntent(TripleDiveDamage, TripleDiveHits));
        MoveState stare = new MoveState("DEATH_STARE", DeathStareMove, new SingleAttackIntent(StareDamage));

        // 形态1循环：龙息吐息 → 翼击 → 循环
        breath.FollowUpState = gust;
        gust.FollowUpState = breath;

        // 形态2循环：俯冲冲撞 → 龙息云 → 循环
        dive.FollowUpState = cloud;
        cloud.FollowUpState = dive;

        // 形态3循环：咆哮 → 三重俯冲 → 死亡凝视 → 循环
        roar.FollowUpState = tripleDive;
        tripleDive.FollowUpState = stare;
        stare.FollowUpState = roar;

        // 死亡后按复活次数切换形态分支
        ConditionalBranchState reviveBranch = new ConditionalBranchState("REVIVE_BRANCH");
        DeadState.FollowUpState = reviveBranch;
        reviveBranch.AddState(dive, () => Respawns < 2);
        reviveBranch.AddState(roar, () => Respawns >= 2);

        states.Add(DeadState);
        states.Add(breath);
        states.Add(gust);
        states.Add(dive);
        states.Add(cloud);
        states.Add(roar);
        states.Add(tripleDive);
        states.Add(stare);
        states.Add(reviveBranch);

        return new MonsterMoveStateMachine(states, breath);
    }

    // 形态1·龙息吐息：16+灼伤2
    private async Task DragonBreathMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(BreathDamage).FromMonster(this)
            .WithAttackerAnim("Cast", 0.3f)
            .WithAttackerFx(null, null, "mc_dragon_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, BreathBurn, null);
    }

    // 形态1·翼击：双翼横扫 2×10
    private async Task WingGustMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GustDamage).WithHitCount(GustHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_dragon_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 形态2·俯冲冲撞：全力俯冲撞击 24
    private async Task DiveChargeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DiveDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.35f)
            .WithAttackerFx(null, null, "mc_dragon_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // 形态2·龙息云：低空喷吐龙息云 10+灼伤3
    private async Task DragonCloudMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(CloudDamage).FromMonster(this)
            .WithAttackerAnim("Cast", 0.3f)
            .WithAttackerFx(null, null, "mc_dragon_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await CardPileCmd.AddToCombatAndPreview<Burn>(targets, PileType.Discard, CloudBurn, null);
    }

    // 形态3·龙怒咆哮：力量3+格挡20（狂怒蓄力）
    private async Task RageRoarMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, RoarStrength, Creature, null);
        await CreatureCmd.GainBlock(base.Creature, RoarBlock, ValueProp.Unpowered, null);
    }

    // 形态3·三重俯冲：3×12 连续俯冲
    private async Task TripleDiveMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TripleDiveDamage).WithHitCount(TripleDiveHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_dragon_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // 形态3·死亡凝视：22 重击
    private async Task DeathStareMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StareDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_dragon_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }

    // 复活招式：切换形态（第二次复活后移除复活力量，第三次死亡为真死）
    private async Task RespawnMove(IReadOnlyList<Creature> targets)
    {
        Respawns++;
        await CreatureCmd.TriggerAnim(base.Creature, "Idle", 0f);
        switch (Respawns)
        {
            case 1:
                await CreatureCmd.SetMaxHp(Creature, SecondFormHp);
                await CreatureCmd.Heal(Creature, SecondFormHp);
                break;
            case 2:
                await CreatureCmd.SetMaxHp(Creature, ThirdFormHp);
                await CreatureCmd.Heal(Creature, ThirdFormHp);
                await PowerCmd.Remove<EnderDragonRevivePower>(base.Creature);
                // 第三形态：天空之翼——飞行状态受到攻击伤害减半
                await PowerCmd.Apply<DragonFlightPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
                break;
        }
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new AnimState("idle_loop", isLooping: true);
        AnimState cast = new AnimState("cast");
        AnimState attack = new AnimState("attack");
        AnimState hurt = new AnimState("hurt");
        AnimState die = new AnimState("die");
        cast.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Dead", die);
        return animator;
    }
}
