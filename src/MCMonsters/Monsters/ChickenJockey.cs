using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
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
/// 我的世界：小僵尸鸡骑士（第1幕精英，双命设计，参考官方实验体的复活机制）
/// 第一条命（70）：开场召唤僵尸+骷髅（JockeyRevivePower挡死），
///   之后 攻击8 / 全体力量+1 循环（给所有友军包括小怪叠力量）；
///   被击杀 → 躺一回合（复活招式），复活时场上小怪全部死亡；
/// 第二条命（36）：每回合攻击15，直到真死。
/// </summary>
public sealed class ChickenJockey : MonsterModel
{
    private MoveState? _deadState;

    // 第一条命70（高进阶76）；第二条命36（高进阶40）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 76, 70);
    public override int MaxInitialHp => MinInitialHp;

    public int SecondLifeHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 36);

    private int StrikeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int WarCryStrength => 1;
    private int RushDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<JockeyRevivePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 由 JockeyRevivePower.AfterDeath 在第一次死亡时调用：强制进入复活招式（躺一回合）
    public async Task TriggerDeadState()
    {
        SetMoveImmediate(DeadState!, forceTransition: true);
    }

    private MoveState DeadState
    {
        get => _deadState!;
        set
        {
            AssertMutable();
            _deadState = value;
        }
    }

    // AI流程：
    // 第一命：召唤骑手帮 → (攻击8 ↔ 全体鼓舞) 循环
    // 被击杀 → 浴血重生（小怪全灭+自身回满36）→ 第二命：亡命突袭（15/回合）直到真死
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        DeadState = new MoveState("RESPAWN", RespawnMove, new HealIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };
        MoveState summon = new MoveState("SUMMON", SummonMove, new SummonIntent());
        MoveState strike1 = new MoveState("STRIKE", StrikeMove, new SingleAttackIntent(StrikeDamage));
        MoveState warCry = new MoveState("WAR_CRY", WarCryMove, new BuffIntent());
        MoveState strike2 = new MoveState("STRIKE2", StrikeMove, new SingleAttackIntent(StrikeDamage));
        MoveState rush = new MoveState("RUSH", RushMove, new SingleAttackIntent(RushDamage));

        summon.FollowUpState = strike1;
        strike1.FollowUpState = warCry;
        warCry.FollowUpState = strike2;
        strike2.FollowUpState = warCry;
        DeadState.FollowUpState = rush;
        rush.FollowUpState = rush;

        states.Add(DeadState);
        states.Add(summon);
        states.Add(strike1);
        states.Add(warCry);
        states.Add(strike2);
        states.Add(rush);

        return new MonsterMoveStateMachine(states, summon);
    }

    // 开场：依次召唤僵尸和骷髅（走遭遇槽位）
    private async Task SummonMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        foreach (int _ in new[] { 0, 1 })
        {
            string slot = base.CombatState.Encounter?.GetNextSlot(base.CombatState);
            if (string.IsNullOrEmpty(slot))
            {
                continue;
            }
            if (_ == 0)
            {
                await CreatureCmd.Add<Zombie>(base.CombatState, slot);
            }
            else
            {
                await CreatureCmd.Add<Skeleton>(base.CombatState, slot);
            }
        }
    }

    // 骑手斩击：8（高进阶9）
    private async Task StrikeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(StrikeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_chicken_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 全体鼓舞：所有友军（含自己和小怪）力量+1
    private async Task WarCryMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), base.CombatState.Enemies, WarCryStrength, Creature, null);
    }

    // 浴血重生：场上小怪全部死亡，自己回满第二条命
    private async Task RespawnMove(IReadOnlyList<Creature> targets)
    {
        // 先快照再击杀：击杀会改变Enemies集合，边遍历边杀会漏杀
        var alliesToKill = new List<Creature>();
        foreach (var ally in base.CombatState.Enemies)
        {
            if (ally != Creature && ally.CurrentHp > 0)
            {
                alliesToKill.Add(ally);
            }
        }
        foreach (var ally in alliesToKill)
        {
            await CreatureCmd.Kill(ally);
        }
        Creature.GetPower<JockeyRevivePower>()?.DoRevive();
        await CreatureCmd.SetMaxHp(Creature, SecondLifeHp);
        await CreatureCmd.Heal(Creature, SecondLifeHp);
        await CreatureCmd.TriggerAnim(base.Creature, "Idle", 0f);
        // 移除复活力量：第二命的死亡就是真死（否则会无限复活）
        await PowerCmd.Remove<JockeyRevivePower>(base.Creature);
    }

    // 亡命突袭：第二命每回合15
    private async Task RushMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RushDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_chicken_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
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
