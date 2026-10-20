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
using MCMonsters.Powers;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：末影人（第3幕普通怪）
/// 被动【空间闪避】：每次受到攻击后瞬移+5格挡，每回合最多2次（惩罚多段攻击流）。
/// 招式循环：瞬移撕裂（12+8格挡）→ 虚空凝视（虚弱2）→ 虚空重击（22）。
/// 数值：150血（进阶158）。
/// </summary>
public sealed class Enderman : MonsterModel
{
    // MC末影人40HP但高挑 → STS2第3幕 150
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 158, 150);
    public override int MaxInitialHp => MinInitialHp;

    private int RendDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 12);
    private int RendBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 10, 8);
    private int GazeWeak => 2;
    private int SmashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 24, 22);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<EndermanDodgePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // AI循环：瞬移撕裂 → 虚空凝视 → 虚空重击 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState rend = new MoveState("TELEPORT_REND", RendMove, new SingleAttackIntent(RendDamage), new DefendIntent());
        MoveState gaze = new MoveState("VOID_GAZE", GazeMove, new DebuffIntent());
        MoveState smash = new MoveState("VOID_SMASH", SmashMove, new SingleAttackIntent(SmashDamage));

        rend.FollowUpState = gaze;
        gaze.FollowUpState = smash;
        smash.FollowUpState = rend;

        states.Add(rend);
        states.Add(gaze);
        states.Add(smash);

        return new MonsterMoveStateMachine(states, rend);
    }

    // 瞬移撕裂：攻击12 + 自身格挡8
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

    // 虚空重击：22大伤害
    private async Task SmashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SmashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_enderman_teleport.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
    }
}
