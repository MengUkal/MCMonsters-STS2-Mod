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
/// 我的世界：女巫（第3幕普通怪，重做版）
/// 四段循环：毒剂投掷（中毒3）→ 诅咒喷洒（虚弱3）→ 腐蚀投掷（20）→ 饮用治疗（回复20%最大生命）。
/// 治疗按百分比：单刷260血=回52；多人联机怪物血量按人数缩放，治疗随之放大。
/// 数值：260血（进阶268）。
/// </summary>
public sealed class Witch : MonsterModel
{
    // MC女巫26HP → STS2第3幕 260（重做：更肉的四段循环）
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 268, 260);
    public override int MaxInitialHp => MinInitialHp;

    private int PoisonAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int WeakAmount => 3;
    private int ThrowDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);
    private const decimal HealFraction = 0.2m; // 每次回复最大生命的1/5

    // AI循环：毒剂投掷 → 诅咒喷洒 → 腐蚀投掷 → 饮用治疗 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState poison = new MoveState("TOXIC_VIAL", PoisonMove, new DebuffIntent());
        MoveState curse = new MoveState("CURSE_SPRAY", CurseMove, new DebuffIntent());
        MoveState acid = new MoveState("ACID_THROW", AcidThrowMove, new SingleAttackIntent(ThrowDamage));
        MoveState heal = new MoveState("HEAL_POTION", HealMove, new HealIntent());

        poison.FollowUpState = curse;
        curse.FollowUpState = acid;
        acid.FollowUpState = heal;
        heal.FollowUpState = poison;

        states.Add(poison);
        states.Add(curse);
        states.Add(acid);
        states.Add(heal);

        return new MonsterMoveStateMachine(states, poison);
    }

    // 毒剂投掷：给予玩家中毒3
    private async Task PoisonMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_witch_throw.mp3");
        await PowerCmd.Apply<PoisonPower>(new ThrowingPlayerChoiceContext(), targets, PoisonAmount, Creature, null);
    }

    // 诅咒喷洒：给予玩家虚弱3
    private async Task CurseMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_witch_throw.mp3");
        await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(), targets, WeakAmount, Creature, null);
    }

    // 腐蚀投掷：攻击20
    private async Task AcidThrowMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ThrowDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithAttackerFx(null, null, "mc_witch_throw.mp3")
            .WithHitFx("vfx/vfx_slime_impact")
            .Execute(null);
    }

    // 饮用治疗：回复自身20%最大生命（多人联机血量缩放后治疗同步放大；向下取整防小数血量）
    private async Task HealMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_witch_drink.ogg");
        decimal healAmount = decimal.Floor(base.Creature.MaxHp * HealFraction);
        await CreatureCmd.Heal(base.Creature, healAmount);
    }
}
