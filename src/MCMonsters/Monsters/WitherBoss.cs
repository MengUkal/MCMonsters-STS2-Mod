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
/// 我的世界：凋灵（Wither，第2幕BOSS）
/// 原版行为映射：三头凋灵——凋零之触持续侵蚀，半血进入疯狂护壁状态。
/// 被动【幽冥护壁】：生命降至一半以下时获得99层人工制品（免疫一切状态效果）。
/// 招式循环：挥砍16 → 凋零之触（6+2层凋零）→ 狂怒连击（2×6）→ 幽冥强化（力量+1+格挡16）。
/// 数值：第2幕BOSS 356血（进阶375）。
/// </summary>
public sealed class WitherBoss : MonsterModel
{
    // MC凋灵300HP → STS2第2幕BOSS 356
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 375, 356);
    public override int MaxInitialHp => MinInitialHp;

    private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 16);
    private int TouchDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int TouchWither => 2;
    private int FuryDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private int FuryHits => 2;
    private int BuffStrength => 1;
    private int BuffBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 18, 16);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<WitherWardingPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // AI循环：挥砍 → 凋零之触 → 狂怒连击 → 幽冥强化 → 循环
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        List<MonsterState> states = new List<MonsterState>();

        MoveState slash = new MoveState("SLASH", McGuard.Move("SLASH", SlashMove), new SingleAttackIntent(SlashDamage));
        MoveState touch = new MoveState("WITHER_TOUCH", McGuard.Move("WITHER_TOUCH", TouchMove), new SingleAttackIntent(TouchDamage), new DebuffIntent());
        MoveState fury = new MoveState("FURY", McGuard.Move("FURY", FuryMove), new MultiAttackIntent(FuryDamage, FuryHits));
        MoveState buff = new MoveState("WARDING", McGuard.Move("WARDING", WardingMove), new BuffIntent(), new DefendIntent());

        slash.FollowUpState = touch;
        touch.FollowUpState = fury;
        fury.FollowUpState = buff;
        buff.FollowUpState = slash;

        states.Add(slash);
        states.Add(touch);
        states.Add(fury);
        states.Add(buff);

        return new MonsterMoveStateMachine(states, slash);
    }

    // 挥砍：16
    private async Task SlashMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(SlashDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.25f)
            .WithAttackerFx(null, null, "mc_wither_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 凋零之触：6 + 2层凋零（每回合每层失去1点生命）
    private async Task TouchMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(TouchDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.2f)
            .WithAttackerFx(null, null, "mc_wither_attack.ogg")
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);
        await PowerCmd.Apply<McWitherPower>(new ThrowingPlayerChoiceContext(), targets, TouchWither, Creature, null);
    }

    // 狂怒连击：2×6
    private async Task FuryMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(FuryDamage).WithHitCount(FuryHits).FromMonster(this)
            .OnlyPlayAnimOnce()
            .WithAttackerAnim("Attack", 0.15f)
            .WithAttackerFx(null, null, "mc_wither_attack.ogg")
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    // 幽冥强化：力量+1 + 格挡16
    private async Task WardingMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(base.Creature, "Cast", 0.4f);
        global::MegaCrit.Sts2.Core.Audio.Debug.NDebugAudioManager.Instance?.Play("mc_wither_attack.ogg");
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, BuffStrength, Creature, null);
        await CreatureCmd.GainBlock(base.Creature, BuffBlock, ValueProp.Unpowered, null);
    }
}
