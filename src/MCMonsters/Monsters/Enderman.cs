using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：末影人（第3幕普通怪，单怪遭遇）
/// 被动【空间闪避】：每次受到攻击后瞬移+4格挡，无限触发（惩罚多段攻击流）。
/// 被动【虚空侵蚀】：每当敌方回合开始，玩家失去1点敏捷。
/// 中立生物：未被玩家以牌指定目标时保持潜行（无事发生）；
/// 一旦打出以它为目标的牌，它立刻被激怒——意图当场变为攻击并进入攻击循环。
/// 招式循环：瞬移撕裂（17+20格挡）→ 虚空凝视（虚弱2）→ 虚空重击（25）。
/// 数值：230血（进阶238）。
/// </summary>
public sealed class Enderman : MonsterModel
{
    private MoveState? _rendState;
    private bool _aggro;

    // MC末影人40HP但高挑 → STS2第3幕单怪 230
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 238, 230);
    public override int MaxInitialHp => MinInitialHp;

    private int RendDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 17);
    private int RendBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 24, 20);
    private int GazeWeak => 2;
    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 27, 25);

    private MoveState RendState
    {
        get => _rendState!;
        set
        {
            AssertMutable();
            _rendState = value;
        }
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<EndermanDodgePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<EndermanErosionPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 潜行 →（被以牌指定目标后）瞬移撕裂 → 虚空凝视 → 虚空重击 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState dormant = new MoveState("DORMANT", McGuard.Move("DORMANT", DormantMove), new SleepIntent());
        MoveState rend = new MoveState("TELEPORT_REND", McGuard.Move("TELEPORT_REND", RendMove), new SingleAttackIntent(RendDamage), new DefendIntent());
        MoveState gaze = new MoveState("VOID_GAZE", McGuard.Move("VOID_GAZE", GazeMove), new DebuffIntent());
        MoveState smash = new MoveState("VOID_SMASH", McGuard.Move("VOID_SMASH", SmashMove), new SingleAttackIntent(SmashDamage));
        RendState = rend;
        _aggro = false;

        ConditionalBranchState wake = new ConditionalBranchState("ENDERMAN_WAKE");
        wake.AddState(dormant, () => !_aggro);
        wake.AddState(rend, () => _aggro);

        dormant.FollowUpState = wake;
        rend.FollowUpState = gaze;
        gaze.FollowUpState = smash;
        smash.FollowUpState = rend;

        states.Add(dormant);
        states.Add(wake);
        states.Add(rend);
        states.Add(gaze);
        states.Add(smash);

        return new MonsterMoveStateMachine(states, dormant);
    }

    // 中立触发：玩家打出以末影人为目标的牌 → 立刻激怒，意图当场变为攻击
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (_aggro || cardPlay.Target == null || cardPlay.Target != base.Creature)
        {
            return;
        }
        _aggro = true;
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_enderman_teleport.ogg");
        if (RendState != null)
        {
            SetMoveImmediate(RendState, forceTransition: true);
        }
        await Task.CompletedTask;
    }

    // 潜行：安静地站在原地（被打会被动瞬移获得格挡）
    private Task DormantMove(IReadOnlyList<Creature> targets)
    {
        return Task.CompletedTask;
    }

    // 瞬移撕裂：攻击17 + 自身格挡20
    private async Task RendMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(RendDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, null, "mc_enderman_teleport.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(base.Creature, RendBlock, ValueProp.Unpowered, null);
    }

    // 虚空凝视：虚弱2（它盯着你）
    private async Task GazeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_enderman_teleport.ogg");
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, GazeWeak, Creature, null);
    }

    // 虚空重击：25大伤害
    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SmashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_enderman_teleport.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
