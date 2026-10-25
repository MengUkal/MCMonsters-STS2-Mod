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
/// 我的世界：袭击队长（Raid Captain，第3幕精英「掠夺者突袭」的主怪）
/// 与普通掠夺者完全不同的意图：号令全军、狙击、压制射击。
/// 立绘复用掠夺者骨架，视觉尺寸放大一圈（targetH 300 vs 250）。
/// 招式循环：号令（全体友军+1力量+自身格挡10）→ 狙击（20）→ 压制射击（2×10+虚弱1）。
/// 数值：130血（进阶138）。
/// </summary>
public sealed class RaidCaptain : MonsterModel
{
    // MC袭击队长 = 掠夺者变体，精英主怪
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 138, 130);
    public override int MaxInitialHp => MinInitialHp;

    private int RallyStrength => 1;
    private int RallyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 10);
    private int SnipeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);
    private int SuppressDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 11, 10);
    private int SuppressHits => 2;
    private int SuppressWeak => 1;

    // AI循环：号令 → 狙击 → 压制射击 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState rally = new MoveState("RALLY", McGuard.Move("RALLY", RallyMove), new BuffIntent(), new DefendIntent());
        MoveState snipe = new MoveState("SNIPE", McGuard.Move("SNIPE", SnipeMove), new SingleAttackIntent(SnipeDamage));
        MoveState suppress = new MoveState("SUPPRESS", McGuard.Move("SUPPRESS", SuppressMove), new MultiAttackIntent(SuppressDamage, SuppressHits), new DebuffIntent());

        rally.FollowUpState = snipe;
        snipe.FollowUpState = suppress;
        suppress.FollowUpState = rally;

        states.Add(rally);
        states.Add(snipe);
        states.Add(suppress);

        return new MonsterMoveStateMachine(states, rally);
    }

    // 号令：全体友军+1力量 + 自身格挡10
    private async Task RallyMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.5f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_pillager_attack.ogg");
        var allies = base.CombatState.HittableEnemies;
        foreach (var ally in allies)
        {
            if (ally.IsAlive)
            {
                await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), ally, RallyStrength, Creature, null);
            }
        }
        await CreatureCmd.GainBlock(base.Creature, RallyBlock, ValueProp.Unpowered, null);
    }

    // 狙击：20单发重弩
    private async Task SnipeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SnipeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_pillager_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 压制射击：2×10 + 虚弱1
    private async Task SuppressMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SuppressDamage).WithHitCount(SuppressHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_pillager_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, SuppressWeak, Creature, null);
    }
}
