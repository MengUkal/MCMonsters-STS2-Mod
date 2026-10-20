using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace MCMonsters.Monsters;

/// <summary>
/// 我的世界：苦力怕
/// 原版行为映射：悄悄逼近 → 嘶嘶膨胀（点燃引信，力量+3）→ 爆炸（大伤害+自爆退场）。
/// 击杀窗口只有第一回合——正是苦力怕的体验。
/// 参考：游戏官方自爆怪 GasBomb（DeathBlowIntent + CreatureCmd.Kill 自毁）。
/// </summary>
public sealed class Creeper : MonsterModel
{
    private bool _hasExploded;

    // 数值（36血；高进阶+6。3回合蓄力期就是击杀窗口）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 42, 36);
    public override int MaxInitialHp => MinInitialHp;

    private int ExplodeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 31, 30);

    public override bool ShouldFadeAfterDeath => false;

    public override DamageSfxType TakeDamageSfxType => DamageSfxType.Magic;

    private bool HasExploded
    {
        get => _hasExploded;
        set
        {
            AssertMutable();
            _hasExploded = value;
        }
    }

    // AI流程：？？？（3回合未知意图蓄力——玩家只有3回合处理它）→ 全场大爆炸
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState hiss = new MoveState("HISS", HissMove, new UnknownIntent());
        MoveState hiss2 = new MoveState("HISS2", HissMove, new UnknownIntent());
        MoveState hiss3 = new MoveState("HISS3", HissMove, new UnknownIntent());
        MoveState explode = new MoveState("EXPLODE", ExplodeMove, new DeathBlowIntent(() => ExplodeDamage));

        hiss.FollowUpState = hiss2;
        hiss2.FollowUpState = hiss3;
        hiss3.FollowUpState = explode;

        states.Add(hiss);
        states.Add(hiss2);
        states.Add(hiss3);
        states.Add(explode);

        return new MonsterMoveStateMachine(states, hiss);
    }

    // 蓄力：冒白烟涨大（无数值效果，纯粹的死亡倒计时）
    private async Task HissMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.6f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_creeper_hiss.ogg");
    }

    // 爆炸：全场所有单位（玩家+所有敌人）各受30点，然后自爆退场
    // MC原味：苦力怕的爆炸不分敌我，队友僵尸也会被炸
    private async Task ExplodeMove(IReadOnlyList<Creature> targets)
    {
        HasExploded = true;
        var combatState = base.CombatState;
        foreach (var unit in combatState.Creatures)
        {
            if (unit == Creature || !unit.IsHittable)
            {
                continue;   // 自己的爆炸由自毁体现，不重复结算
            }
            await DamageCmd.Attack(ExplodeDamage).FromMonster(this)
                .WithNoAttackerAnim()
                .WithAttackerFx(null, "event:/sfx/enemy/enemy_attacks/living_fog/living_fog_explode")
                .Targeting(unit)
                .Execute(null);
        }
        global::MegaCrit.Sts2.Core.Commands.VfxCmd.PlayOnCreatureCenters(combatState.Creatures, "vfx/fire_impact/vfx_fire_burst_center_flipbook");
        global::MegaCrit.Sts2.Core.Nodes.NGame.Instance?.ScreenShake(global::MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.ShakeStrength.Strong, global::MegaCrit.Sts2.Core.Nodes.Vfx.Utilities.ShakeDuration.Normal);
        await CreatureCmd.Kill(base.Creature);
    }

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new AnimState("idle_loop", isLooping: true);
        AnimState cast = new AnimState("cast");
        AnimState attack = new AnimState("attack");
        AnimState hurt = new AnimState("hurt");
        AnimState die = new AnimState("die");
        AnimState explode = new AnimState("explode");
        cast.NextState = idle;
        attack.NextState = idle;
        hurt.NextState = idle;
        CreatureAnimator animator = new CreatureAnimator(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Hit", hurt);
        animator.AddAnyState("Dead", die, () => !HasExploded);
        animator.AddAnyState("ExplodeTrigger", explode);
        return animator;
    }
}
